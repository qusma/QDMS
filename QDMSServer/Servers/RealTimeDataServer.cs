// -----------------------------------------------------------------------
// <copyright file="RealTimeDataServer.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// This class handles networking for real time data data.
// Clients send their requests through ZeroMQ. Here they are parsed
// and then forwarded to the RealTimeDataBroker.
// Data sent from the RealTimeDataBroker is sent out to the clients.
// Two types of possible requests: 
// 1. To start receiving real time data
// 2. To cancel a real time data stream


using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using NLog;
using ProtoBuf;
using QDMS;
using ZeroMQ;

namespace QDMSServer
{
    public class RealTimeDataServer : IDisposable
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
        /// Is true if the server is running
        /// </summary>
        public bool ServerRunning { get; private set; }

        public IRealTimeDataBroker Broker { get; set; }

        private Thread _requestThread;

        private bool _runServer = true;
        private ZmqContext _context;
        private ZmqSocket _pubSocket;
        private ZmqSocket _reqSocket;
        

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly object _pubSocketLock = new object();

        public RealTimeDataServer(int pubPort, int reqPort, IRealTimeDataBroker broker = null)
        {
            if (pubPort == reqPort) throw new Exception("Publish and request ports must be different");
            PublisherPort = pubPort;
            RequestPort = reqPort;

            Broker = broker ?? new RealTimeDataBroker();
            Broker.RealTimeDataArrived += _broker_RealTimeDataArrived;
        }

        /// <summary>
        /// When data arrives from an external data source to the broker, this event is fired.
        /// </summary>
        void _broker_RealTimeDataArrived(object sender, RealTimeDataEventArgs e)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, e);
                //this lock is needed because this method will be called from 
                //the thread of each DataSource in the broker
                lock (_pubSocketLock) 
                {
                    _pubSocket.SendMore(Encoding.UTF8.GetBytes(e.Symbol)); //start by sending the ticker before the data
                    _pubSocket.Send(ms.ToArray()); //then send the serialized bar
                }
            }
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

                _requestThread = new Thread(RequestServer) { Name = "RTDB Request Thread" };

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
                Broker.CancelRTDStream(instrument.ID.Value);

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

            //make sure the ID and data sources are set
            if (!request.Instrument.ID.HasValue)
            {
                _reqSocket.SendMore("ERROR", Encoding.UTF8);
                _reqSocket.Send("Instrument had no ID set.", Encoding.UTF8);

                Log(LogLevel.Error, "Instrument with no ID requested.");
                return;
            }

            if (request.Instrument.Datasource == null)
            {
                _reqSocket.SendMore("ERROR", Encoding.UTF8);
                _reqSocket.Send("Instrument had no data source set.", Encoding.UTF8);

                Log(LogLevel.Error, "Instrument with no data source requested.");
                return;
            }

            //with the current approach we can't handle multiple real time data streams from
            //the same symbol and data source, but at different frequencies

            //forward the request to the broker
            try
            {
                if (Broker.RequestRealTimeData(request))
                {
                    //and report success back to the requesting client
                    _reqSocket.SendMore("SUCCESS", Encoding.UTF8);
                    //along with the request
                    _reqSocket.Send(MyUtils.ProtoBufSerialize(request, ms));
                }
                else
                {
                    //report error back to the requesting client
                    _reqSocket.SendMore("ERROR", Encoding.UTF8);
                    //error message
                    _reqSocket.SendMore("Unkown error.", Encoding.UTF8);
                    //along with the request
                    _reqSocket.Send(MyUtils.ProtoBufSerialize(request, ms));
                }
            }
            catch (Exception ex)
            {
                //there was a problem with requesting the feed
                _reqSocket.SendMore("ERROR", Encoding.UTF8);
                //error message
                _reqSocket.SendMore(ex.Message, Encoding.UTF8);
                //request
                _reqSocket.Send(MyUtils.ProtoBufSerialize(request, ms));
            }

            
        }

        /// <summary>
        /// Log stuff.
        /// </summary>
        private void Log(LogLevel level, string message)
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.InvokeAsync(() =>
                    _logger.Log(level, message));
        }

        public void Dispose()
        {
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
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }
    }
}
