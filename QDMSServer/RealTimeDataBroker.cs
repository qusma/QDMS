// -----------------------------------------------------------------------
// <copyright file="RealTimeDataBroker.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using NLog;
using ProtoBuf;
using QDMS;
using QDMSServer.DataSources;
using ZeroMQ;
using Timer = System.Timers.Timer;

namespace QDMSServer
{
    public class RealTimeDataBroker : IDisposable
    {
        /// <summary>
        /// This property determines the port used to send out real time data on the publish socket.
        /// </summary>
        public int PublisherPort { get; private set; }

        /// <summary>
        /// This property determines the port used to receive new requests.
        /// </summary>
        public int RequestPort { get; private set; }

        /// <summary>
        /// Holds the real time data sources.
        /// </summary>
        public Dictionary<string, IRealTimeDataSource> DataSources { get; private set; }

        /// <summary>
        /// Holds the active data streams. They KVP consists of key: request ID, value: data source name
        /// </summary>
        public ConcurrentNotifierBlockingList<RealTimeStreamInfo> ActiveStreams { get; private set; }

        /// <summary>
        /// Here we keep track of what clients are subscribed to what data streams.
        /// KVP consists of key: request ID, value: data source name.
        /// The int is simply the number of clients subscribed to that stream.
        /// </summary>
        private Dictionary<RealTimeStreamInfo, int> StreamSubscribersCount { get; set; }

        /// <summary>
        /// When there's a request for real time data of a continuous future,
        /// obviously the symbol received from the external source is not the
        /// continuous future itself, but the actual futures contract.
        /// So we use aliases: the key is the underlying contract
        /// and the value is a list of aliases that are also sent out.
        /// </summary>
        private readonly Dictionary<string, List<string>> _aliases;

        /// <summary>
        /// Is true if the server is running
        /// </summary>
        public bool ServerRunning { get; private set; }
        
        ///<summary>
        ///When bars arrive, the data source raises an event
        ///the event adds the data to the _arrivedBars
        ///then the publishing server sends out the data
        ///</summary>
        private readonly BlockingCollection<RealTimeDataEventArgs> _arrivedBars;

        /// <summary>
        /// When a real time data request for a continuous future comes in, we have to
        /// make an asynchronous request to find which actual futures contract we need to request.
        /// This dictionary holds the IDs from the front contract requests and their corresponding RealTimeDataRequests.
        /// </summary>
        private readonly Dictionary<int, RealTimeDataRequest> _pendingCFRealTimeRequests;

        /// <summary>
        /// Maps continuous futures instrument IDs to the front contract instrument ID.
        /// Key: CF ID, Value: front contract ID
        /// </summary>
        private readonly Dictionary<int, int> _continuousFuturesIDMap;

        private Thread _requestThread;
        private ContinuousFuturesBroker _cfBroker;
        private bool _runServer = true;
        private ZmqContext _context;
        private ZmqSocket _pubSocket;
        private ZmqSocket _reqSocket;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private Timer _connectionTimer; //tries to reconnect every once in a while
        private MemoryStream _ms;
        private readonly object _pubSocketLock = new object();
        private readonly object _subscriberCountLock = new object();
        private readonly object _aliasLock = new object();
        private readonly object _cfRequestLock = new object();

