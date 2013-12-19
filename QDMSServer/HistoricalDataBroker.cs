// -----------------------------------------------------------------------
// <copyright file="HistoricalDataBroker.cs" company="">
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
using LZ4;
using NLog;
using ProtoBuf;
using QDMS;
using QDMSServer.DataSources;
using ZeroMQ;
using Timer = System.Timers.Timer;

namespace QDMSServer
{
    public class HistoricalDataBroker : IDisposable
    {
        /// <summary>
        /// Holds the real time data sources.
        /// </summary>
        public Dictionary<string, IHistoricalDataSource> DataSources { get; private set; }

        /// <summary>
        /// Whether the broker is running or not.
        /// </summary>
        public bool ServerRunning { get; set; }

        private readonly IDataStorage _dataStorage;

        private readonly Thread _serverThread;
        private ZmqContext _context;
        private ZmqSocket _routerSocket;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private Timer _connectionTimer; //tries to reconnect every once in a while if a datasource is disconnected
        private bool _runServer = true;

        private readonly object _identityMapLock = new object();
        private readonly object _localStorageLock = new object();

        private readonly int _listenPort;

        //when we get a new data request first we check the local storage
        //if only part of the data is there, we create subrequests to the outside storages, they are held
        //in _subRequests. When they have all arrived, we grab what we need from the local storage (where
        //new data has been saved) and send it off
        private readonly ConcurrentDictionary<int, HistoricalDataRequest> _originalRequests;

        private readonly ConcurrentDictionary<int, List<HistoricalDataRequest>> _subRequests;

        private readonly ConcurrentQueue<KeyValuePair<int, List<OHLCBar>>> _dataQueue;

        //requests are given an int ID that uniquely identifies them
        //this is then paired to the client's identity
        //when the data arrives, we then know where to route the results by looking up the ID in this Dictionary
        private readonly Dictionary<int, string> _requestToIdentityMap;

        public void Dispose()
        {
            if (_routerSocket != null)
            {
                _routerSocket.Dispose();
                _routerSocket = null;
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
            if (DataSources.ContainsKey("ContinuousFuturesBroker"))
            {
                ((ContinuousFuturesBroker)DataSources["ContinuousFuturesBroker"]).Dispose();
            }
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        public HistoricalDataBroker(int port)
        {
            _listenPort = port;

            _dataStorage = new MySQLStorage();
            DataSources = new Dictionary<string, IHistoricalDataSource> 
            {
                { "Interactive Brokers", new IB(3) },
                { "Yahoo", new Yahoo() },
                { "ContinuousFuturesBroker", new ContinuousFuturesBroker() }
            };

            foreach (IHistoricalDataSource ds in DataSources.Values)
            {
                ds.Error += DatasourceError;
                ds.HistoricalDataArrived += ExternalHistoricalDataArrived;
                ds.Disconnected += SourceDisconnects;
            }

            _dataStorage.Error += DatasourceError;
            _dataStorage.HistoricalDataArrived += LocalStorageHistoricalDataArrived;

            _serverThread = new Thread(Server);
            _serverThread.Name = "HDB Thread";

            _connectionTimer = new Timer(10000);
            _connectionTimer.Elapsed += ConnectionTimerElapsed;
            _connectionTimer.Start();

            _requestToIdentityMap = new Dictionary<int, string>();
            _originalRequests = new ConcurrentDictionary<int, HistoricalDataRequest>();
            _subRequests = new ConcurrentDictionary<int, List<HistoricalDataRequest>>();
            _dataQueue = new ConcurrentQueue<KeyValuePair<int, List<OHLCBar>>>();

            TryConnect();

            StartServer();
        }

        /// <summary>
        /// This method is called when a data source disconnects
        /// </summary>
        private void SourceDisconnects(object sender, DataSourceDisconnectEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                Log(LogLevel.Info, string.Format("Real Time Data Broker: Data source {0} disconnected", e.SourceName))
            );
        }

        /// <summary>
        /// Add a message to the log.
        ///</summary>
        private void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }

        /// <summary>
        /// This method handles data arrivals from the local database
        ///</summary>
        private void LocalStorageHistoricalDataArrived(object sender, HistoricalDataEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                Log(LogLevel.Info, string.Format("Pulled {0} data points from local storage on instrument {1}.",
                e.Data.Count,
                e.Request.Instrument.Symbol))
            );


            //then add the data to the queue to be sent out over the network
            _dataQueue.Enqueue(new KeyValuePair<int, List<OHLCBar>>(e.Request.AssignedID, e.Data));
        }

