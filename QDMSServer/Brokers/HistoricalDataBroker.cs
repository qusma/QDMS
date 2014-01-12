// -----------------------------------------------------------------------
// <copyright file="HistoricalDataBroker.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using NLog;
using QDMS;
using QDMSServer.DataSources;
using Timer = System.Timers.Timer;

namespace QDMSServer
{
    public class HistoricalDataBroker : IDisposable, IHistoricalDataBroker
    {
        /// <summary>
        /// Holds the real time data sources.
        /// </summary>
        public Dictionary<string, IHistoricalDataSource> DataSources { get; private set; }

        private readonly IDataStorage _dataStorage;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Timer _connectionTimer; //tries to reconnect every once in a while if a datasource is disconnected
        
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;

        /// <summary>
        /// Keeps track of IDs assigned to requests that have already been used, so there are no duplicates.
        /// </summary>
        private List<int> _usedIDs;
        
        private readonly object _localStorageLock = new object();

        //when we get a new data request first we check the local storage
        //if only part of the data is there, we create subrequests to the outside storages, they are held
        //in _subRequests. When they have all arrived, we grab what we need from the local storage (where
        //new data has been saved) and send it off
        private readonly ConcurrentDictionary<int, HistoricalDataRequest> _originalRequests;

        private readonly ConcurrentDictionary<int, List<HistoricalDataRequest>> _subRequests;

        public void Dispose()
        {

            if (_connectionTimer != null)
            {
                _connectionTimer.Dispose();
                _connectionTimer = null;
            }
            if (DataSources.ContainsKey("Interactive Brokers"))
            {
                ((IB)DataSources["Interactive Brokers"]).Dispose();
            }
            if (DataSources.ContainsKey("ContinuousFuturesBroker"))
            {
                ((ContinuousFuturesBroker)DataSources["ContinuousFuturesBroker"]).Dispose();
            }
        }

        public HistoricalDataBroker(IDataStorage localStorage = null, IEnumerable<IHistoricalDataSource> additionalSources = null)
        {
            _dataStorage = localStorage ?? new MySQLStorage();

            DataSources = new Dictionary<string, IHistoricalDataSource>
            {
                { "Interactive Brokers", new IB(3) },
                { "Yahoo", new Yahoo() },
                { "ContinuousFuturesBroker", new ContinuousFuturesBroker() },
                { "Quandl", new Quandl() }
            };

            //add additional sources
            if (additionalSources != null)
            {
                foreach (IHistoricalDataSource ds in additionalSources)
                {
                    if (!DataSources.ContainsKey(ds.Name))
                        DataSources.Add(ds.Name, ds);
                }
            }

            foreach (IHistoricalDataSource ds in DataSources.Values)
            {
                ds.Error += DatasourceError;
                ds.HistoricalDataArrived += ExternalHistoricalDataArrived;
                ds.Disconnected += SourceDisconnects;
            }

            _dataStorage.Error += DatasourceError;
            _dataStorage.HistoricalDataArrived += LocalStorageHistoricalDataArrived;

            _connectionTimer = new Timer(10000);
            _connectionTimer.Elapsed += ConnectionTimerElapsed;
            _connectionTimer.Start();

            
            _originalRequests = new ConcurrentDictionary<int, HistoricalDataRequest>();
            _subRequests = new ConcurrentDictionary<int, List<HistoricalDataRequest>>();
            _usedIDs = new List<int>();

            TryConnect();
        }

        /// <summary>
        /// This method is called when a data source disconnects
        /// </summary>
        private void SourceDisconnects(object sender, DataSourceDisconnectEventArgs e)
        {
            Log(LogLevel.Info, string.Format("Historical Data Broker: Data source {0} disconnected", e.SourceName));
        }

        /// <summary>
        /// Add a message to the log.
        ///</summary>
        private void Log(LogLevel level, string message)
        {
            if(Application.Current != null)
                Application.Current.Dispatcher.InvokeAsync(() =>
                    _logger.Log(level, message));
        }

        /// <summary>
        /// This method handles data arrivals from the local database
        ///</summary>
        private void LocalStorageHistoricalDataArrived(object sender, HistoricalDataEventArgs e)
        {
            Log(LogLevel.Info, string.Format("Pulled {0} data points from local storage on instrument {1}.",
                e.Data.Count,
                e.Request.Instrument.Symbol));

            //pass up the data to the server so it can be sent out
            RaiseEvent(HistoricalDataArrived, this, e);
        }

        ///<summary>
        /// Raise the event in a threadsafe manner
        ///</summary>
        ///<param name="event"></param>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        ///<typeparam name="T"></typeparam>
        static private void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
        where T : EventArgs
        {
            EventHandler<T> handler = @event;
            if (handler == null) return;
            handler(sender, e);
        }

