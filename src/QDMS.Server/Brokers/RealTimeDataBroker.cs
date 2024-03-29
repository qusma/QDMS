﻿// -----------------------------------------------------------------------
// <copyright file="RealTimeDataBroker.cs" company="">
//     Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NLog;
using QDMS;
using QDMSApp.DataSources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;
using Timer = System.Timers.Timer;

namespace QDMSApp
{
    /// <summary>
    /// The RealTimeDataBroker sits between the HistoricalDataServer and the external data source adapters. Requests for new real time data
    /// streams are handled in RequestRealTimeData(), then forwarded the appropriate external data source (if the stream doesn't already exist)
    /// </summary>
    public class RealTimeDataBroker : IRealTimeDataBroker
    {
        /// <summary>
        /// When there's a request for real time data of a continuous future, obviously the symbol received from the external source is not
        /// the continuous future itself, but the actual futures contract. So we use aliases: the key is the underlying contract and the
        /// value is a list of aliases that are also sent out.
        /// </summary>
        private readonly Dictionary<int, List<int>> _aliases = new Dictionary<int, List<int>>();

        ///<summary>
        ///When bars arrive, the data source raises an event
        ///the event adds the data to the _arrivedBars
        ///then the publishing server sends out the data
        ///</summary>
        private readonly BlockingCollection<RealTimeDataEventArgs> _arrivedBars = new BlockingCollection<RealTimeDataEventArgs>();

        /// <summary>
        /// Maps continuous futures instrument IDs to the front contract instrument ID.
        /// Key: CF ID, Value: front contract ID
        /// </summary>
        private readonly Dictionary<int, int> _continuousFuturesIDMap = new Dictionary<int, int>();

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// When a real time data request for a continuous future comes in, we have to make an asynchronous request to find which actual
        /// futures contract we need to request. This dictionary holds the IDs from the front contract requests and their corresponding RealTimeDataRequests.
        /// </summary>
        private readonly Dictionary<int, RealTimeDataRequest> _pendingCFRealTimeRequests = new Dictionary<int, RealTimeDataRequest>();

        /// <summary>
        /// Here we store the requests, key is the AssignedID.
        /// </summary>
        private readonly Dictionary<int, RealTimeDataRequest> _requests = new Dictionary<int, RealTimeDataRequest>();

        private readonly object _aliasLock = new object();
        private readonly object _activeStreamsLock = new object();
        private readonly object _cfRequestLock = new object();
        private readonly object _requestsLock = new object();
        private readonly object _subscriberCountLock = new object();

        private IContinuousFuturesBroker _cfBroker;

        private Timer _connectionTimer;

        private IDataStorage _localStorage;

        /// <summary>
        /// For id generation
        /// </summary>
        private Random _rand = new Random();

        /// <summary>
        /// Keeps track of IDs assigned to requests that have already been used, so there are no duplicates.
        /// </summary>
        private HashSet<int> _usedIDs = new HashSet<int>();

        /// <summary>
        /// Holds the active data streams. They KVP consists of key: request ID, value: data source name
        /// </summary>
        public ConcurrentNotifierBlockingList<RealTimeStreamInfo> ActiveStreams { get; } = new ConcurrentNotifierBlockingList<RealTimeStreamInfo>();

        /// <summary>
        /// Holds the real time data sources.
        /// </summary>
        public ObservableDictionary<string, IRealTimeDataSource> DataSources { get; } = new ObservableDictionary<string, IRealTimeDataSource>();

        /// <summary>
        /// Here we keep track of what clients are subscribed to what data streams. KVP consists of key: request ID, value: data source
        /// name. The int is simply the number of clients subscribed to that stream.
        /// </summary>
        private Dictionary<RealTimeStreamInfo, int> StreamSubscribersCount { get; } = new Dictionary<RealTimeStreamInfo, int>();

        public event EventHandler<RealTimeDataEventArgs> RealTimeDataArrived;

        public event EventHandler<TickEventArgs> RealTimeTickArrived;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="additionalDataSources">Optional. Pass any additional data sources (for testing purposes).</param>
        /// <param name="cfBroker">IContinuousFuturesBroker (for testing purposes).</param>
        public RealTimeDataBroker(IContinuousFuturesBroker cfBroker, IDataStorage localStorage, IEnumerable<IRealTimeDataSource> additionalDataSources = null)
        {
            if (cfBroker == null)
                throw new ArgumentNullException("cfBroker");
            if (localStorage == null)
                throw new ArgumentNullException("localStorage");

            //timer for automatic reconnection to datasources
            InitializeConnectionTimer();

            DataSources = new ObservableDictionary<string, IRealTimeDataSource>
            {
                {"SIM", new RealTimeSim()}
            };

            foreach (IRealTimeDataSource ds in additionalDataSources)
            {
                AddDataSource(ds);
            }

            //connect to our data sources
            TryConnect();

            //local storage
            _localStorage = localStorage;

            //hook up the continuous futures broker
            _cfBroker = cfBroker;
            _cfBroker.FoundFrontContract += _cfBroker_FoundFrontContract;
        }