        public void Dispose()
        {
            if (_ms != null)
            {
                _ms.Dispose();
                _ms = null;
            }
            if (_cfBroker != null)
            {
                _cfBroker.Dispose();
                _cfBroker = null;
            }
            if (_pubSocket != null)
            {
                _pubSocket.Dispose();
                _pubSocket = null;
            }
            if (_reqSocket != null)
            {
                _reqSocket.Dispose();
                _reqSocket = null;
            }
            if (_connectionTimer != null)
            {
                _connectionTimer.Dispose();
                _connectionTimer = null;
            }
            if (DataSources.ContainsKey("Interactive Brokers"))
            {
                ((IB)DataSources["Interactive Brokers"]).Dispose();
            }
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
            if (_arrivedBars != null)
                _arrivedBars.Dispose();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pubPort">The port to use for the publishing server.</param>
        /// <param name="reqPort">The port to use for the request server.</param>
        /// <param name="additionalDataSources">Optional. Pass any additional data sources (for testing purposes).</param>
        /// <param name="cfBroker">Optional. IContinuousFuturesBroker (for testing purposes).</param>
        public RealTimeDataBroker(int pubPort, int reqPort, IEnumerable<IRealTimeDataSource> additionalDataSources = null, IContinuousFuturesBroker cfBroker = null)
        {
            if (pubPort == reqPort) throw new Exception("Publish and request ports must be different");
            PublisherPort = pubPort;
            RequestPort = reqPort;
            _connectionTimer = new Timer(10000);
            _connectionTimer.Elapsed += ConnectionTimerElapsed;
            _connectionTimer.Start();

            DataSources = new Dictionary<string, IRealTimeDataSource> 
            {
                {"SIM", new RealTimeSim()}, 
                {"Interactive Brokers", new IB()}
            };

            if (additionalDataSources != null)
            {
                foreach (IRealTimeDataSource ds in additionalDataSources)
                {
                    DataSources.Add(ds.Name, ds);
                }
            }

            //we need to set the appropriate event methods for every data source
            foreach (IRealTimeDataSource s in DataSources.Values)
            {
                s.DataReceived += RealTimeData;
                s.Disconnected += SourceDisconnects;
                s.Error += s_Error;
            }

            ActiveStreams = new ConcurrentNotifierBlockingList<RealTimeStreamInfo>();
            _arrivedBars = new BlockingCollection<RealTimeDataEventArgs>();
            StreamSubscribersCount = new Dictionary<RealTimeStreamInfo, int>();
            _aliases = new Dictionary<string, List<string>>();
            _pendingCFRealTimeRequests = new Dictionary<int, RealTimeDataRequest>();
            _continuousFuturesIDMap = new Dictionary<int, int>();

            _ms = new MemoryStream();

            //connect to our data sources
            TryConnect();

            //finally start listening and stuff
            StartServer();

            //start up the continuous futures broker
            if (cfBroker == null)
            {
                _cfBroker = new ContinuousFuturesBroker(clientName: "RTDBCFClient");
            }
            _cfBroker.FoundFrontContract += _cfBroker_FoundFrontContract;
        }

        /// <summary>
        /// When one of the data sources has some sort of error, it raises an event which is handled by this method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void s_Error(object sender, ErrorArgs e)
        {
            Log(LogLevel.Error, string.Format("RTB: {0} - {1}", e.ErrorCode, e.ErrorMessage));
        }

        /// <summary>
        /// When one of the data sources receives new real time data, it raises an event which is handled by this method,
        /// which then forwards the data over the PUB socket after serializing it.
        /// </summary>
        public void RealTimeData(object sender, RealTimeDataEventArgs e)
        {
            lock (_pubSocketLock)
            {
                _ms.SetLength(0);
                _pubSocket.SendMore(Encoding.UTF8.GetBytes(e.Symbol)); //start by sending the ticker before the data
                Serializer.Serialize(_ms, e);
                _pubSocket.Send(_ms.ToArray()); //then send the serialized bar
            }

            //continuous futures aliases
            lock (_aliasLock)
            {
                if (_aliases.ContainsKey(e.Symbol))
                {
                    foreach (string alias in _aliases[e.Symbol])
                    {
                        _ms.SetLength(0);
                        _pubSocket.SendMore(Encoding.UTF8.GetBytes(alias)); //start by sending the ticker before the data
                        e.Symbol = alias; //change to the symbol to the alias
                        Serializer.Serialize(_ms, e);
                        _pubSocket.Send(_ms.ToArray()); //then send the serialized bar
                    }
                }
            }

#if DEBUG
            Log(LogLevel.Trace, 
                string.Format("RTD Received Symbol: {0} O:{1} H:{2} L:{3} C:{4} V:{5} T:{6}",
                    e.Symbol,
                    e.Open,
                    e.High,
                    e.Low,
                    e.Close,
                    e.Volume,
                    e.Time));
#endif
        }

        //this function is here because events may execute on other threads, and therefore can't use the logger on this one and must call the dispatcher
        private void Log(LogLevel level, string message)
        {
            Application.Current.Dispatcher.InvokeAsync(() => 
                _logger.Log(level, message));
        }

        //tells the servers to stop running and waits for the threads to shut down.
        public void StopServer()
        {
            _runServer = false;

            //clear the socket and context and say it's not running any more
            if (_pubSocket != null)
                _pubSocket.Dispose();

            _requestThread.Join();
        }


        /// <summary>
        /// Starts the publishing and request servers.
        /// </summary>
        public void StartServer()
        {
            if (!ServerRunning) //only start if it isn't running already
            {
                _runServer = true;
                _context = ZmqContext.Create();
                
                //the publisher socket
                _pubSocket = _context.CreateSocket(SocketType.PUB);
                _pubSocket.Bind("tcp://*:" + PublisherPort);

                //the request socket
                _reqSocket = _context.CreateSocket(SocketType.REP);
                _reqSocket.Bind("tcp://*:" + RequestPort);
                
                _requestThread = new Thread(RequestServer) {Name = "RTDB Request Thread"};

                //clear queue before starting?
                _requestThread.Start();
            }
            ServerRunning = true;
        }


        /// <summary>
        /// This method runs on its own thread. The loop receives requests and sends the appropriate reply.
        /// Can request a ping or to open a new real time data stream.
        /// </summary>
        private void RequestServer()
        {
            TimeSpan timeout = new TimeSpan(50000);
            
            MemoryStream ms = new MemoryStream();
            while (_runServer)
            {
                string requestType = _reqSocket.Receive(Encoding.UTF8, timeout);
                if (requestType == null) continue;

                //handle ping requests
                if (requestType == "PING")
                {
                    _reqSocket.Send("PONG", Encoding.UTF8);
                    continue;
                }

                //Handle real time data requests
                if (requestType == "RTD") //Two part message: first, "RTD" string. Then the RealTimeDataRequest object.
                {
                    HandleRTDataRequest(timeout, ms);
                }

                //manage cancellation requests
                //two part message: first: "CANCEL". Second: the instrument
                if (requestType == "CANCEL")
                {
                    HandleRTDataCancelRequest(timeout);
                }
            }

            //clear the socket and context and say it's not running any more
            _reqSocket.Dispose();

            ms.Dispose();
            ServerRunning = false;
        }

        /// <summary>
        /// Cancel a real time data stream and clean up after it.
        /// </summary>
        /// <returns>True if the stream was canceled, False if subsribers remain.</returns>
        private bool CancelRTDStream(int instrumentID)
        {
            //make sure there is a data stream for this instrument
            if (ActiveStreams.Collection.Any(x => x.Instrument.ID == instrumentID))
            {
                var streamInfo = ActiveStreams.Collection.First(x => x.Instrument.ID == instrumentID);
                var instrument = streamInfo.Instrument;

                //if it's a continuous future we also need to cancel the actual contract
                if (instrument.IsContinuousFuture)
                {
                    var contractID = _continuousFuturesIDMap[instrumentID];
                    var contract = ActiveStreams.Collection.First(x => x.Instrument.ID == contractID).Instrument;

                    //we must also clear the alias list
                    lock (_aliasLock)
                    {
                        _aliases[instrument.Symbol].Remove(contract.Symbol);
                        if (_aliases[instrument.Symbol].Count == 0)
                        {
                            _aliases.Remove(instrument.Symbol);
                        }
                    }

                    //finally cancel the contract's stream
                    CancelRTDStream(contractID);
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
            }

            return false;
        }

        // Accept a request to cancel a real time data stream
        // Obviously we only actually cancel it if 
        private void HandleRTDataCancelRequest(TimeSpan timeout)
        {
            int receivedBytes;
            byte[] buffer = _reqSocket.Receive(null, timeout, out receivedBytes);
            if (receivedBytes <= 0) return;


            //receive the instrument
            var ms = new MemoryStream();
            ms.Write(buffer, 0, receivedBytes);
            ms.Position = 0;
            var instrument = Serializer.Deserialize<Instrument>(ms);

            if (instrument.ID != null) 
                CancelRTDStream(instrument.ID.Value);

            //two part message: 
            //1: "CANCELED"
            //2: the symbol
            _reqSocket.SendMore("CANCELED", Encoding.UTF8);
            _reqSocket.Send(instrument.Symbol, Encoding.UTF8);
        }

        // Accept a real time data request
        private void HandleRTDataRequest(TimeSpan timeout, MemoryStream ms)
        {
            int receivedBytes;
            byte[] buffer = _reqSocket.Receive(null, timeout, out receivedBytes);
            if (receivedBytes <= 0) return;

            ms.Write(buffer, 0, receivedBytes);
            ms.Position = 0;
            var request = Serializer.Deserialize<RealTimeDataRequest>(ms);

            //with the current approach we can't handle multiple real time data streams from
            //the same symbol and data source, but at different frequencies

            //if there is already an active stream of this instrument
            if (ActiveStreams.Collection.Any(x => x.Instrument.ID == request.Instrument.ID))
            {
                IncrementSubscriberCount(request.Instrument);

                //log the request
                Log(LogLevel.Info,
                    string.Format("RTD Request for existing stream: {0} from {1} @ {2}",
                        request.Instrument.Symbol,
                        request.Instrument.Datasource.Name,
                        Enum.GetName(typeof(BarSize), request.Frequency)));

                //and report success back to the requesting client
                _reqSocket.SendMore("SUCCESS", Encoding.UTF8);
                //along with the symbol of the instrument
                _reqSocket.Send(request.Instrument.Symbol, Encoding.UTF8);
            }
            else if (DataSources.ContainsKey(request.Instrument.Datasource.Name) && //make sure the datasource is present & connected
                     DataSources[request.Instrument.Datasource.Name].Connected)
            {
                if (request.Instrument.IsContinuousFuture)
                {
                    //if it's a CF, we need to find which contract is currently "used"
                    //and request that one
                    lock (_cfRequestLock)
                    {
                        _pendingCFRealTimeRequests.Add(_cfBroker.RequestFrontContract(request.Instrument), request);
                    }

                    //the asynchronous nature of the request for the front month creates a lot of problems
                    //we either have to abandon the REP socket and use something asynchronous there
                    //which creates a ton of problems (we need unique IDs for every request and so forth)
                    //or we send back "success" without actually knowing if the request for the
                    //continuous futures real time data was successful or not!
                    //For now I have chosen the latter approach.
                }
                else //NOT a continuous future, just a normal instrument: do standard request procedure
                {
                    ForwardRTDRequest(request);
                }

                //and report success back to the requesting client
                _reqSocket.SendMore("SUCCESS", Encoding.UTF8);
                //along with the symbol of the instrument
                _reqSocket.Send(request.Instrument.Symbol, Encoding.UTF8);
            }
            else //no new request was made, send the client the reason why
            {
                _reqSocket.SendMore("ERROR", Encoding.UTF8);
                if (!DataSources.ContainsKey(request.Instrument.Datasource.Name))
                {
                    _reqSocket.Send("No such data source.", Encoding.UTF8);
                }
                else if (!DataSources[request.Instrument.Datasource.Name].Connected)
                {
                    _reqSocket.Send("Data source not connected.", Encoding.UTF8);
                }
            }
        }

        /// <summary>
        /// Increments the number of subscribers to a real time data stream by 1.
        /// </summary>
        private void IncrementSubscriberCount(Instrument instrument)
        {
            //Find the KeyValuePair<string, int> from the dictionary that corresponds to this instrument
            //The KVP consists of key: request ID, value: data source name
            var streamInfo = ActiveStreams.Collection.First(x => x.Instrument.ID == instrument.ID);

            //increment the subsriber count
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

        /// <summary>
        /// Sends a real time data request to the correct data source, logs it, and updates subscriber counts
        /// </summary>
        /// <param name="request"></param>
        private void ForwardRTDRequest(RealTimeDataRequest request)
        {
            //send the request to the correct data source
            int reqID = DataSources[request.Instrument.Datasource.Name].RequestRealTimeData(request);

            //log the request
            Log(LogLevel.Info,
                string.Format("RTD Request: {0} from {1} @ {2} ID:{3}",
                    request.Instrument.Symbol,
                    request.Instrument.Datasource.Name,
                    Enum.GetName(typeof(BarSize), request.Frequency),
                    reqID));

            //add the request to the active streams, though it's not necessarily active yet
            var streamInfo = new RealTimeStreamInfo(
                request.Instrument,
                reqID,
                request.Instrument.Datasource.Name,
                request.Frequency,
                request.RTHOnly);
            ActiveStreams.TryAdd(streamInfo);

            lock (_subscriberCountLock)
            {
                StreamSubscribersCount.Add(streamInfo, 1);
            }
        }

        /// <summary>
        /// This method is called when the continuous futures broker returns the results of a request for the "front"
        /// contract of a continuous futures instrument.
        /// </summary>
        void _cfBroker_FoundFrontContract(object sender, FoundFrontContractEventArgs e)
        {
            RealTimeDataRequest request;
            
            //grab the original request
            lock (_cfRequestLock)
            {
                request = _pendingCFRealTimeRequests[e.ID];
                _pendingCFRealTimeRequests.Remove(e.ID);
            }

            //add the contract to the ID map
            if (request.Instrument.ID.HasValue &&
                !_continuousFuturesIDMap.ContainsKey(request.Instrument.ID.Value) &&
                e.Instrument.ID.HasValue)
            {
                _continuousFuturesIDMap.Add(request.Instrument.ID.Value, e.Instrument.ID.Value);
            }

            //add the alias
            lock (_aliasLock)
            {
                string contract = e.Instrument.Symbol;
                if (!_aliases.ContainsKey(contract))
                {
                    _aliases.Add(contract, new List<string>());
                }
                _aliases[contract].Add(request.Instrument.Symbol);
            }

            //need to check if there's already a stream of the contract....
            if (ActiveStreams.Collection.Any(x => x.Instrument.ID == e.Instrument.ID))
            {
                //all we need to do in this case is increment the number of subscribers to this stream
                IncrementSubscriberCount(e.Instrument);

                //log it
                Log(LogLevel.Info,
                    string.Format("RTD Request for CF {0} @ {1} {2}, filled by existing stream of symbol {3}.",
                        request.Instrument.Symbol,
                        request.Instrument.Datasource.Name,
                        Enum.GetName(typeof(BarSize), request.Frequency),
                        e.Instrument.Symbol));
            }
            else //no current stream of this contract, add it
            {
                //make the request
                var contractRequest = (RealTimeDataRequest)request.Clone();
                contractRequest.Instrument = e.Instrument; //we take the original request, and substitute the CF for the front contract
                ForwardRTDRequest(contractRequest);

                //add the request to the active streams, though it's not necessarily active yet
                var streamInfo = new RealTimeStreamInfo(
                    request.Instrument,
                    -1,
                    request.Instrument.Datasource.Name,
                    request.Frequency,
                    request.RTHOnly);
                ActiveStreams.TryAdd(streamInfo);

                lock (_subscriberCountLock)
                {
                    StreamSubscribersCount.Add(streamInfo, 1);
                }

                //log it
                Log(LogLevel.Info,
                    string.Format("RTD Request for CF: {0} from {1} @ {2}, filled by contract: {3}",
                        request.Instrument.Symbol,
                        request.Instrument.Datasource.Name,
                        Enum.GetName(typeof(BarSize), request.Frequency),
                        e.Instrument.Symbol));
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
        /// There is a timer which periodically calls the tryconnect function to connect to any disconnected data sources
        /// </summary>
        private void ConnectionTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(TryConnect);
        }

        /// <summary>
        /// Loops through data sources and tries to connect to those that are disconnected
        /// </summary>
        private void TryConnect()
        {
            foreach (var s in DataSources)
            {
                if (!s.Value.Connected)
                {
                    Log(LogLevel.Info, string.Format("Real Time Data Broker: Trying to connect to data source {0}", s.Key));
                    s.Value.Connect();
                }
            }
        }

    }
}