        /// <summary>
        /// This one handles data arrivals from historical data sources other than local storage
        /// </summary>
        private void ExternalHistoricalDataArrived(object sender, HistoricalDataEventArgs e)
        {
            //save the data to local storage, then (maybe) send the original request to be fulfilled by local storage, which now has all the data available
            HistoricalDataRequest originalRequest;
            int assignedID = e.Request.IsSubrequestFor.HasValue ? e.Request.IsSubrequestFor.Value : e.Request.AssignedID;
            bool gotOriginalRequest = _originalRequests.TryGetValue(assignedID, out originalRequest);
            if (!gotOriginalRequest)
                throw new Exception("Something went wrong: original request disappeared");

            if (e.Request.SaveDataToStorage) //the request asked to save newly arrived data in local storage
            {
                bool doAdjustment = e.Data.Any(x => x.Split.HasValue || x.Dividend.HasValue);
                lock (_localStorageLock)
                {
                    _dataStorage.AddData(e.Data, e.Request.Instrument, e.Request.Frequency, overwrite:true, adjust: doAdjustment);
                }

                //check if there is a list in the subrequests for this request...
                if (e.Request.IsSubrequestFor.HasValue && _subRequests.ContainsKey(e.Request.IsSubrequestFor.Value))
                {
                    //remove this item...if the list is empty, remove it and send the original request to the
                    //local storage, because all the requests to external data sources have now arrived
                    _subRequests[e.Request.IsSubrequestFor.Value].Remove(e.Request);
                    if (_subRequests[e.Request.IsSubrequestFor.Value].Count == 0)
                    {
                        List<HistoricalDataRequest> tmpList;
                        _subRequests.TryRemove(e.Request.IsSubrequestFor.Value, out tmpList); //remove the list for this ID

                        _dataStorage.RequestHistoricalData(originalRequest); //and finally send the original request to the local db
                    }
                }
                else //there is not -- this is a standalone request, so just grab the data from the db and return it
                {
                    _dataStorage.RequestHistoricalData(originalRequest);
                }

                Log(LogLevel.Info, string.Format("Pulled {0} data points from source {1} on instrument {2}.",
                    e.Data.Count,
                    e.Request.Instrument.Datasource.Name,
                    e.Request.Instrument.Symbol));
            }
            else //the data does NOT go to local storage, so we have to load that stuff and combine it right here
            {
                lock (_localStorageLock)
                {
                    //grab the rest of the data from historical storage
                    var storageData = new List<OHLCBar>();
                    if (e.Data.Count > 0 && e.Data[0].Date.ToDateTime() > originalRequest.StartingDate)
                    {
                        //we add half a bar to the request limit so that the data we get starts with the next one
                        DateTime correctedDateTime = e.Data[0].Date.Date.ToDateTime().AddMilliseconds(originalRequest.Frequency.ToTimeSpan().TotalMilliseconds / 2);
                        storageData = _dataStorage.GetData(originalRequest.Instrument, originalRequest.StartingDate,
                            correctedDateTime, originalRequest.Frequency);
                    }

                    //then send the data to the server through the event, so it can be send out to the client
                    RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(e.Request, storageData.Concat(e.Data).ToList()));

                    Log(LogLevel.Info, string.Format("Pulled {0} data points from source {1} on instrument {2} and {3} points from local storage.",
                        e.Data.Count,
                        e.Request.Instrument.Datasource.Name,
                        e.Request.Instrument.Symbol,
                        storageData.Count));
                }
            }
        }

        /// <summary>
        /// Fires when any of the underlying data sources raise their error event.
        /// </summary>
        private void DatasourceError(object sender, ErrorArgs e)
        {
            Log(LogLevel.Error, string.Format("HDB: {0} - {1}", e.ErrorCode, e.ErrorMessage));
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
            if (!_dataStorage.Connected)
                _dataStorage.Connect();

            foreach (var s in DataSources)
            {
                if (!s.Value.Connected)
                {
                    Log(LogLevel.Info, string.Format("Historical Data Broker: Trying to connect to data source {0}", s.Key));
                    s.Value.Connect();
                }
            }
        }


