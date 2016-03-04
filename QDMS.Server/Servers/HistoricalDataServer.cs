// -----------------------------------------------------------------------
// <copyright file="HistoricalDataServer.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// This class handles networking for historical data.
// Clients send their requests through ZeroMQ. Here they are parsed
// and then forwarded to the HistoricalDataBroker.
// Data sent from the HistoricalDataBroker is sent out to the clients.
// Three types of possible requests:
// 1. For historical data
// 2. To check what data is available in the local database
// 3. To add data to the local database

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using LZ4;
using NLog;
using ProtoBuf;
using QDMS;
using NetMQ;

namespace QDMSServer
{
    public class HistoricalDataServer : IDisposable
    {
        /// <summary>
        /// Whether the broker is running or not.
        /// </summary>
        public bool ServerRunning { get; set; }

        private NetMQContext _context;
        private NetMQSocket _routerSocket;
        private readonly int _listenPort;
        private IHistoricalDataBroker _broker;
        private readonly ConcurrentQueue<KeyValuePair<HistoricalDataRequest, List<OHLCBar>>> _dataQueue;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private Poller _poller;
        private readonly object _socketLock = new object();

        public HistoricalDataServer(int port, IHistoricalDataBroker broker)
        {
            if (broker == null)
                throw new ArgumentNullException("broker");

            _listenPort = port;

            _dataQueue = new ConcurrentQueue<KeyValuePair<HistoricalDataRequest, List<OHLCBar>>>();

            _broker = broker;
            _broker.HistoricalDataArrived += _broker_HistoricalDataArrived;
        }