        /// <summary>
        /// Cancel a real time data stream and clean up after it.
        /// </summary>
        /// <returns>True if the stream was canceled, False if subsribers remain.</returns>
        public bool CancelRTDStream(int instrumentID, BarSize frequency)
        {
            lock (_activeStreamsLock)
            {
                //make sure there is a data stream for this instrument
                if (!ActiveStreams.Collection.Any(x => x.Instrument.ID == instrumentID && x.Frequency == frequency))
                {
                    Log(LogLevel.Warn, "Received cancelation request for stream that does not exist");
                    return false;
                }

                var streamInfo = GetStreamInfo(instrumentID, frequency);
                var instrument = streamInfo.Instrument;

                //if it's a continuous future we also need to cancel the actual contract
                if (instrument.IsContinuousFuture)
                {
                    CancelContinuousFutureRTD(instrumentID, frequency, instrument);
                }

                //log the request
                Log(LogLevel.Info,
                    string.Format("RTD Cancelation request: {0} from {1}",
                        instrument.Symbol,
                        instrument.Datasource.Name));

                lock (_subscriberCountLock)
                {
                    StreamSubscribersCount[streamInfo]--;
                    if (StreamSubscribersCount[streamInfo] == 0)
                    {
                        //there are no clients subscribed to this stream anymore
                        //cancel it and remove it from all the places
                        StreamSubscribersCount.Remove(streamInfo);

                        ActiveStreams.TryRemove(streamInfo);

                        if (!instrument.IsContinuousFuture)
                        {
                            DataSources[streamInfo.Datasource].CancelRealTimeData(streamInfo.RequestID);
                        }
                        return true;
                    }
                }

                return false;
            }
        }

        //tries to reconnect every once in a while
        public void Dispose()
        {
            if (_cfBroker != null)
            {
                _cfBroker.Dispose();
                _cfBroker = null;
            }
            if (_connectionTimer != null)
            {
                _connectionTimer.Dispose();
                _connectionTimer = null;
            }
            foreach (var dataSource in DataSources.Where(ds => ds.Value is IDisposable))
            {
                ((IDisposable)dataSource.Value).Dispose();
            }

            if (_arrivedBars != null)
                _arrivedBars.Dispose();
        }

        /// <summary>
        /// When one of the data sources receives new real time data, it raises an event which is handled by this method, which then
        /// forwards the data over the PUB socket after serializing it.
        /// </summary>
        public void RealTimeData(object sender, RealTimeDataEventArgs e)
        {
            RaiseEvent(RealTimeDataArrived, this, e);

            //continuous futures aliases
            RaiseRealTimeDataOnContFutAliases(e);

            //save to local storage
            SaveRealTimeDataToLocalStorage(e);

#if DEBUG
            Log(LogLevel.Trace,
                string.Format("RTD Received Instrument ID: {0} O:{1} H:{2} L:{3} C:{4} V:{5} T:{6}",
                    e.InstrumentID,
                    e.Open,
                    e.High,
                    e.Low,
                    e.Close,
                    e.Volume,
                    e.Time));
#endif
        }

        /// <summary>
        /// Request to initiate a real time data stream.
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns>True is the request was successful, false otherwise.</returns>
        public void RequestRealTimeData(RealTimeDataRequest request)
        {
            request.AssignedID = GetUniqueRequestID();
            lock (_requestsLock)
            {
                _requests.Add(request.AssignedID, request);
            }

            //if there is already an active stream of this instrument
            bool streamExists = CheckStreamExists(request);

            if (streamExists)
            {
                HandleReqForExistingStream(request);
            }
            else
            {
                HandleReqForNewStream(request);
            }
        }

        ///<summary>
        /// Raise the event in a threadsafe manner
        ///</summary>
        private static void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
            where T : EventArgs
        {
            EventHandler<T> handler = @event;
            if (handler == null) return;
            handler(sender, e);
        }

