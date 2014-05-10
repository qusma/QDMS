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
using System.Text;
using System.Threading;
using System.Windows;
using LZ4;
using NLog;
using ProtoBuf;
using QDMS;
using ZeroMQ;

namespace QDMSServer
{
    public class HistoricalDataServer : IDisposable
    {
        /// <summary>
        /// Whether the broker is running or not.
        /// </summary>
        public bool ServerRunning { get; set; }

        private readonly Thread _serverThread;
        private ZmqContext _context;
        private ZmqSocket _routerSocket;
        private bool _runServer = true;
        private readonly int _listenPort;
        private IHistoricalDataBroker _broker;
        private readonly ConcurrentQueue<KeyValuePair<HistoricalDataRequest, List<OHLCBar>>> _dataQueue;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public HistoricalDataServer(int port, IHistoricalDataBroker broker = null)
        {
            _listenPort = port;

            _dataQueue = new ConcurrentQueue<KeyValuePair<HistoricalDataRequest, List<OHLCBar>>>();

            _broker = broker ?? new HistoricalDataBroker();
            _broker.HistoricalDataArrived += _broker_HistoricalDataArrived;

            _serverThread = new Thread(Server);
            _serverThread.Name = "HDB Thread";
        }

        private void _broker_HistoricalDataArrived(object sender, HistoricalDataEventArgs e)
        {
            _dataQueue.Enqueue(new KeyValuePair<HistoricalDataRequest, List<OHLCBar>>(e.Request, e.Data));
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

            if (_serverThread != null && _serverThread.ThreadState == ThreadState.Running)
                _serverThread.Join();
        }

        /// <summary>
        /// Receives new requests by polling, and sends data when it has arrived
        /// </summary>
        private void Server()
        {
            var timeout = TimeSpan.FromMilliseconds(10);
            _routerSocket.ReceiveReady += socket_ReceiveReady;
            KeyValuePair<HistoricalDataRequest, List<OHLCBar>> newDataItem;
            var ms = new MemoryStream();

            using (var poller = new Poller(new[] { _routerSocket }))
            {
                while (_runServer)
                {
                    poller.Poll(timeout); //this'll trigger the ReceiveReady event when we've got an incoming request

                    //check if there's anything in the queue, if there is we want to send it
                    if (_dataQueue.TryDequeue(out newDataItem))
                    {
                        SendFilledHistoricalRequest(newDataItem.Key, newDataItem.Value, ms);
                    }
                }
            }

            ms.Dispose();
            _routerSocket.Dispose();
            _context.Dispose();
            ServerRunning = false;
        }

        /// <summary>
        /// Given a historical data request and the data that fill it,
        /// send the reply to the client who made the request.
        /// </summary>
        private void SendFilledHistoricalRequest(HistoricalDataRequest request, List<OHLCBar> data, MemoryStream ms)
        {
            //this is a 5 part message
            //1st message part: the identity string of the client that we're routing the data to
            string clientIdentity = request.RequesterIdentity;
            _routerSocket.SendMore(clientIdentity, Encoding.UTF8);

            //2nd message part: the type of reply we're sending
            _routerSocket.SendMore("HISTREQREP", Encoding.UTF8);

            //3rd message part: the HistoricalDataRequest object that was used to make the request
            _routerSocket.SendMore(MyUtils.ProtoBufSerialize(request, ms));

            //4th message part: the size of the uncompressed, serialized data. Necessary for decompression on the client end.
            byte[] uncompressed = MyUtils.ProtoBufSerialize(data, ms);
            _routerSocket.SendMore(BitConverter.GetBytes(uncompressed.Length));

            //5th message part: the compressed serialized data.
            byte[] compressed = LZ4Codec.EncodeHC(uncompressed, 0, uncompressed.Length); //compress
            _routerSocket.Send(compressed);
        }

        /// <summary>
        /// This is called when a new historical data request or data push request is made.
        /// </summary>
        private void socket_ReceiveReady(object sender, SocketEventArgs e)
        {
            //Here we process the first two message parts: first, the identity string of the client
            string requesterIdentity = e.Socket.Receive(Encoding.UTF8);

            //second: the string specifying the type of request
            string text = e.Socket.Receive(Encoding.UTF8);
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
            Log(LogLevel.Info, string.Format("Received local data storage info request for {0}.",
                instrument.Symbol));

            //and send the reply

            var storageInfo = _broker.GetAvailableDataInfo(instrument);
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
            Log(LogLevel.Info, string.Format("Received data push request for {0}.",
                request.Instrument.Symbol));

            //start building the reply
            socket.SendMore(requesterIdentity, Encoding.UTF8);
            socket.SendMore("PUSHREP", Encoding.UTF8);
            try
            {
                _broker.AddData(request);
                socket.Send("OK", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                socket.SendMore("ERROR", Encoding.UTF8);
                socket.Send(ex.Message, Encoding.UTF8);
            }
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
            _routerSocket.SendMore(requesterIdentity, Encoding.UTF8);

            //2nd message part: the type of reply we're sending
            _routerSocket.SendMore("ERROR", Encoding.UTF8);

            //3rd message part: the request ID
            _routerSocket.SendMore(BitConverter.GetBytes(requestID));

            //4th message part: the error
            _routerSocket.Send(message, Encoding.UTF8);
        }

        /// <summary>
        /// Add a message to the log.
        ///</summary>
        private void Log(LogLevel level, string message)
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.InvokeAsync(() =>
                    _logger.Log(level, message));
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