        private void _broker_HistoricalDataArrived(object sender, HistoricalDataEventArgs e)
        {
            lock (_socketLock)
            {
                SendFilledHistoricalRequest(e.Request, e.Data);
            }
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void StartServer()
        {
            //check that it's not already running
            if (ServerRunning) return;
            _context = NetMQContext.Create();

            _routerSocket = _context.CreateSocket(ZmqSocketType.Router);
            _routerSocket.Bind("tcp://*:" + _listenPort);
            _routerSocket.ReceiveReady += socket_ReceiveReady;

            _poller = new Poller(new[] { _routerSocket });

            Task.Factory.StartNew(_poller.PollTillCancelled, TaskCreationOptions.LongRunning);

            ServerRunning = true;
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void StopServer()
        {
            if (!ServerRunning) return;
            if (_poller != null && _poller.IsStarted)
            {
                _poller.CancelAndJoin();
            }
            ServerRunning = false;
        }


        /// <summary>
        /// Given a historical data request and the data that fill it,
        /// send the reply to the client who made the request.
        /// </summary>
        private void SendFilledHistoricalRequest(HistoricalDataRequest request, List<OHLCBar> data)
        {
            using (var ms = new MemoryStream())
            {
                //this is a 5 part message
                //1st message part: the identity string of the client that we're routing the data to
                string clientIdentity = request.RequesterIdentity;
                if (clientIdentity == null)
                    _routerSocket.SendMoreFrame(string.Empty);
                else
                    _routerSocket.SendMoreFrame(clientIdentity);

                //2nd message part: the type of reply we're sending
                _routerSocket.SendMoreFrame("HISTREQREP");

                //3rd message part: the HistoricalDataRequest object that was used to make the request
                _routerSocket.SendMoreFrame(MyUtils.ProtoBufSerialize(request, ms));

                //4th message part: the size of the uncompressed, serialized data. Necessary for decompression on the client end.
                byte[] uncompressed = MyUtils.ProtoBufSerialize(data, ms);
                _routerSocket.SendMoreFrame(BitConverter.GetBytes(uncompressed.Length));

                //5th message part: the compressed serialized data.
                byte[] compressed = LZ4Codec.EncodeHC(uncompressed, 0, uncompressed.Length); //compress
                _routerSocket.SendFrame(compressed);
            }
        }

        /// <summary>
        /// This is called when a new historical data request or data push request is made.
        /// </summary>
        private void socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            lock (_socketLock)
            {
                //Here we process the first two message parts: first, the identity string of the client
                string requesterIdentity = e.Socket.ReceiveFrameString();

                //second: the string specifying the type of request
                string text = e.Socket.ReceiveFrameString();
                if (text == "HISTREQ") //the client wants to request some data
                {
                    AcceptHistoricalDataRequest(requesterIdentity, e.Socket);
                }
                else if (text == "HISTPUSH") //the client wants to push some data into the db
                {
                    AcceptDataAdditionRequest(requesterIdentity, e.Socket);
                }
                else if (text == "AVAILABLEDATAREQ") //client wants to know what kind of data we have stored locally
                {
                    AcceptAvailableDataRequest(requesterIdentity, e.Socket);
                }
                else
                {
                    Log(LogLevel.Info, "Unrecognized request to historical data broker: " + text);
                }
            }
        }

        /// <summary>
        /// Handles requests for information on data that is available in local storage
        /// </summary>
        private void AcceptAvailableDataRequest(string requesterIdentity, NetMQSocket socket)
        {
            //get the instrument
            bool hasMore;
            var ms = new MemoryStream();
            byte[] buffer = socket.ReceiveFrameBytes(out hasMore);

            var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

            //log the request
            Log(LogLevel.Info, string.Format("Received local data storage info request for {0}.",
                instrument.Symbol));

            //and send the reply

            var storageInfo = _broker.GetAvailableDataInfo(instrument);
            socket.SendMoreFrame(requesterIdentity);
            socket.SendMoreFrame("AVAILABLEDATAREP");

            socket.SendMoreFrame(MyUtils.ProtoBufSerialize(instrument, ms));

            socket.SendMoreFrame(BitConverter.GetBytes(storageInfo.Count));
            foreach (StoredDataInfo sdi in storageInfo)
            {
                socket.SendMoreFrame(MyUtils.ProtoBufSerialize(sdi, ms));
            }
            socket.SendFrame("END");
        }

        /// <summary>
        /// Handles incoming data "push" requests: the client sends data for us to add to local storage.
        /// </summary>
        private void AcceptDataAdditionRequest(string requesterIdentity, NetMQSocket socket)
        {
            //final message part: receive the DataAdditionRequest object
            bool hasMore;
            var ms = new MemoryStream();
            byte[] buffer = socket.ReceiveFrameBytes(out hasMore);

            var request = MyUtils.ProtoBufDeserialize<DataAdditionRequest>(buffer, ms);

            //log the request
            Log(LogLevel.Info, string.Format("Received data push request for {0}.",
                request.Instrument.Symbol));

            //start building the reply
            socket.SendMoreFrame(requesterIdentity);
            socket.SendMoreFrame("PUSHREP");
            try
            {
                _broker.AddData(request);
                socket.SendFrame("OK");
            }
            catch (Exception ex)
            {
                socket.SendMoreFrame("ERROR");
                socket.SendFrame(ex.Message);
            }
        }

        /// <summary>
        /// Processes incoming historical data requests.
        /// </summary>
        private void AcceptHistoricalDataRequest(string requesterIdentity, NetMQSocket socket)
        {
            //third: a serialized HistoricalDataRequest object which contains the details of the request
            bool hasMore;
            byte[] buffer = socket.ReceiveFrameBytes(out hasMore);


            var ms = new MemoryStream();
            ms.Write(buffer, 0, buffer.Length);
            ms.Position = 0;
            HistoricalDataRequest request = Serializer.Deserialize<HistoricalDataRequest>(ms);

            //log the request
            Log(LogLevel.Info, string.Format("Historical Data Request from client {0}: {7} {1} @ {2} from {3} to {4} Location: {5} {6:;;SaveToLocal}",
                requesterIdentity,
                request.Instrument.Symbol,
                Enum.GetName(typeof(BarSize), request.Frequency),
                request.StartingDate,
                request.EndingDate,
                request.DataLocation,
                request.SaveDataToStorage ? 0 : 1,
                request.Instrument.Datasource.Name));

            request.RequesterIdentity = requesterIdentity;

            try
            {
                _broker.RequestHistoricalData(request);
            }
            catch (Exception ex) //there's some sort of problem with fulfilling the request. Inform the client.
            {
                SendErrorReply(requesterIdentity, request.RequestID, ex.Message);
            }
        }

        /// <summary>
        /// If a historical data request can't be filled,
        /// this method sends a reply with the relevant error.
        /// </summary>
        private void SendErrorReply(string requesterIdentity, int requestID, string message)
        {
            //this is a 4 part message
            //1st message part: the identity string of the client that we're routing the data to
            _routerSocket.SendMoreFrame(requesterIdentity);

            //2nd message part: the type of reply we're sending
            _routerSocket.SendMoreFrame("ERROR");

            //3rd message part: the request ID
            _routerSocket.SendMoreFrame(BitConverter.GetBytes(requestID));

            //4th message part: the error
            _routerSocket.SendFrame(message);
        }

        /// <summary>
        /// Add a message to the log.
        ///</summary>
        private void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }

        public void Dispose()
        {
            StopServer();

            if (_routerSocket != null)
            {
                _routerSocket.Dispose();
                _routerSocket = null;
            }
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }
    }
}