        /// <summary>
        /// This method is called when the continuous futures broker returns the results of a request for the "front" contract of a
        /// continuous futures instrument.
        /// </summary>
        private void _cfBroker_FoundFrontContract(object sender, FoundFrontContractEventArgs e)
        {
            RealTimeDataRequest request;
            if (!e.Instrument.ID.HasValue)
            {
                Log(LogLevel.Error, "CF Broker returned front contract with no ID");
                return;
            }

            Log(LogLevel.Info, string.Format("Front contract received on request ID {0}, is: {1}", e.ID, e.Instrument.Symbol));

            //grab the original request
            request = GetQueuedContFutRequest(e);

            //add the contract to the ID map
            if (request.Instrument.ID.HasValue &&
                !_continuousFuturesIDMap.ContainsKey(request.Instrument.ID.Value))
            {
                _continuousFuturesIDMap.Add(request.Instrument.ID.Value, e.Instrument.ID.Value);
            }

            //add the alias
            AddContFutAlias(e, request);

            //need to check if there's already a stream of the contract....
            bool streamExists;
            lock (_activeStreamsLock)
            {
                streamExists = ActiveStreams.Collection.Any(x => x.Instrument.ID == e.Instrument.ID);
            }

            if (streamExists)
            {
                //all we need to do in this case is increment the number of subscribers to this stream
                IncrementSubscriberCount(e.Instrument);

                Log(LogLevel.Info,
                    string.Format("RTD Request for CF {0} @ {1} {2}, filled by existing stream of symbol {3}.",
                        request.Instrument.Symbol,
                        request.Instrument.Datasource.Name,
                        Enum.GetName(typeof(BarSize), request.Frequency),
                        e.Instrument.Symbol));
            }
            else
            {
                //no current stream of this contract, add it
                InitializeContFutStream(e, request);
            }
        }

        private void AddContFutAlias(FoundFrontContractEventArgs e, RealTimeDataRequest request)
        {
            lock (_aliasLock)
            {
                int contractID = e.Instrument.ID.Value;
                if (!_aliases.ContainsKey(contractID))
                {
                    _aliases.Add(contractID, new List<int>());
                }

                if (request.Instrument.ID.HasValue)
                {
                    _aliases[contractID].Add(request.Instrument.ID.Value);
                }
            }
        }

        private void AddDataSource(IRealTimeDataSource ds)
        {
            DataSources.Add(ds.Name, ds);

            //hook up all the relevant events
            ds.DataReceived += RealTimeData;
            ds.TickReceived += TickReceived;
            ds.Disconnected += SourceDisconnects;
            ds.Error += s_Error;
        }

        private void CancelContinuousFutureRTD(int instrumentID, BarSize frequency, Instrument instrument)
        {
            var contractID = _continuousFuturesIDMap[instrumentID];
            var contract = ActiveStreams.Collection.First(x => x.Instrument.ID == contractID).Instrument;

            //we must also clear the alias list
            lock (_aliasLock)
            {
                _aliases[contractID].Remove(instrumentID);
                if (_aliases[contractID].Count == 0)
                {
                    _aliases.Remove(contractID);
                }
            }

            //finally cancel the contract's stream
            CancelRTDStream(contractID, frequency);
        }

        private bool CheckStreamExists(RealTimeDataRequest request)
        {
            bool streamExists;
            lock (_activeStreamsLock)
            {
                streamExists = ActiveStreams.Collection.Any(x => x.Instrument.ID == request.Instrument.ID &&
                                                                 x.Frequency == request.Frequency);
            }

            return streamExists;
        }

        /// <summary>
        /// There is a timer which periodically calls the tryconnect function to connect to any disconnected data sources
        /// </summary>
        private void ConnectionTimerElapsed(object sender, ElapsedEventArgs e)
        {
            TryConnect();
        }

        /// <summary>
        /// Sends a real time data request to the correct data source, logs it, and updates subscriber counts
        /// </summary>
        /// <param name="request"></param>
        private void ForwardRTDRequest(RealTimeDataRequest request)
        {
            //send the request to the correct data source
            try
            {
                DataSources[request.Instrument.Datasource.Name].RequestRealTimeData(request);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Error requesting real time data: " + ex.Message);
                return;
            }

            //log the request
            Log(LogLevel.Info,
                string.Format("RTD Request: {0} from {1} @ {2} ID:{3}",
                    request.Instrument.Symbol,
                    request.Instrument.Datasource.Name,
                    Enum.GetName(typeof(BarSize), request.Frequency),
                    request.AssignedID));

            //add the request to the active streams, though it's not necessarily active yet
            InitializeStreamInfo(request, request.AssignedID);
        }