        /// <summary>
        /// This one handles data arrivals from historical data sources other than local storage
        /// </summary>
        private void ExternalHistoricalDataArrived(object sender, HistoricalDataEventArgs e)
        {
            //save the data to local storage, then (maybe) send the original request to be fulfilled by local storage, which now has all the data available
            HistoricalDataRequest originalRequest;
            bool gotOriginalRequest = _originalRequests.TryGetValue(e.Request.AssignedID, out originalRequest);
            if (!gotOriginalRequest)
                throw new Exception("Something went wrong: original request disappeared");

            if (e.Request.SaveDataToStorage) //the request asked to save newly arrived data in local storage
            {
                lock (_localStorageLock)
                {
                    _dataStorage.AddData(e.Data, e.Request.Instrument, e.Request.Frequency, true);
                }

                //check if there is a list in the subrequests for this request...
                if (_subRequests.ContainsKey(e.Request.AssignedID))
                {
                    //remove this item...if the list is empty, remove it and send the original request to the
                    //local storage, because all the requests to external data sources have now arrived
                    _subRequests[e.Request.AssignedID].Remove(e.Request);
                    if (_subRequests[e.Request.AssignedID].Count == 0)
                    {
                        List<HistoricalDataRequest> tmpList;
                        _subRequests.TryRemove(e.Request.AssignedID, out tmpList); //remove the list for this ID

                        _dataStorage.RequestHistoricalData(originalRequest); //and finally send the original request to the local db
                    }
                }
                else //there is not -- this is a standalone request, so just grab the data from the db and return it
                {
                    _dataStorage.RequestHistoricalData(originalRequest);
                }


                Application.Current.Dispatcher.Invoke(() =>
                    Log(LogLevel.Info, string.Format("Pulled {0} data points from source {1} on instrument {2}.",
                    e.Data.Count,
                    e.Request.Instrument.Datasource.Name,
                    e.Request.Instrument.Symbol))
                );
            }
            else //the data does NOT go to local storage, so we have to load that stuff and combine it right here
            {
                lock (_localStorageLock)
                {
                    //grab the rest of the data from historical storage
                    var storageData = new List<OHLCBar>();
                    if (e.Data[0].Date.ToDateTime() > originalRequest.StartingDate)
                    {
                        //we add half a bar to the request limit so that the data we get starts with the next one
                        DateTime correctedDateTime = e.Data[0].Date.Date.ToDateTime().AddMilliseconds(originalRequest.Frequency.ToTimeSpan().TotalMilliseconds / 2);
                        storageData = _dataStorage.GetData(originalRequest.Instrument, originalRequest.StartingDate,
                            correctedDateTime, originalRequest.Frequency);
                    }

                    //then add the data to the queue to be sent out over the network
                    _dataQueue.Enqueue(new KeyValuePair<int, List<OHLCBar>>(e.Request.AssignedID, storageData.Concat(e.Data).ToList()));

                    Application.Current.Dispatcher.Invoke(() =>
                        Log(LogLevel.Info, string.Format("Pulled {0} data points from source {1} on instrument {2} and {3} points from local storage.",
                        e.Data.Count,
                        e.Request.Instrument.Datasource.Name,
                        e.Request.Instrument.Symbol,
                        storageData.Count))
                    );
                }
            }
        }

        /// <summary>
        /// Fires when any of the underlying data sources raise their error event.
        /// </summary>
        private void DatasourceError(object sender, ErrorArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
                Log(LogLevel.Error, string.Format("HDB: {0} - {1}", e.ErrorCode, e.ErrorMessage))
            );
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
            if(!_dataStorage.Connected)
                _dataStorage.Connect();

