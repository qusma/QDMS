// -----------------------------------------------------------------------
// <copyright file="QDMSClient.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;
using LZ4;
using NetMQ.Sockets;
using NetMQ.zmq;
using ProtoBuf;
using QDMS;
using NetMQ;
using Poller = NetMQ.Poller;
using Timer = System.Timers.Timer;

namespace QDMSClient
{
    public class QDMSClient : IDataClient
    {
        /// <summary>
        /// Returns true if the connection to the server is up.
        /// </summary>
        public bool Connected
        {
            get
            {
                return (DateTime.Now - _lastHeartBeat).TotalSeconds < 5;
            }
        }

        /// <summary>
        /// Keeps track of historical requests that have been sent but the data has not been received yet.
        /// </summary>
        public ObservableCollection<HistoricalDataRequest> PendingHistoricalRequests { get; set; }

        /// <summary>
        /// Keeps track of live real time data streams.
        /// </summary>
        public ObservableCollection<RealTimeDataRequest> RealTimeDataStreams { get; set; }

        private NetMQContext _context;

        /// <summary>
        /// This socket sends requests for real time data.
        /// </summary>
        private DealerSocket _reqSocket;

        /// <summary>
        /// This socket receives real time data.
        /// </summary>
        private SubscriberSocket _subSocket;

        /// <summary>
        /// This socket sends requests for and receives historical data.
        /// </summary>
        private NetMQSocket _dealerSocket;

        /// <summary>
        /// Periodically sends heartbeat messages to server to ensure the connection is up.
        /// </summary>
        private Timer _heartBeatTimer;

        /// <summary>
        /// The time when the last heartbeat was received. If it's too long ago we're disconnected.
        /// </summary>
        private DateTime _lastHeartBeat;

        /// <summary>
        /// This thread run the DealerLoop() method.
        /// It sends out requests for historical data, and receives data when requests are fulfilled.
        /// </summary>
        private Thread _dealerLoopThread;


        //Where to connect
        private readonly string _host;

        private readonly int _realTimeRequestPort;
        private readonly int _realTimePublishPort;
        private readonly int _instrumentServerPort;
        private readonly int _historicalDataPort;

        /// <summary>
        /// This holds the zeromq identity string that we'll be using.
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// Queue of historical data requests waiting to be sent out.
        /// </summary>
        private readonly ConcurrentQueue<HistoricalDataRequest> _historicalDataRequests;

        /// <summary>
        /// This int is used to give each historical request a unique RequestID.
        /// Keep in mind this is unique to the CLIENT. AssignedID is unique to the server.
        /// </summary>
        private int _requestCount;

        private readonly object _reqSocketLock = new object();
        private readonly object _dealerSocketLock = new object();
        private readonly object _pendingHistoricalRequestsLock = new object();
        private readonly object _realTimeDataStreamsLock = new object();

        /// <summary>
        /// Used to start and stop the various threads that keep the client running.
        /// </summary>
        private bool _running;

        private Poller _poller;

