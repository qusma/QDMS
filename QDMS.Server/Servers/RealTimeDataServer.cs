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
using System.Threading.Tasks;
using NLog;
using ProtoBuf;
using QDMS;
using NetMQ;
using Poller = NetMQ.Poller;

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

        private NetMQContext _context;
        private NetMQSocket _pubSocket;
        private NetMQSocket _reqSocket;
        

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly object _pubSocketLock = new object();
        private Poller _poller;

        public RealTimeDataServer(int pubPort, int reqPort, IRealTimeDataBroker broker)
        {
            if (broker == null) throw new ArgumentNullException(nameof(broker));
            if (pubPort == reqPort) throw new Exception("Publish and request ports must be different");
            PublisherPort = pubPort;
            RequestPort = reqPort;

            Broker = broker;
            Broker.RealTimeDataArrived += _broker_RealTimeDataArrived;
        }

        /// <summary>
        /// When data arrives from an external data source to the broker, this event is fired.
        /// </summary>
        void _broker_RealTimeDataArrived(object sender, RealTimeDataEventArgs e)
        {
            if (_pubSocket == null) return;

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, e);
                //this lock is needed because this method will be called from 
                //the thread of each DataSource in the broker
                lock (_pubSocketLock) 
                {
                    _pubSocket.SendMoreFrame(BitConverter.GetBytes(e.InstrumentID)); //start by sending the ticker before the data
                    _pubSocket.SendFrame(ms.ToArray()); //then send the serialized bar
                }
            }
        }

        //tells the servers to stop running and waits for the threads to shut down.
        public void StopServer()
        {
            if (_poller != null && _poller.IsStarted)
            {
                _poller.CancelAndJoin();
            }

            //clear the socket and context and say it's not running any more
            if (_pubSocket != null)
            {
                _pubSocket.Dispose();
            }
            ServerRunning = false;
        }

        /// <summary>
        /// Starts the publishing and request servers.
        /// </summary>
        public void StartServer()
        {
            if (!ServerRunning)
            {
                _context = NetMQContext.Create();

                //the publisher socket
                _pubSocket = _context.CreatePublisherSocket();
                _pubSocket.Bind("tcp://*:" + PublisherPort);

                //the request socket
                _reqSocket = _context.CreateSocket(ZmqSocketType.Rep);
                _reqSocket.Bind("tcp://*:" + RequestPort);
                _reqSocket.ReceiveReady += _reqSocket_ReceiveReady;

                _poller = new Poller(new[] { _reqSocket });
                Task.Factory.StartNew(_poller.PollTillCancelled, TaskCreationOptions.LongRunning);
            }
            ServerRunning = true;
        }

        void _reqSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            string requestType = _reqSocket.ReceiveFrameString();
            if (requestType == null) return;

            //handle ping requests
            if (requestType == "PING")
            {
                _reqSocket.SendFrame("PONG");
                return;
            }

            //Handle real time data requests
            if (requestType == "RTD") //Two part message: first, "RTD" string. Then the RealTimeDataRequest object.
            {
                HandleRTDataRequest();
            }

            //manage cancellation requests
            //two part message: first: "CANCEL". Second: the instrument
            if (requestType == "CANCEL")
            {
                HandleRTDataCancelRequest();
            }
        }

        // Accept a request to cancel a real time data stream
        // Obviously we only actually cancel it if
        private void HandleRTDataCancelRequest()
        {
            bool hasMore;
            byte[] buffer = _reqSocket.ReceiveFrameBytes(out hasMore);

            //receive the instrument
            var ms = new MemoryStream();
            var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

            if (instrument.ID != null)
                Broker.CancelRTDStream(instrument.ID.Value);

            //two part message:
            //1: "CANCELED"
            //2: the symbol
            _reqSocket.SendMoreFrame("CANCELED");
            _reqSocket.SendFrame(instrument.Symbol);
        }

        /// <summary>
        /// Send an error reply to a real time data request.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="serializedRequest"></param>
        private void SendErrorReply(string message, byte[] serializedRequest)
        {
            _reqSocket.SendMoreFrame("ERROR");
            _reqSocket.SendMoreFrame(message);
            _reqSocket.SendFrame(serializedRequest);
        }

        // Accept a real time data request
        private void HandleRTDataRequest()
        {
            using (var ms = new MemoryStream())
            {
                bool hasMore;
                byte[] buffer = _reqSocket.ReceiveFrameBytes(out hasMore);

                var request = MyUtils.ProtoBufDeserialize<RealTimeDataRequest>(buffer, ms);

                //make sure the ID and data sources are set
                if (!request.Instrument.ID.HasValue)
                {
                    SendErrorReply("Instrument had no ID set.", buffer);

                    Log(LogLevel.Error, "Instrument with no ID requested.");
                    return;
                }

                if (request.Instrument.Datasource == null)
                {
                    SendErrorReply("Instrument had no data source set.", buffer);

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
                        _reqSocket.SendMoreFrame("SUCCESS");
                        //along with the request
                        _reqSocket.SendFrame(MyUtils.ProtoBufSerialize(request, ms));
                    }
                    else
                    {
                        throw new Exception("Unknown error.");
                    }
                }
                catch (Exception ex)
                {
                    //there was a problem with requesting the feed
                    SendErrorReply(ex.Message, buffer);

                    //log the error
                    Log(LogLevel.Error, string.Format("RTDS: Error handling RTD request {0} @ {1} ({2}): {3}", 
                        request.Instrument.Symbol, request.Instrument.Datasource, request.Frequency, ex.Message));
                }
            }
        }

        /// <summary>
        /// Log stuff.
        /// </summary>
        private void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }

        public void Dispose()
        {
            StopServer();

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