        /// <summary>
        /// Processes incoming historical data requests.
        /// </summary>
        public void RequestHistoricalData(HistoricalDataRequest request)
        {


            _originalRequests.TryAdd(request.AssignedID, request);

            //request is for fresh data ONLY -- send the request directly to the external data source
            if (request.ForceFreshData)
            {
                ForwardHistoricalRequest(request);
                return;
            }

            //request says to ignore the external data source, just send the request as-is to the local storage
            if (request.LocalStorageOnly)
            {
                lock (_localStorageLock)
                {
                    _dataStorage.RequestHistoricalData(request);
                }
                return;
            }

            //we are allowed to use data from local storage, but want to get any missing data from the external data source
            lock (_localStorageLock)
            {
                if (request.Instrument.ID == null) return;

                //we need to get some detailed info on the data we have available locally
                //check which dates are available in local storage
                var localDataInfo = _dataStorage.GetStorageInfo(request.Instrument.ID.Value, request.Frequency);

                //if the local storage can satisfy the request, send it there immediately
                if (localDataInfo != null
                    && localDataInfo.LatestDate >= request.EndingDate
                    && localDataInfo.EarliestDate <= request.StartingDate)
                {
                    _dataStorage.RequestHistoricalData(request);
                    return;
                }

                //alternatively, we know from the expiration that there exists no new data, so go to local storage
                if (localDataInfo != null && request.Instrument.Expiration.HasValue)
                {
                    //get the right session
                    var dotw = request.Instrument.Expiration.Value.Date.DayOfWeek.ToInt();
                    var session = request.Instrument.Sessions.First(x => (int)x.ClosingDay == dotw && x.IsSessionEnd);

                    //if it exists, use it to set the proper time
                    if (session != null
                        && localDataInfo.LatestDate >= (request.Instrument.Expiration.Value.Date + session.ClosingTime))
                    {
                        _dataStorage.RequestHistoricalData(request);
                        return;
                    }
                }

                //we have no data available at all, send off the request as it is
                if (localDataInfo == null ||
                    (request.Instrument.IsContinuousFuture &&
                        request.Instrument.ContinuousFuture.AdjustmentMode != ContinuousFuturesAdjustmentMode.NoAdjustment))
                {
                    //if the instruemnt is a continuous future and it has ratio or difference adjustment mode
                    //then we can't load data from local storage, because it would screw up the adjustment calcs
                    //therefore in that case we send the entire request, without splitting it up
                    ForwardHistoricalRequest(request);
                }
                else //we have SOME data available, check how it holds up, possibly split up into subrequests
                {
                    _subRequests.TryAdd(request.AssignedID, new List<HistoricalDataRequest>());

                    //earlier data that may be needed
                    HistoricalDataRequest newBackRequest = null;
                    if (localDataInfo.EarliestDate > request.StartingDate)
                    {
                        newBackRequest = (HistoricalDataRequest)request.Clone();
                        newBackRequest.EndingDate = localDataInfo.EarliestDate.AddMilliseconds(-request.Frequency.ToTimeSpan().TotalMilliseconds / 2);
                        newBackRequest.IsSubrequestFor = request.AssignedID;
                        newBackRequest.AssignedID = GetUniqueRequestID();
                        _subRequests[newBackRequest.IsSubrequestFor.Value].Add(newBackRequest);
                    }

                    //later data that may be needed
                    HistoricalDataRequest newForwardRequest = null;
                    if (localDataInfo.LatestDate < request.EndingDate)
                    {
                        //the local storage is insufficient, so we save the original request, make a copy,
                        //modify it, and pass it to the external data source
                        newForwardRequest = (HistoricalDataRequest)request.Clone();
                        newForwardRequest.StartingDate = localDataInfo.LatestDate.AddMilliseconds(request.Frequency.ToTimeSpan().TotalMilliseconds / 2);
                        newForwardRequest.IsSubrequestFor = request.AssignedID;
                        newForwardRequest.AssignedID = GetUniqueRequestID();
                        _subRequests[newForwardRequest.IsSubrequestFor.Value].Add(newForwardRequest);
                    }

                    //we send these together, because too large of a delay between the two requests can cause problems
                    if (newBackRequest != null)
                    {
                        ForwardHistoricalRequest(newBackRequest);
                    }
                    if (newForwardRequest != null)
                    {
                        ForwardHistoricalRequest(newForwardRequest);
                    }
                }
            }
        }

        //Sends off a historical data reques to the datasource that needs to fulfill it
        private void ForwardHistoricalRequest(HistoricalDataRequest request)
        {
            if (request.Instrument.IsContinuousFuture)
            {
                DataSources["ContinuousFuturesBroker"].RequestHistoricalData(request);
            }
            else
            {
                DataSources[request.Instrument.Datasource.Name].RequestHistoricalData(request);
            }
        }

        /// <summary>
        /// Gets a new unique AssignedID to identify requests with.
        /// </summary>
        private int GetUniqueRequestID()
        {
            //requests can arrive very close to each other and thus have the same seed, so we need to make sure it's unique
            var rand = new Random();
            int id;
            do
            {
                id = rand.Next(1, int.MaxValue);
            } while (_usedIDs.Contains(id));

            _usedIDs.Add(id);
            return id;
        }

        /// <summary>
        /// Forwards a data addition request to local storage.
        /// </summary>
        public void AddData(DataAdditionRequest request)
        {
            lock (_localStorageLock)
            {
                _dataStorage.AddData(request.Data, request.Instrument, request.Frequency, request.Overwrite);
            }
        }

        public List<StoredDataInfo> GetAvailableDataInfo(Instrument instrument)
        {
            if (instrument.ID == null)
            {
                Log(LogLevel.Info, "Request for available data on instrument without ID.");
                return new List<StoredDataInfo>();
            }

            return _dataStorage.GetStorageInfo(instrument.ID.Value);
        }
    }
}