        private RealTimeDataRequest GetQueuedContFutRequest(FoundFrontContractEventArgs e)
        {
            RealTimeDataRequest request;
            lock (_cfRequestLock)
            {
                request = _pendingCFRealTimeRequests[e.ID];
                _pendingCFRealTimeRequests.Remove(e.ID);
            }

            return request;
        }

        private RealTimeStreamInfo GetStreamInfo(int instrumentID, BarSize frequency)
        {
            return ActiveStreams.Collection.First(x => x.Instrument.ID == instrumentID && x.Frequency == frequency);
        }

        /// <summary>
        /// Gets a new unique AssignedID to identify requests with.
        /// </summary>
        private int GetUniqueRequestID()
        {
            //requests can arrive very close to each other and thus have the same seed, so we need to make sure it's unique
            int id;
            do
            {
                id = _rand.Next(1, int.MaxValue);
            } while (_usedIDs.Contains(id));

            _usedIDs.Add(id);
            return id;
        }

        /// <summary>
        /// A client has requested real-time data for a stream that already exists
        /// </summary>
        /// <param name="request"></param>
        private void HandleReqForExistingStream(RealTimeDataRequest request)
        {
            IncrementSubscriberCount(request.Instrument);

            //log the request
            Log(LogLevel.Info,
                string.Format("RTD Request for existing stream: {0} from {1} @ {2}",
                    request.Instrument.Symbol,
                    request.Instrument.Datasource.Name,
                    Enum.GetName(typeof(BarSize), request.Frequency)));
        }

        /// <summary>
        /// Handle a request to start a new real-time stream for a continuous future
        /// </summary>
        /// <param name="request"></param>
        private void HandleReqForNewContFutStream(RealTimeDataRequest request)
        {
            //if it's a CF, we need to find which contract is currently "used"
            //and request that one. The client will raise the foundfrontcontract event, we handle the rest of the request there.
            int frontContractRequestID;

            lock (_cfRequestLock)
            {
                frontContractRequestID = _cfBroker.RequestFrontContract(request.Instrument);
                _pendingCFRealTimeRequests.Add(frontContractRequestID, request);
            }

            Log(LogLevel.Info,
                string.Format("Request for CF RTD, sent front contract request, RT request ID: {0}, FC request ID: {1}",
                request.AssignedID,
                frontContractRequestID));

            //the asynchronous nature of the request for the front month creates a lot of problems
            //we either have to abandon the REP socket and use something asynchronous there
            //which creates a ton of problems (we need unique IDs for every request and so forth)
            //or we send back "success" without actually knowing if the request for the
            //continuous futures real time data was successful or not!
            //For now I have chosen the latter approach.
        }

        /// <summary>
        /// Handle a request to start a new real-time stream
        /// </summary>
        /// <param name="request"></param>
        private void HandleReqForNewStream(RealTimeDataRequest request)
        {
            //make sure the datasource is present & connected
            if (!DataSources.ContainsKey(request.Instrument.Datasource.Name))
            {
                throw new Exception("No such datasource.");
            }
            if (!DataSources[request.Instrument.Datasource.Name].Connected)
            {
                throw new Exception("Datasource not connected.");
            }

            if (request.Instrument.IsContinuousFuture)
            {
                HandleReqForNewContFutStream(request);
            }
            else
            {
                //NOT a continuous future, just a normal instrument: do standard request procedure
                ForwardRTDRequest(request);
            }
        }

        /// <summary>
        /// Increments the number of subscribers to a real time data stream by 1.
        /// </summary>
        private void IncrementSubscriberCount(Instrument instrument)
        {
            lock (_activeStreamsLock)
            {
                var streamInfo = ActiveStreams.Collection.First(x => x.Instrument.ID == instrument.ID);

                lock (_subscriberCountLock)
                {
                    StreamSubscribersCount[streamInfo]++;

                    //if it's a continuous future we also need to increment the counter on the actual contract
                    if (instrument.IsContinuousFuture && instrument.ID.HasValue)
                    {
                        var contractID = _continuousFuturesIDMap[instrument.ID.Value];
                        var contractStreamInfo = ActiveStreams.Collection.First(x => x.Instrument.ID == contractID);
                        StreamSubscribersCount[contractStreamInfo]++;
                    }
                }
            }
        }

        private void InitializeConnectionTimer()
        {
            _connectionTimer = new Timer(10000);
            _connectionTimer.Elapsed += ConnectionTimerElapsed;
            _connectionTimer.Start();
        }

