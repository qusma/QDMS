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
using System.Timers;
using System.Linq;
using LZ4;
using ProtoBuf;
using QDMS;
using ZeroMQ;
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

        private ZmqContext _context;

        /// <summary>
        /// This socket sends requests for real time data.
        /// </summary>
        private ZmqSocket _reqSocket;

        /// <summary>
        /// This socket receives real time data.
        /// </summary>
        private ZmqSocket _subSocket;

        /// <summary>
        /// This socket sends requests for and receives historical data.
        /// </summary>
        private ZmqSocket _dealerSocket;

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

        /// <summary>
        /// This thread runs the RealTimeDataReceiveLoop() method. Receives data.
        /// </summary>
        private Thread _realTimeDataReceiveLoopThread;

        /// <summary>
        /// This thread runs the RequestRepliesThread() method.
        /// It polls the _reqSocket, which receives replies to requests:
        /// heartbeats, errors, successful stream requests, and successful stream cancelations.
        /// </summary>
        private Thread _reqLoopThread;

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
            if (request.Instrument.ID == null)
            {
                RaiseEvent(Error, null, new ErrorArgs(-1, "Instrument must have an ID"));
                return;
            }

            var ms = new MemoryStream();

            lock (_dealerSocketLock)
            {
                _dealerSocket.SendMore("HISTPUSH", Encoding.UTF8);
                _dealerSocket.Send(MyUtils.ProtoBufSerialize(request, ms));
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
                _dealerSocket.SendMore("AVAILABLEDATAREQ", Encoding.UTF8);
                _dealerSocket.Send(MyUtils.ProtoBufSerialize(instrument, ms));
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
                _reqSocket.SendMore("", Encoding.UTF8);
                _reqSocket.SendMore("RTD", Encoding.UTF8);
                _reqSocket.Send(MyUtils.ProtoBufSerialize(request, ms));
            }
            return request.RequestID;
        }

        /// <summary>
        /// Tries to connect to the QDMS server.
        /// </summary>
        public void Connect()
        {
            Dispose();

            _context = ZmqContext.Create();
            _reqSocket = _context.CreateSocket(SocketType.DEALER);
            _subSocket = _context.CreateSocket(SocketType.SUB);
            _dealerSocket = _context.CreateSocket(SocketType.DEALER);

            _reqSocket.Identity = Encoding.UTF8.GetBytes(_name);
            _subSocket.Identity = Encoding.UTF8.GetBytes(_name);
            _dealerSocket.Identity = Encoding.UTF8.GetBytes(_name);

            _dealerSocket.ReceiveReady += _dealerSocket_ReceiveReady;
            _reqSocket.ReceiveReady += _reqSocket_ReceiveReady;
            _subSocket.ReceiveReady += _subSocket_ReceiveReady;

            _reqSocket.Connect(string.Format("tcp://{0}:{1}", _host, _realTimeRequestPort));

            //start off by sending a ping to make sure everything is regular
            string reply = "";
            try
            {
                _reqSocket.SendMore("", Encoding.UTF8);
                _reqSocket.Send("PING", Encoding.UTF8);

                _reqSocket.Receive(Encoding.UTF8, TimeSpan.FromSeconds(1)); //empty frame starts the REP message
                reply = _reqSocket.Receive(Encoding.UTF8, TimeSpan.FromMilliseconds(50));
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

            //this loop takes care of the real time data requests
            _realTimeDataReceiveLoopThread = new Thread(RealTimeDataReceiveLoop) { Name = "Client RTD Loop" };
            _realTimeDataReceiveLoopThread.Start();

            //this loop takes care of replies to the request socket: heartbeats and data request status messages
            _reqLoopThread = new Thread(RequestRepliesThread) { Name = "Client Requests Loop" };
            _reqLoopThread.Start();

            _heartBeatTimer = new Timer(1000);
            _heartBeatTimer.Elapsed += _timer_Elapsed;
            _heartBeatTimer.Start();
        }

        /// <summary>
        /// Poll for replies on the request socket.
        /// </summary>
        private void RequestRepliesThread()
        {
            var timeout = TimeSpan.FromMilliseconds(10);

            using (var poller = new Poller(new[] { _reqSocket }))
            {
                try
                {
                    while (_running)
                    {
                        poller.Poll(timeout);
                    }
                }
                catch
                {
                    Dispose();
                }
            }
        }

        /// <summary>
        /// Process replies on the request socket.
        /// Heartbeats, errors, and subscribing to real time data streams.
        /// </summary>
        private void _reqSocket_ReceiveReady(object sender, SocketEventArgs e)
        {
            var timeout = TimeSpan.FromMilliseconds(10);
            var ms = new MemoryStream();

            //wait for reply to see what happened
            lock (_reqSocketLock)
            {
                string reply = _reqSocket.Receive(Encoding.UTF8, timeout);

                if (reply == null) return;

                reply = _reqSocket.Receive(Encoding.UTF8, timeout);

                switch (reply)
                {
                    case "PONG": //reply to heartbeat message
                        _lastHeartBeat = DateTime.Now;
                        break;

                    case "ERROR": //something went wrong
                        {
                            //first the message
                            string error = _reqSocket.Receive(Encoding.UTF8);

                            //then the request
                            int size;
                            byte[] buffer = _reqSocket.Receive(null, TimeSpan.FromSeconds(1), out size);
                            var request = MyUtils.ProtoBufDeserialize<RealTimeDataRequest>(buffer, ms);

                            //error event
                            RaiseEvent(Error, this, new ErrorArgs(-1, "Real time data request error: " + error, request.RequestID));
                            return;
                        }
                    case "SUCCESS": //successful request to start a new real time data stream
                        {
                            //receive the request
                            int size;
                            byte[] buffer = _reqSocket.Receive(null, TimeSpan.FromSeconds(1), out size);
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
                            string symbol = _reqSocket.Receive(Encoding.UTF8);

                            //nothing to do?
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Disconnects from the server.
        ///  </summary>
        public void Disconnect()
        {
            //start by canceling all active real time streams
            while (RealTimeDataStreams.Count > 0)
            {
                CancelRealTimeData(RealTimeDataStreams.First().Instrument);
            }

            _running = false;

            if(_heartBeatTimer != null)
                _heartBeatTimer.Stop();

            if(_dealerLoopThread != null && _dealerLoopThread.ThreadState == ThreadState.Running)
                _dealerLoopThread.Join(10);

            if (_realTimeDataReceiveLoopThread != null && _realTimeDataReceiveLoopThread.ThreadState == ThreadState.Running)
                _realTimeDataReceiveLoopThread.Join(10);

            if (_reqLoopThread != null && _reqLoopThread.ThreadState == ThreadState.Running)
                _reqLoopThread.Join(10);

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
            var timeout = TimeSpan.FromMilliseconds(5);
            _dealerSocket.Identity = Encoding.Unicode.GetBytes(_name);

            using (var poller = new Poller(new[] { _dealerSocket }))
            {
                var ms = new MemoryStream();
                try
                {
                    while (_running)
                    {
                        //send any pending historical data requests
                        if (!_historicalDataRequests.IsEmpty)
                        {
                            lock (_dealerSocketLock)
                            {
                                HistoricalDataRequest request;
                                bool success = _historicalDataRequests.TryDequeue(out request);
                                if (success)
                                {
                                    byte[] buffer = MyUtils.ProtoBufSerialize(request, ms);
                                    _dealerSocket.SendMore("HISTREQ", Encoding.UTF8);
                                    _dealerSocket.Send(buffer);
                                }
                            }
                        }

                        //poller raises event when there's data coming in. See _dealerSocket_ReceiveReady()
                        poller.Poll(timeout);
                    }
                }
                catch
                {
                    Dispose();
                }
            }
        }

        /// <summary>
        /// This loop waits for real time data to be received on the subscription socket, then raises the RealTimeDataReceived event with it.
        /// </summary>
        private void RealTimeDataReceiveLoop()
        {
            var timeout = TimeSpan.FromMilliseconds(1);

            using (var poller = new Poller(new[] { _subSocket }))
            {
                try
                {
                    while (_running)
                    {
                        poller.Poll(timeout);
                    }
                }
                catch
                {
                    Dispose();
                }
            }
        }

        private void _subSocket_ReceiveReady(object sender, SocketEventArgs e)
        {
            int size;
            byte[] instrumentID = _subSocket.Receive(null, out size);

            if (size > 0)
            {
                byte[] buffer = _subSocket.Receive(null, out size);
                var bar = MyUtils.ProtoBufDeserialize<RealTimeDataEventArgs>(buffer, new MemoryStream());
                RaiseEvent(RealTimeDataReceived, null, bar);
            }
        }

        /// <summary>
        /// Handling replies to a data push, a historical data request, or an available data request
        /// </summary>
        private void _dealerSocket_ReceiveReady(object sender, SocketEventArgs e)
        {
            lock (_dealerSocketLock)
            {
                //1st message part: what kind of stuff we're receiving
                string type = _dealerSocket.Receive(Encoding.UTF8);
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
            int size;
            byte[] buffer = _dealerSocket.Receive(null, TimeSpan.FromMilliseconds(100), out size);
            if (size <= 0) return;
            int requestID = BitConverter.ToInt32(buffer, 0);

            //remove from pending requests
            lock (_pendingHistoricalRequestsLock)
            {
                PendingHistoricalRequests.RemoveAll(x => x.RequestID == requestID);
            }

            //finally the error message
            string message = _dealerSocket.Receive(Encoding.UTF8);

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
            int size;
            byte[] buffer = _dealerSocket.Receive(null, out size);
            var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

            //second the number of items
            buffer = _dealerSocket.Receive(null, out size);
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
                    buffer = _dealerSocket.Receive(null, out size);
                    var info = MyUtils.ProtoBufDeserialize<StoredDataInfo>(buffer, ms);
                    storageInfo.Add(info);

                    if (!_dealerSocket.ReceiveMore) break;
                }

                RaiseEvent(LocallyAvailableDataInfoReceived, this, new LocallyAvailableDataInfoReceivedEventArgs(instrument, storageInfo));
            }
        }

        /// <summary>
        /// Called on a reply to a data push
        /// </summary>
        private void HandleDataPushReply()
        {
            string result = _dealerSocket.Receive(Encoding.UTF8);
            if (result == "OK") //everything is alright
            {
                return; //maybe raise an event to report the status of the request instead?
            }
            else if (result == "ERROR")
            {
                //receive the error
                string error = _dealerSocket.Receive(Encoding.UTF8);
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
            int size;
            byte[] requestBuffer = _dealerSocket.Receive(null, out size);
            if (size <= 0) return;

            var request = MyUtils.ProtoBufDeserialize<HistoricalDataRequest>(requestBuffer, ms);

            //3rd message part: the size of the uncompressed, serialized data. Necessary for decompression.
            byte[] sizeBuffer = _dealerSocket.Receive(null, out size);
            if (size <= 0) return;

            int outputSize = BitConverter.ToInt32(sizeBuffer, 0);

            //4th message part: the compressed serialized data.
            byte[] dataBuffer = _dealerSocket.Receive(null, out size);
            byte[] decompressed = LZ4Codec.Decode(dataBuffer, 0, size, outputSize);

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
                _reqSocket.SendMore("", Encoding.UTF8);
                _reqSocket.Send("PING", Encoding.UTF8);
            }
        }

        /// <summary>
        /// Query the server for contracts matching a particular set of features.
        /// </summary>
        /// <param name="instrument">An Instrument object; any features that are not null will be search parameters. If null, all instruments are returned.</param>
        /// <returns>A list of instruments matching these features.</returns>
        public List<Instrument> FindInstruments(Instrument instrument = null)
        {
            using (var context = ZmqContext.Create())
            {
                using (ZmqSocket s = context.CreateSocket(SocketType.REQ))
                {
                    s.Connect(string.Format("tcp://{0}:{1}", _host, _instrumentServerPort));
                    var ms = new MemoryStream();

                    if (instrument == null) //all contracts
                    {
                        s.Send("ALL", Encoding.UTF8);
                    }
                    else //an actual search
                    {
                        s.SendMore("SEARCH", Encoding.UTF8); //first we send a search request

                        //then we need to serialize and send the instrument
                        s.Send(MyUtils.ProtoBufSerialize(instrument, ms));
                    }

                    //first we receive the size of the final uncompressed byte[] array
                    int size;
                    byte[] sizeBuffer = s.Receive(null, TimeSpan.FromSeconds(1), out size);
                    if (size <= 0)
                    {
                        RaiseEvent(Error, this, new ErrorArgs(-1, "Contract request failed, received no reply."));
                        return new List<Instrument>();
                    }

                    int outputSize = BitConverter.ToInt32(sizeBuffer, 0);

                    //then the actual data
                    byte[] buffer = s.Receive(null, TimeSpan.FromSeconds(1), out size);
                    if (size <= 0)
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
                    _reqSocket.SendMore("", Encoding.UTF8);
                    _reqSocket.SendMore("CANCEL", Encoding.UTF8);
                    _reqSocket.Send(MyUtils.ProtoBufSerialize(instrument, ms));
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