        public void Dispose()
        {
            Disconnect();

            if (_reqSocket != null)
            {
                _reqSocket.Dispose();
                _reqSocket = null;
            }
            if (_subSocket != null)
            {
                _subSocket.Dispose();
                _subSocket = null;
            }
            if (_dealerSocket != null)
            {
                _dealerSocket.Dispose();
                _dealerSocket = null;
            }
            if (_heartBeatTimer != null)
            {
                _heartBeatTimer.Dispose();
                _heartBeatTimer = null;
            }
            //The context must be disposed of last! It will hang if the sockets have not been disposed.
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="clientName">The name of this client. Should be unique. Used to route historical data.</param>
        /// <param name="host">The address of the server.</param>
        /// <param name="realTimeRequestPort">The port used for real time data requsts.</param>
        /// <param name="realTimePublishPort">The port used for publishing new real time data.</param>
        /// <param name="instrumentServerPort">The port used by the instruments server.</param>
        /// <param name="historicalDataPort">The port used for historical data.</param>
        public QDMSClient(string clientName, string host, int realTimeRequestPort, int realTimePublishPort, int instrumentServerPort, int historicalDataPort)
        {
            _host = host;
            _name = clientName;
            _realTimeRequestPort = realTimeRequestPort;
            _realTimePublishPort = realTimePublishPort;
            _instrumentServerPort = instrumentServerPort;
            _historicalDataPort = historicalDataPort;


            _historicalDataRequests = new ConcurrentQueue<HistoricalDataRequest>();
            PendingHistoricalRequests = new ObservableCollection<HistoricalDataRequest>();
            RealTimeDataStreams = new ObservableCollection<RealTimeDataRequest>();
        }

        /// <summary>
        /// Pushes data to local storage.
        /// </summary>
        public void PushData(DataAdditionRequest request)
        {
            if (request.Instrument == null || request.Instrument.ID == null)
            {
                RaiseEvent(Error, null, new ErrorArgs(-1, "Instrument must be set and have an ID."));
                return;
            }

            var ms = new MemoryStream();

            lock (_dealerSocketLock)
            {
                _dealerSocket.SendMoreFrame("HISTPUSH");
                _dealerSocket.SendFrame(MyUtils.ProtoBufSerialize(request, ms));
            }
        }

        /// <summary>
        /// Requests information on what historical data is available in local storage for this instrument.
        /// </summary>
        public void GetLocallyAvailableDataInfo(Instrument instrument)
        {
            var ms = new MemoryStream();
            lock (_dealerSocketLock)
            {
                _dealerSocket.SendMoreFrame("AVAILABLEDATAREQ");
                _dealerSocket.SendFrame(MyUtils.ProtoBufSerialize(instrument, ms));
            }
        }

        //The timer sends heartbeat messages so we know that the server is still up.
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            HeartBeat();
        }

        /// <summary>
        /// Request historical data. Data will be delivered through the HistoricalDataReceived event.
        /// </summary>
        /// <returns>An ID uniquely identifying this historical data request. -1 if there was an error.</returns>
        public int RequestHistoricalData(HistoricalDataRequest request)
        {
            //make sure the request is valid
            if (request.EndingDate < request.StartingDate)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Historical Data Request Failed: Starting date must be after ending date."));
                return -1;
            }

            if (request.Instrument == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Historical Data Request Failed: null Instrument."));
                return -1;
            }

            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not request historical data - not connected."));
                return -1;
            }

