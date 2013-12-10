// -----------------------------------------------------------------------
// <copyright file="QDMSClient.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using LZ4;
using ProtoBuf;
using QDMS;
using ZeroMQ;
using Timer = System.Timers.Timer;

namespace QDMSClient
{
    public class QDMSClient : IDisposable
    {
        public event EventHandler<RealTimeDataEventArgs> RealTimeDataReceived;
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataReceived;
        public event EventHandler<LocallyAvailableDataInfoReceivedEventArgs> LocallyAvailableDataInfoReceived;
        public event EventHandler<ErrorArgs> Error;
        
        private DateTime _lastHeartBeat;
        public bool Connected 
        {
            get
            {
                return (DateTime.Now - _lastHeartBeat).Seconds < 5;
            }
        }

        private ZmqContext _context;
        private ZmqSocket _reqSocket; //this socket sends requests for real time data
        private ZmqSocket _subSocket; //this socket receives real time data
        private ZmqSocket _dealerSocket; //this socket sends requests for and receives historical data
        private Timer _heartBeatTimer;

        private Thread _dealerLoopThread;
        private Thread _realTimeDataReceiveLoopThread;
        private Thread _reqLoopThread;

        private readonly string _host;
        private readonly string _name;
        private readonly int _realTimeRequestPort;
        private readonly int _realTimePublishPort;
        private readonly int _instrumentServerPort;
        private readonly int _historicalDataPort;
        private readonly ConcurrentQueue<HistoricalDataRequest> _historicalDataRequests;

        private readonly object _reqSocketLock = new object();
        private readonly object _dealerSocketLock = new object();

        private bool _running;

        public void Dispose()
        {
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
            _context = ZmqContext.Create();
            _reqSocket = _context.CreateSocket(SocketType.DEALER);
            _subSocket = _context.CreateSocket(SocketType.SUB);
            _dealerSocket = _context.CreateSocket(SocketType.DEALER);

            _reqSocket.Identity = Encoding.UTF8.GetBytes(clientName);
            _subSocket.Identity = Encoding.UTF8.GetBytes(clientName);
            _dealerSocket.Identity = Encoding.UTF8.GetBytes(clientName);

            _heartBeatTimer = new Timer(1000);
            _heartBeatTimer.Elapsed += _timer_Elapsed;
            _host = host;
            _name = clientName;

            _realTimeRequestPort = realTimeRequestPort;
            _realTimePublishPort = realTimePublishPort;
            _instrumentServerPort = instrumentServerPort;
            _historicalDataPort = historicalDataPort;

            _historicalDataRequests = new ConcurrentQueue<HistoricalDataRequest>();
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
        /// <param name="instrument"></param>
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
        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            HeartBeat();
        }

        /// <summary>
        /// Request historical data. Data will be delivered through the HistoricalDataReceived event.
        /// </summary>
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            _historicalDataRequests.Enqueue(request);
        }

        /// <summary>
        /// Request a new real time data stream. Data will be delivered through the RealTimeDataReceived event.
        /// </summary>
        public void RequestRealTimeData(RealTimeDataRequest request)
        {
            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not request real time data - not connected."));
                return;
            }