            foreach (var s in DataSources)
            {
                if (!s.Value.Connected)
                {
                    _logger.Log(LogLevel.Info, string.Format("Historical Data Broker: Trying to connect to data source {0}", s.Key));
                    s.Value.Connect();
                }
            }
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void StartServer()
        {
            //check that it's not already running
            if (ServerRunning) return;
            _context = ZmqContext.Create();
            _routerSocket = _context.CreateSocket(SocketType.ROUTER);
            _routerSocket.Bind("tcp://*:" + _listenPort);

            _runServer = true;
            _serverThread.Start();
            ServerRunning = true;
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void StopServer()
        {
            if (!ServerRunning) return;

            _runServer = false;
            //if(_serverThread.ThreadState == ThreadState.Running)
                _serverThread.Join();
        }

        /// <summary>
        /// Receives new requests by polling, and sends data when it has arrived
        /// </summary>
        public void Server()
        {
            var timeout = TimeSpan.FromMilliseconds(10);
            _routerSocket.ReceiveReady += socket_ReceiveReady;
            KeyValuePair<int, List<OHLCBar>> newDataItem;
            var ms = new MemoryStream();

            using (var poller = new Poller(new[] { _routerSocket }))
            {
                while (_runServer)
                {
                    poller.Poll(timeout); //this'll trigger the ReceiveReady event when we've got an incoming request

                    //check if there's anything in the queue, if there is we want to send it
                    if (_dataQueue.TryDequeue(out newDataItem))
                    {
                        //this is a 4 part message
                        //1st message part: the identity string of the client that we're routing the data to
                        string clientIdentity;
                        lock (_identityMapLock)
                        {
                            clientIdentity = _requestToIdentityMap[newDataItem.Key];
                            _requestToIdentityMap.Remove(newDataItem.Key);
                        }
                        _routerSocket.SendMore(clientIdentity, Encoding.UTF8);

                        //2nd message part: the type of reply we're sending
                        _routerSocket.SendMore("HISTREQREP", Encoding.UTF8);

                        //3rd message part: the HistoricalDataRequest object that was used to make the request
                        HistoricalDataRequest request;
                        _originalRequests.TryRemove(newDataItem.Key, out request);
                        _routerSocket.SendMore(MyUtils.ProtoBufSerialize(request, ms));

                        //4th message part: the size of the uncompressed, serialized data. Necessary for decompression on the client end.
                        byte[] uncompressed = MyUtils.ProtoBufSerialize(newDataItem.Value, ms);
                        _routerSocket.SendMore(BitConverter.GetBytes(uncompressed.Length));

                        //5th message part: the compressed serialized data.
                        byte[] compressed = LZ4Codec.EncodeHC(uncompressed, 0, uncompressed.Length); //compress
                        _routerSocket.Send(compressed);
                    }
                }
            }

            ms.Dispose();
            _routerSocket.Dispose();
            _context.Dispose();
            ServerRunning = false;
        }

        /// <summary>
        /// Processes incoming historical data requests.
        /// </summary>
        private void AcceptHistoricalDataRequest(string requesterIdentity, ZmqSocket socket)
        {
            //third: a serialized HistoricalDataRequest object which contains the details of the request
            int size;
            byte[] buffer = socket.Receive(null, out size);
            if (size <= 0) return; //empty request object

            var ms = new MemoryStream();
            ms.Write(buffer, 0, size);
            ms.Position = 0;
            HistoricalDataRequest request = Serializer.Deserialize<HistoricalDataRequest>(ms);

            //give the request an ID that we can use to track it
            var rand = new Random();
            request.AssignedID = rand.Next(1, int.MaxValue);

            //log the request
            Application.Current.Dispatcher.Invoke(() =>
                Log(LogLevel.Info, string.Format("Historical Data Request from client {0}: {8} {1} @ {2} from {3} to {4} {5:;;ForceFresh} {6:;;LocalOnly} {7:;;SaveToLocal}",
                requesterIdentity,
                request.Instrument.Symbol,
                Enum.GetName(typeof(BarSize), request.Frequency),
                request.StartingDate,
                request.EndingDate,
                request.ForceFreshData ? 0 : 1,
                request.LocalStorageOnly ? 0 : 1,
                request.SaveDataToStorage ? 0 : 1,
                request.Instrument.Datasource.Name))
            );

            //we have the identity of the sender and their request, now we add them to our request id -> identity map
            lock (_identityMapLock)
            {
                _requestToIdentityMap.Add(request.AssignedID, requesterIdentity);
            }

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
                if(localDataInfo != null && request.Instrument.Expiration.HasValue)
                {
                    //get the right session
                    var dotw = request.Instrument.Expiration.Value.Date.DayOfWeek.ToInt();
                    var session = request.Instrument.Sessions.First(x => (int)x.ClosingDay == dotw && x.IsSessionEnd);

                    //if it exists, use it to set the proper time
                    if(session != null
                        && localDataInfo.LatestDate >= (request.Instrument.Expiration.Value.Date + session.ClosingTime))
                    {
                        _dataStorage.RequestHistoricalData(request);
                        return;
                    }
                }

                //we have no data available at all, send off the request as it is
                if (localDataInfo == null)
                {
                    ForwardHistoricalRequest(request);
                }
                else //we have SOME data available, check how it holds up
                {
                    _subRequests.TryAdd(request.AssignedID, new List<HistoricalDataRequest>());

                    //earlier data that may be needed
                    HistoricalDataRequest newBackRequest = null;
                    if (localDataInfo.EarliestDate > request.StartingDate)
                    {
                        newBackRequest = (HistoricalDataRequest)request.Clone();
                        newBackRequest.EndingDate = localDataInfo.EarliestDate.AddMilliseconds(-request.Frequency.ToTimeSpan().TotalMilliseconds / 2);
                        _subRequests[request.AssignedID].Add(newBackRequest);
                        
                    }

                    //later data that may be needed
                    HistoricalDataRequest newForwardRequest = null;
                    if (localDataInfo.LatestDate < request.EndingDate)
                    {
                        //the local storage is insufficient, so we save the original request, make a copy, 
                        //modify it, and pass it to the external data source
                        newForwardRequest = (HistoricalDataRequest)request.Clone();
                        newForwardRequest.StartingDate = localDataInfo.LatestDate.AddMilliseconds(request.Frequency.ToTimeSpan().TotalMilliseconds / 2);
                        _subRequests[request.AssignedID].Add(newForwardRequest);
                    }

                    //we send these together, because too large of a delay between the two requests can cause problems
                    if(newBackRequest != null)
                        ForwardHistoricalRequest(newBackRequest);
                    if (newForwardRequest != null)
                        ForwardHistoricalRequest(newForwardRequest);
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
        /// Handles incoming data "push" requests: the client sends data for us to add to local storage.
        /// </summary>
        private void AcceptDataAdditionRequest(string requesterIdentity, ZmqSocket socket)
        {
            //final message part: receive the DataAdditionRequest object
            int size;
            var ms = new MemoryStream();
            byte[] buffer = socket.Receive(null, TimeSpan.FromMilliseconds(10), out size);
            if (size <= 0) return;

            var request = MyUtils.ProtoBufDeserialize<DataAdditionRequest>(buffer, ms);

            //log the request
            Application.Current.Dispatcher.Invoke(() =>
                _logger.Log(LogLevel.Info, string.Format("Received data push request for {0}.",
                request.Instrument.Symbol)));

            //start building the reply
            socket.SendMore(requesterIdentity, Encoding.UTF8);
            socket.SendMore("PUSHREP", Encoding.UTF8);
            try
            {
                lock (_localStorageLock)
                {
                    _dataStorage.AddData(request.Data, request.Instrument, request.Frequency, request.Overwrite);
                }
                socket.Send("OK", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                socket.SendMore("ERROR", Encoding.UTF8);
                socket.Send(ex.Message, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Handles requests for information on data that is available in local storage
        /// </summary>
        private void AcceptAvailableDataRequest(string requesterIdentity, ZmqSocket socket)
        {
            //get the instrument
            int size;
            var ms = new MemoryStream();
            byte[] buffer = socket.Receive(null, TimeSpan.FromMilliseconds(10), out size);
            if (size <= 0) return;

            var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

            //log the request
            Application.Current.Dispatcher.Invoke(() =>
                _logger.Log(LogLevel.Info, string.Format("Received local data storage info request for {0}.",
                instrument.Symbol)));


            //and send the reply
            lock (_localStorageLock)
            {
                List<StoredDataInfo> storageInfo;
                if (instrument.ID != null) storageInfo = _dataStorage.GetStorageInfo(instrument.ID.Value);
                else return;

                socket.SendMore(requesterIdentity, Encoding.UTF8);
                socket.SendMore("AVAILABLEDATAREP", Encoding.UTF8);

                socket.SendMore(MyUtils.ProtoBufSerialize(instrument, ms));

                socket.SendMore(BitConverter.GetBytes(storageInfo.Count));
                foreach (StoredDataInfo sdi in storageInfo)
                {
                    socket.SendMore(MyUtils.ProtoBufSerialize(sdi, ms));
                }
                socket.Send("END", Encoding.UTF8);
            }
        }

        /// <summary>
        /// This is called when a new historical data request or data push request is made.
        /// </summary>
        void socket_ReceiveReady(object sender, SocketEventArgs e)
        {
            //Here we process the first two message parts: first, the identity string of the client
            string requesterIdentity = e.Socket.Receive(Encoding.UTF8);

            //second: the string specifying the type of request
            string text = e.Socket.Receive(Encoding.UTF8);
            if (text == "HISTREQ") //the client wants to request some data
            {
                AcceptHistoricalDataRequest(requesterIdentity, e.Socket);
            }
            else if (text == "HISTPUSH") //the client wants to push same data into the db
            {
                AcceptDataAdditionRequest(requesterIdentity, e.Socket);
            }
            else if (text == "AVAILABLEDATAREQ") //client wants to know what kind of data we have stored locally
            {
                AcceptAvailableDataRequest(requesterIdentity, e.Socket);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                    _logger.Log(LogLevel.Info, "Unrecognized request to historical data broker: " + text));
            }
        }
    }
}