        /// <summary>
        /// Starts a real-time stream for a continuous future
        /// </summary>
        /// <param name="e"></param>
        /// <param name="request"></param>
        private void InitializeContFutStream(FoundFrontContractEventArgs e, RealTimeDataRequest request)
        {
            //make the request
            var contractRequest = (RealTimeDataRequest)request.Clone();
            contractRequest.Instrument = e.Instrument; //we take the original request, and substitute the CF for the front contract
            ForwardRTDRequest(contractRequest);

            //add the request to the active streams, though it's not necessarily active yet
            InitializeStreamInfo(request, -1);

            Log(LogLevel.Info,
                string.Format("RTD Request for CF: {0} from {1} @ {2}, filled by contract: {3}",
                    request.Instrument.Symbol,
                    request.Instrument.Datasource.Name,
                    Enum.GetName(typeof(BarSize), request.Frequency),
                    e.Instrument.Symbol));
        }

        /// <summary>
        /// Creates a RealTimeStreamInfo, adds it to the ActiveStreams, and begins tracking subscriber counts
        /// </summary>
        /// <param name="request"></param>
        private void InitializeStreamInfo(RealTimeDataRequest request, int assignedId)
        {
            var streamInfo = new RealTimeStreamInfo(
                request.Instrument,
                assignedId,
                request.Instrument.Datasource.Name,
                request.Frequency,
                request.RTHOnly);

            lock (_activeStreamsLock)
            {
                ActiveStreams.TryAdd(streamInfo);
            }

            lock (_subscriberCountLock)
            {
                StreamSubscribersCount.Add(streamInfo, 1);
            }
        }

        /// <summary>
        /// Log stuff.
        /// </summary>
        private void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }

        /// <summary>
        /// If a particular instrument has aliases, we trigger the data event for them as well
        /// </summary>
        /// <param name="e"></param>
        private void RaiseRealTimeDataOnContFutAliases(RealTimeDataEventArgs e)
        {
            lock (_aliasLock)
            {
                int instrumentID = e.InstrumentID;
                if (_aliases.ContainsKey(instrumentID))
                {
                    for (int i = 0; i < _aliases[instrumentID].Count; i++)
                    {
                        e.InstrumentID = _aliases[instrumentID][i];
                        RaiseEvent(RealTimeDataArrived, this, e);
                    }
                }
            }
        }

        /// <summary>
        /// When one of the data sources has some sort of error, it raises an event which is handled by this method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void s_Error(object sender, ErrorArgs e)
        {
            Log(LogLevel.Error, string.Format("RTB: {0} - {1}", e.ErrorCode, e.ErrorMessage));
        }

        private void SaveRealTimeDataToLocalStorage(RealTimeDataEventArgs e)
        {
            //the data is added to a queue and processed in batches
            if (_requests[e.RequestID].SaveToLocalStorage)
            {
                _localStorage.AddDataAsync(
                    new OHLCBar { Open = e.Open, High = e.High, Low = e.Low, Close = e.Close, Volume = e.Volume, DT = MyUtils.TimestampToDateTime(e.Time) },
                    _requests[e.RequestID].Instrument,
                    _requests[e.RequestID].Frequency,
                    overwrite: false);
            }
        }

        /// <summary>
        /// This method is called when a data source disconnects
        /// </summary>
        private void SourceDisconnects(object sender, DataSourceDisconnectEventArgs e)
        {
            Log(LogLevel.Info, string.Format("Real Time Data Broker: Data source {0} disconnected", e.SourceName));
        }

        /// <summary>
        /// Tick data comes in from a datasource
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TickReceived(object sender, TickEventArgs e)
        {
            RaiseEvent(RealTimeTickArrived, this, e);

            //todo in the future add continuous future aliases here too?

            //todo add saving to storage after we get a tick storage
        }

        /// <summary>
        /// Loops through data sources and tries to connect to those that are disconnected
        /// </summary>
        private void TryConnect()
        {
            foreach (KeyValuePair<string, IRealTimeDataSource> s in DataSources)
            {
                if (!s.Value.Connected)
                {
                    Log(LogLevel.Info, string.Format("Real Time Data Broker: Trying to connect to data source {0}", s.Key));

                    try
                    {
                        s.Value.Connect();
                    }
                    catch (WebException ex)
                    {
                        _logger.Error(ex, "Real Time Data Broker: Error while connecting to data source {0}", s.Key);
                    }
                }
            }
        }
    }
}