            lock (_reqSocketLock)
            {
                var ms = new MemoryStream();
                _reqSocket.SendMore("", Encoding.UTF8);
                _reqSocket.SendMore("RTD", Encoding.UTF8);
                _reqSocket.Send(MyUtils.ProtoBufSerialize(request, ms));
            }
        }

        /// <summary>
        /// Tries to connect to the QDMS server.
        /// </summary>
        public void Connect()
        {
            _reqSocket.Connect(string.Format("tcp://{0}:{1}", _host, _realTimeRequestPort));
            _reqSocket.SendMore("", Encoding.UTF8);
            _reqSocket.Send("PING", Encoding.UTF8);

            _reqSocket.Receive(Encoding.UTF8, TimeSpan.FromSeconds(1)); //empty frame starts the REP message
            string reply = _reqSocket.Receive(Encoding.UTF8, TimeSpan.FromMilliseconds(50));
            if (reply != "PONG") //server didn't reply or replied incorrectly
            {
                _reqSocket.Disconnect(string.Format("tcp://{0}:{1}", _host, _realTimeRequestPort));
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
            _dealerLoopThread = new Thread(DealerLoop) {Name = "Client Dealer Loop"};
            _dealerLoopThread.Start();

            //this loop takes care of the real time data requests
            _realTimeDataReceiveLoopThread = new Thread(RealTimeDataReceiveLoop) {Name = "Client RTD Loop"};
            _realTimeDataReceiveLoopThread.Start();

            //this loop takes care of replies to the request socket: heartbeats and data request status messages
            _reqLoopThread = new Thread(RequestRepliesThread) {Name = "Client Requests Loop"};
            _reqLoopThread.Start();

            _heartBeatTimer.Start();
            
        }

        private void RequestRepliesThread()
        {
            var timeout = TimeSpan.FromMilliseconds(10);

            string reply;
            while (_running)
            {
                //wait for reply to see what happened
                lock (_reqSocketLock)
                {
                    reply = _reqSocket.Receive(Encoding.UTF8, timeout); //will be an empty string if there is a message


                    if (reply == null)
                    {
                        continue;
                    }

                    reply = _reqSocket.Receive(Encoding.UTF8, timeout);


                    if (reply == "PONG") _lastHeartBeat = DateTime.Now;

                    if (reply == "ERROR")
                    {
                        string error = _reqSocket.Receive(Encoding.UTF8);
                        RaiseEvent(Error, this, new ErrorArgs(-1, "Real time data request error: " + error));
                        return;
                    }

                    if (reply == "SUCCESS")
                    {
                        //also receive the symbol
                        string symbol = _reqSocket.Receive(Encoding.UTF8);
                        //request worked, so we subscribe to the stream
                        _subSocket.Subscribe(Encoding.UTF8.GetBytes(symbol));
                    }
                }
            }
        }

        /// <summary>
        /// Disconnects from the server.
        ///  </summary>
        public void Disconnect()
        {
            _running = false;
            _heartBeatTimer.Stop();

            _dealerLoopThread.Join();
            _realTimeDataReceiveLoopThread.Join();
            _reqLoopThread.Join();
            
            _reqSocket.Disconnect(string.Format("tcp://{0}:{1}", _host, _realTimeRequestPort));
            _subSocket.Disconnect(string.Format("tcp://{0}:{1}", _host, _realTimePublishPort));
            _dealerSocket.Disconnect(string.Format("tcp://{0}:{1}", _host, _historicalDataPort));
        }

        //dealer socket sends out requests for historical data and raises an event when it's received
        private void DealerLoop()
        {
            var timeout = TimeSpan.FromMilliseconds(5);
            _dealerSocket.Identity = Encoding.Unicode.GetBytes(_name);
            _dealerSocket.ReceiveReady += _dealerSocket_ReceiveReady;
            using (var poller = new Poller(new[] { _dealerSocket }))
            {
                var ms = new MemoryStream();
                while (_running)
                {
                        if (!_historicalDataRequests.IsEmpty)
                        {
                            lock (_dealerSocketLock)
                            {
                                _dealerSocket.SendMore("HISTREQ", Encoding.UTF8);

                                HistoricalDataRequest request;
                                bool success = _historicalDataRequests.TryDequeue(out request);
                                if (success)
                                {
                                    byte[] buffer = MyUtils.ProtoBufSerialize(request, ms);
                                    _dealerSocket.Send(buffer);
                                }
                                else
                                {
                                    _dealerSocket.Send(new byte[0]);
                                }
                            }
                        }

                    poller.Poll(timeout);
                }
            }
        }

        //This loop waits for real time data to be received on the subscription socket, then raises the RealTimeDataReceived event with it.
        private void RealTimeDataReceiveLoop()
        {
            var timeout = TimeSpan.FromMilliseconds(1);
            var ms = new MemoryStream();
            while (_running)
            {
                int size;
                byte[] symbol = _subSocket.Receive(null, timeout, out size);

                if (size > 0)
                {
                    byte[] buffer = _subSocket.Receive(null, out size);
                    var bar = MyUtils.ProtoBufDeserialize<RealTimeDataEventArgs>(buffer, ms);
                    RaiseEvent(RealTimeDataReceived, null, bar);
                }
            }
        }

        //we are ready to receive the historical data and raise the HistoricalDataReceived event
        private void _dealerSocket_ReceiveReady(object sender, SocketEventArgs e)
        {
            lock (_dealerSocketLock)
            {
                //1st message part: what kind of stuff we're receiving
                string type = _dealerSocket.Receive(Encoding.UTF8);
                if (type == "PUSHREP") //a reply to a data push request
                {
                    HandleDataPushReply();
                }
                else if (type == "HISTREQREP") //a reply to a request for historical data
                {
                    HandleHistoricalDataRequestReply();
                }
                else if (type == "AVAILABLEDATAREP") //a reply to a request for info on available locally stored data
                {
                    HandleAvailabledataReply();
                }
            }
        }

        //this is called when we get a reply on a request for available data in local storage
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

        //this is called on a reply to a data push
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

        //this is called ona reply to a historical data request
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
                        Serializer.Serialize(ms, instrument);
                        byte[] serializedInstrument = new byte[ms.Length];
                        ms.Read(serializedInstrument, 0, (int)ms.Length);
                        s.Send(serializedInstrument);
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
                        s.Close();

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
        /// <param name="request"></param>
        public void CancelRealTimeData(RealTimeDataRequest request)
        {
            throw new NotImplementedException();
            //todo write this
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
    }
}