            if (!request.RTHOnly && request.Frequency >= BarSize.OneDay && request.DataLocation != DataLocation.ExternalOnly)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Warning: Requesting low-frequency data outside RTH should be done with DataLocation = ExternalOnly, data from local storage will be incorrect."));
            }

            request.RequestID = _requestCount++;

            lock (_pendingHistoricalRequestsLock)
            {
                PendingHistoricalRequests.Add(request);
            }

            _historicalDataRequests.Enqueue(request);
            return request.RequestID;
        }

        /// <summary>
        /// Request a new real time data stream. Data will be delivered through the RealTimeDataReceived event.
        /// </summary>
        /// <returns>An ID uniquely identifying this real time data request. -1 if there was an error.</returns>
        public int RequestRealTimeData(RealTimeDataRequest request)
        {
            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not request real time data - not connected."));
                return -1;
            }

            if (request.Instrument == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Real Time Data Request Failed: null Instrument."));
                return -1;
            }

            request.RequestID = _requestCount++;

            lock (_reqSocketLock)
            {
                //two part message:
                //1: "RTD"
                //2: serialized RealTimeDataRequest
                var ms = new MemoryStream();
                _reqSocket.SendMoreFrame("");
                _reqSocket.SendMoreFrame("RTD");
                _reqSocket.SendFrame(MyUtils.ProtoBufSerialize(request, ms));
            }
            return request.RequestID;
        }

        /// <summary>
        /// Tries to connect to the QDMS server.
        /// </summary>
        public void Connect()
        {
            Dispose();

            _context = NetMQContext.Create();
            _reqSocket = _context.CreateDealerSocket();
            _subSocket = _context.CreateSubscriberSocket();
            _dealerSocket = _context.CreateSocket(ZmqSocketType.Dealer);

            _reqSocket.Options.Identity = Encoding.UTF8.GetBytes(_name);
            _subSocket.Options.Identity = Encoding.UTF8.GetBytes(_name);
            _dealerSocket.Options.Identity = Encoding.UTF8.GetBytes(_name);

            _dealerSocket.ReceiveReady += _dealerSocket_ReceiveReady;
            _reqSocket.ReceiveReady += _reqSocket_ReceiveReady;
            _subSocket.ReceiveReady += _subSocket_ReceiveReady;

            _reqSocket.Connect(string.Format("tcp://{0}:{1}", _host, _realTimeRequestPort));

            //start off by sending a ping to make sure everything is regular
            string reply = "";
            try
            {
                _reqSocket.SendMoreFrame("");
                _reqSocket.SendFrame("PING");

                _reqSocket.ReceiveString(TimeSpan.FromSeconds(1)); //empty frame starts the REP message //todo receive string?
                reply = _reqSocket.ReceiveString(TimeSpan.FromMilliseconds(50));
            }
            catch
            {
                Dispose();
            }

            
            if (reply != "PONG") //server didn't reply or replied incorrectly
            {
                _reqSocket.Disconnect(string.Format("tcp://{0}:{1}", _host, _realTimeRequestPort));
                _reqSocket.Close();
                {
                    RaiseEvent(Error, this, new ErrorArgs(-1, "Could not connect to server."));
                    return;
                }
            }

            _lastHeartBeat = DateTime.Now;
            _subSocket.Connect(string.Format("tcp://{0}:{1}", _host, _realTimePublishPort));
            _dealerSocket.Connect(string.Format("tcp://{0}:{1}", _host, _historicalDataPort));

            _running = true;

            //this loop sends out historical data requests and receives the data
            _dealerLoopThread = new Thread(DealerLoop) { Name = "Client Dealer Loop" };
            _dealerLoopThread.Start();

            //this loop takes care of replies to the request socket: heartbeats and data request status messages
            _poller = new Poller();
            _poller.AddSocket(_reqSocket);
            _poller.AddSocket(_subSocket);
            _poller.AddSocket(_dealerSocket);
            Task.Factory.StartNew(_poller.PollTillCancelled, TaskCreationOptions.LongRunning);

            _heartBeatTimer = new Timer(1000);
            _heartBeatTimer.Elapsed += _timer_Elapsed;
            _heartBeatTimer.Start();
        }

        /// <summary>
        /// Process replies on the request socket.
        /// Heartbeats, errors, and subscribing to real time data streams.
        /// </summary>
        private void _reqSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            using (var ms = new MemoryStream())
            {
                lock (_reqSocketLock)
                {
                    string reply = _reqSocket.ReceiveFrameString();

                    if (reply == null) return;

                    reply = _reqSocket.ReceiveFrameString();

                    switch (reply)
                    {
                        case "PONG": //reply to heartbeat message
                            _lastHeartBeat = DateTime.Now;
                            break;

                        case "ERROR": //something went wrong
                        {
                            //first the message
                            string error = _reqSocket.ReceiveFrameString();

                            //then the request
                            byte[] buffer = _reqSocket.ReceiveFrameBytes();
                            var request = MyUtils.ProtoBufDeserialize<RealTimeDataRequest>(buffer, ms);

                            //error event
                            RaiseEvent(Error, this, new ErrorArgs(-1, "Real time data request error: " + error, request.RequestID));
                            return;
                        }
                        case "SUCCESS": //successful request to start a new real time data stream
                        {
                            //receive the request
                            byte[] buffer = _reqSocket.ReceiveFrameBytes();
                            var request = MyUtils.ProtoBufDeserialize<RealTimeDataRequest>(buffer, ms);

                            //Add it to the active streams
                            lock (_realTimeDataStreamsLock)
                            {
                                RealTimeDataStreams.Add(request);
                            }

                            //request worked, so we subscribe to the stream
                            _subSocket.Subscribe(BitConverter.GetBytes(request.Instrument.ID.Value));
                        }
                            break;

                        case "CANCELED": //successful cancelation of a real time data stream
                        {
                            //also receive the symbol
                            string symbol = _reqSocket.ReceiveFrameString();

                            //nothing to do?
                        }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Disconnects from the server.
        ///  </summary>
        public void Disconnect(bool cancelStreams = true)
        {
            //start by canceling all active real time streams
            if (cancelStreams)
            {
                while (RealTimeDataStreams.Count > 0)
                {
                    CancelRealTimeData(RealTimeDataStreams.First().Instrument);
                }
            }

            _running = false;
            if (_poller != null && _poller.IsStarted)
            {
                _poller.CancelAndJoin();
            }

            if(_heartBeatTimer != null)
                _heartBeatTimer.Stop();

            if(_dealerLoopThread != null && _dealerLoopThread.ThreadState == ThreadState.Running)
                _dealerLoopThread.Join(10);

            if (_reqSocket != null)
            {
                try
                {
                    _reqSocket.Disconnect(string.Format("tcp://{0}:{1}", _host, _realTimeRequestPort));
                }
                catch
                {
                    _reqSocket.Dispose();
                    _reqSocket = null;
                }
            }

            if (_subSocket != null)
            {
                try
                {
                    _subSocket.Disconnect(string.Format("tcp://{0}:{1}", _host, _realTimePublishPort));
                }
                catch
                {
                    _subSocket.Dispose();
                    _subSocket = null;
                }
            }

            if (_dealerSocket != null)
            {
                try
                {
                    _dealerSocket.Disconnect(string.Format("tcp://{0}:{1}", _host, _historicalDataPort));
                }
                catch
                {
                    _dealerSocket.Dispose();
                    _dealerSocket = null;
                }
            }
        }

        /// <summary>
        /// Dealer socket sends out requests for historical data and raises an event when it's received
        /// </summary>
        private void DealerLoop()
        {
            var ms = new MemoryStream();
            try
            {
                while (_running)
                {
                    //send any pending historical data requests
                    lock (_dealerSocketLock)
                    {
                        SendQueuedHistoricalRequests(ms);
                    }
                    Thread.Sleep(10);
                }
            }
            catch
            {
                Dispose();
            }
        }

        private void SendQueuedHistoricalRequests(MemoryStream ms)
        {
            while (!_historicalDataRequests.IsEmpty)
            {
                HistoricalDataRequest request;
                if (_historicalDataRequests.TryDequeue(out request))
                {
                    byte[] buffer = MyUtils.ProtoBufSerialize(request, ms);
                    _dealerSocket.SendMoreFrame("HISTREQ");
                    _dealerSocket.SendFrame(buffer);
                }
            }
        }

        private void _subSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            bool hasMore;
            byte[] instrumentID = _subSocket.ReceiveFrameBytes(out hasMore);

            if (hasMore)
            {
                byte[] buffer = _subSocket.ReceiveFrameBytes();
                var bar = MyUtils.ProtoBufDeserialize<RealTimeDataEventArgs>(buffer, new MemoryStream());
                RaiseEvent(RealTimeDataReceived, null, bar);
            }
        }

        /// <summary>
        /// Handling replies to a data push, a historical data request, or an available data request
        /// </summary>
        private void _dealerSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            lock (_dealerSocketLock)
            {
                //1st message part: what kind of stuff we're receiving
                string type = _dealerSocket.ReceiveFrameString();
                switch (type)
                {
                    case "PUSHREP":
                        HandleDataPushReply();
                        break;

                    case "HISTREQREP":
                        HandleHistoricalDataRequestReply();
                        break;

                    case "AVAILABLEDATAREP":
                        HandleAvailabledataReply();
                        break;

                    case "ERROR":
                        HandleErrorReply();
                        break;
                }
            }
        }

        /// <summary>
        /// Called when we get some sort of error reply
        /// </summary>
        private void HandleErrorReply()
        {
            //the request ID
            bool hasMore;
            byte[] buffer = _dealerSocket.ReceiveFrameBytes(out hasMore);
            if (!hasMore) return;
            int requestID = BitConverter.ToInt32(buffer, 0);

            //remove from pending requests
            lock (_pendingHistoricalRequestsLock)
            {
                PendingHistoricalRequests.RemoveAll(x => x.RequestID == requestID);
            }

            //finally the error message
            string message = _dealerSocket.ReceiveFrameString();

            //raise the error event
            RaiseEvent(Error, this, new ErrorArgs(-1, message, requestID));
        }

        /// <summary>
        /// Called when we get a reply on a request for available data in local storage.
        /// </summary>
        private void HandleAvailabledataReply()
        {
            //first the instrument
            var ms = new MemoryStream();
            byte[] buffer = _dealerSocket.ReceiveFrameBytes();
            var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

            //second the number of items
            buffer = _dealerSocket.ReceiveFrameBytes();
            int count = BitConverter.ToInt32(buffer, 0);

            //then actually get the items, if any
            if (count == 0)
            {
                RaiseEvent(LocallyAvailableDataInfoReceived, this, new LocallyAvailableDataInfoReceivedEventArgs(instrument, new List<StoredDataInfo>()));
            }
            else
            {
                var storageInfo = new List<StoredDataInfo>();
                for (int i = 0; i < count; i++)
                {
                    buffer = _dealerSocket.ReceiveFrameBytes();
                    var info = MyUtils.ProtoBufDeserialize<StoredDataInfo>(buffer, ms);
                    storageInfo.Add(info);

                    if (!_dealerSocket.Options.ReceiveMore) break;
                }

                RaiseEvent(LocallyAvailableDataInfoReceived, this, new LocallyAvailableDataInfoReceivedEventArgs(instrument, storageInfo));
            }
        }

        /// <summary>
        /// Called on a reply to a data push
        /// </summary>
        private void HandleDataPushReply()
        {
            string result = _dealerSocket.ReceiveFrameString();
            if (result == "OK") //everything is alright
            {
                return; //maybe raise an event to report the status of the request instead?
            }
            else if (result == "ERROR")
            {
                //receive the error
                string error = _dealerSocket.ReceiveFrameString();
                RaiseEvent(Error, this, new ErrorArgs(-1, "Data push error: " + error));
            }
        }

        /// <summary>
        /// Called ona reply to a historical data request
        /// </summary>
        private void HandleHistoricalDataRequestReply()
        {
            var ms = new MemoryStream();

            //2nd message part: the HistoricalDataRequest object that was used to make the request
            bool hasMore;
            byte[] requestBuffer = _dealerSocket.ReceiveFrameBytes(out hasMore);
            if (!hasMore) return;

            var request = MyUtils.ProtoBufDeserialize<HistoricalDataRequest>(requestBuffer, ms);

            //3rd message part: the size of the uncompressed, serialized data. Necessary for decompression.
            byte[] sizeBuffer = _dealerSocket.ReceiveFrameBytes(out hasMore);
            if (!hasMore) return;

            int outputSize = BitConverter.ToInt32(sizeBuffer, 0);

            //4th message part: the compressed serialized data.
            byte[] dataBuffer = _dealerSocket.ReceiveFrameBytes();
            byte[] decompressed = LZ4Codec.Decode(dataBuffer, 0, dataBuffer.Length, outputSize);

            var data = MyUtils.ProtoBufDeserialize<List<OHLCBar>>(decompressed, ms);

            //remove from pending requests
            lock (_pendingHistoricalRequestsLock)
            {
                PendingHistoricalRequests.RemoveAll(x => x.RequestID == request.RequestID);
            }

            RaiseEvent(HistoricalDataReceived, this, new HistoricalDataEventArgs(request, data));
        }

        //heartbeat makes sure the server is still up
        private void HeartBeat()
        {
            lock (_reqSocketLock)
            {
                _reqSocket.SendMoreFrame("");
                _reqSocket.SendFrame("PING");
            }
        }

        /// <summary>
        /// Query the server for contracts matching a particular set of features.
        /// </summary>
        /// <param name="instrument">An Instrument object; any features that are not null will be search parameters. If null, all instruments are returned.</param>
        /// <returns>A list of instruments matching these features.</returns>
        public List<Instrument> FindInstruments(Instrument instrument = null)
        {
            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not request instruments - not connected."));
                return new List<Instrument>();
            }

            using (NetMQSocket s = _context.CreateSocket(ZmqSocketType.Req))
            {
                s.Connect(string.Format("tcp://{0}:{1}", _host, _instrumentServerPort));
                var ms = new MemoryStream();

                if (instrument == null) //all contracts
                {
                    s.SendFrame("ALL");
                }
                else //an actual search
                {
                    s.SendMoreFrame("SEARCH"); //first we send a search request

                    //then we need to serialize and send the instrument
                    s.SendFrame(MyUtils.ProtoBufSerialize(instrument, ms));
                }

                //first we receive the size of the final uncompressed byte[] array
                bool hasMore;
                byte[] sizeBuffer = s.ReceiveFrameBytes(out hasMore);
                if (sizeBuffer.Length == 0)
                {
                    RaiseEvent(Error, this, new ErrorArgs(-1, "Contract request failed, received no reply."));
                    return new List<Instrument>();
                }

                int outputSize = BitConverter.ToInt32(sizeBuffer, 0);

                //then the actual data
                byte[] buffer = s.ReceiveFrameBytes(out hasMore);
                if (buffer.Length == 0)
                {
                    RaiseEvent(Error, this, new ErrorArgs(-1, "Contract request failed, received no data."));
                    return new List<Instrument>();
                }

                try
                {
                    //then we process it by first decompressing
                    ms.SetLength(0);
                    byte[] decoded = LZ4Codec.Decode(buffer, 0, buffer.Length, outputSize);
                    ms.Write(decoded, 0, decoded.Length);
                    ms.Position = 0;

                    //and finally deserializing
                    return Serializer.Deserialize<List<Instrument>>(ms);
                }
                catch (Exception ex)
                {
                    RaiseEvent(Error, this, new ErrorArgs(-1, "Error processing instrument data: " + ex.Message));
                    return new List<Instrument>();
                }
            }
        }

        /// <summary>
        /// Add an instrument to QDMS.
        /// </summary>
        /// <param name="instrument"></param>
        /// <returns>The instrument with its ID set if successful, null otherwise.</returns>
        public Instrument AddInstrument(Instrument instrument)
        {
            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not add instrument - not connected."));
                return null;
            }

            if(instrument == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not add instrument - instrument is null."));
                return null;
            }

            using (NetMQSocket s = _context.CreateSocket(ZmqSocketType.Req))
            {
                s.Connect(string.Format("tcp://{0}:{1}", _host, _instrumentServerPort));
                var ms = new MemoryStream();

                s.SendMoreFrame("ADD"); //first we send an "ADD" request

                //then we need to serialize and send the instrument
                s.SendFrame(MyUtils.ProtoBufSerialize(instrument, ms));

                //then get the reply
                string result = s.ReceiveString(TimeSpan.FromSeconds(1));

                if(result != "SUCCESS")
                {
                    RaiseEvent(Error, this, new ErrorArgs(-1, "Instrument addition failed: received no reply."));
                    return null;
                }

                //Addition was successful, receive the instrument and return it
                byte[] serializedInstrument = s.ReceiveFrameBytes();

                return MyUtils.ProtoBufDeserialize<Instrument>(serializedInstrument, ms);
            }
        }

        /// <summary>
        /// Cancel a live real time data stream.
        /// </summary>
        public void CancelRealTimeData(Instrument instrument)
        {
            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not cancel real time data - not connected."));
                return;
            }

            if (_reqSocket != null)
            {
                lock (_reqSocketLock)
                {
                    //two part message:
                    //1: "CANCEL"
                    //2: serialized Instrument object
                    var ms = new MemoryStream();
                    _reqSocket.SendMoreFrame("");
                    _reqSocket.SendMoreFrame("CANCEL");
                    _reqSocket.SendFrame(MyUtils.ProtoBufSerialize(instrument, ms));
                }
            }

            if(_subSocket != null)
                _subSocket.Unsubscribe(Encoding.UTF8.GetBytes(instrument.Symbol));

            lock (_realTimeDataStreamsLock)
            {
                RealTimeDataStreams.RemoveAll(x => x.Instrument.ID == instrument.ID);
            }
        }

        /// <summary>
        /// Get a list of all available instruments
        /// </summary>
        /// <returns></returns>
        public List<Instrument> GetAllInstruments()
        {
            return FindInstruments();
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

        public event EventHandler<RealTimeDataEventArgs> RealTimeDataReceived;

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataReceived;

        public event EventHandler<LocallyAvailableDataInfoReceivedEventArgs> LocallyAvailableDataInfoReceived;

        public event EventHandler<ErrorArgs> Error;
    }
}