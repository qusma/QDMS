﻿// -----------------------------------------------------------------------
// <copyright file="RealTimeDataServer.cs" company="">
//     Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NetMQ;
using NetMQ.Sockets;
using NLog;
using ProtoBuf;
using QDMS;
using System;
using System.IO;
using System.Text;

// ReSharper disable once CheckNamespace
namespace QDMSApp
{
    /// <summary>
    /// This class handles networking for real time data data. Clients send their requests through ZeroMQ. Here they are parsed and then
    /// forwarded to the RealTimeDataBroker. Data sent from the RealTimeDataBroker is sent out to the clients. Two types of possible requests:
    /// 1. To start receiving real time data
    /// 2. To cancel a real time data stream
    /// </summary>
    public class RealTimeDataServer : IDisposable, IRealTimeDataServer
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IRealTimeDataBroker _broker;
        private readonly string _publisherConnectionString;
        private readonly string _requestConnectionString;
        private readonly object _publisherSocketLock = new object();
        private readonly object _requestSocketLock = new object();

        private NetMQSocket _publisherSocket;
        private NetMQSocket _requestSocket;
        private NetMQPoller _poller;

        /// <summary>
        /// Whether the server is running or not.
        /// </summary>
        public bool ServerRunning => _poller != null && _poller.IsRunning;

        #region IDisposable implementation

        public void Dispose()
        {
            StopServer();
        }

        #endregion IDisposable implementation

        public RealTimeDataServer(ISettings settings, IRealTimeDataBroker broker)
        {
            if (broker == null)
            {
                throw new ArgumentNullException(nameof(broker), $"{nameof(broker)} cannot be null");
            }

            if (settings.rtDBPubPort == settings.rtDBReqPort)
            {
                throw new ArgumentException("Publish and request ports must be different");
            }

            _publisherConnectionString = $"tcp://*:{settings.rtDBPubPort}";
            _requestConnectionString = $"tcp://*:{settings.rtDBReqPort}";

            _broker = broker;
            _broker.RealTimeDataArrived += BrokerRealTimeDataArrived;
            _broker.RealTimeTickArrived += BrokerRealTimeTickArrived;
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void StartServer()
        {
            if (ServerRunning)
            {
                return;
            }

            lock (_publisherSocketLock)
            {
                _publisherSocket = new PublisherSocket(_publisherConnectionString);
            }

            lock (_requestSocketLock)
            {
                _requestSocket = new ResponseSocket(_requestConnectionString);
                _requestSocket.ReceiveReady += RequestSocketReceiveReady;
            }

            _poller = new NetMQPoller { _requestSocket };
            _poller.RunAsync();
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            if (!ServerRunning)
            {
                return;
            }

            _poller?.Stop();
            _poller?.Dispose();

            ClosePublisherSocket();

            closeReqSocket();

            _poller = null;
        }

        private void closeReqSocket()
        {
            lock (_requestSocketLock)
            {
                if (_requestSocket != null)
                {
                    try
                    {
                        _requestSocket.Disconnect(_requestConnectionString);
                    }
                    finally
                    {
                        _requestSocket.ReceiveReady -= RequestSocketReceiveReady;
                        _requestSocket.Close();
                        _requestSocket = null;
                    }
                }
            }
        }

        private void ClosePublisherSocket()
        {
            lock (_publisherSocketLock)
            {
                if (_publisherSocket != null)
                {
                    try
                    {
                        _publisherSocket.Disconnect(_publisherConnectionString);
                    }
                    finally
                    {
                        _publisherSocket.Close();
                        _publisherSocket = null;
                    }
                }
            }
        }

        #region Event handlers

        /// <summary>
        /// When data arrives from an external data source to the broker, this event is fired.
        /// </summary>
        private void BrokerRealTimeDataArrived(object sender, RealTimeDataEventArgs e)
        {
            lock (_publisherSocketLock)
            {
                if (_publisherSocket == null) return;

                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, e);
                    _publisherSocket.SendMoreFrame(Encoding.UTF8.GetBytes($"{e.InstrumentID}~{(int)e.Frequency}")); // Start by sending the id+freq before the data
                    _publisherSocket.SendMoreFrame(MessageType.RealTimeBars);
                    _publisherSocket.SendFrame(ms.ToArray()); // Then send the serialized bar
                }
            }
        }

        /// <summary>
        /// When tick data arrives from an external data source to the broker, this event is fired.
        /// </summary>
        private void BrokerRealTimeTickArrived(object sender, TickEventArgs e)
        {
            lock (_publisherSocketLock)
            {
                if (_publisherSocket == null) return;

                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, e);
                    _publisherSocket.SendMoreFrame(Encoding.UTF8.GetBytes($"{e.Tick.InstrumentID}~{(int)BarSize.Tick}")); // Start by sending the id+freq before the data
                    _publisherSocket.SendMoreFrame(MessageType.RealTimeTick);
                    _publisherSocket.SendFrame(ms.ToArray()); // Then send the serialized tick
                }
            }
        }

        private void RequestSocketReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            lock (_requestConnectionString)
            {
                if (_requestSocket == null)
                {
                    return;
                }

                var requestType = _requestSocket.ReceiveFrameString();

                if (requestType == null)
                {
                    _logger.Warn("Received RTD request with empty requestType");
                    return;
                }

                HandleRequest(requestType);
            }
        }

        private void HandleRequest(string requestType)
        {
            if (requestType.Equals(MessageType.Ping, StringComparison.InvariantCultureIgnoreCase))
            {
                //Ping
                HandlePingRequest();
            }
            else if (requestType.Equals(MessageType.RTDRequest, StringComparison.InvariantCultureIgnoreCase))
            {
                //Real time data req
                HandleRealTimeDataRequest();
            }
            else if (requestType.Equals(MessageType.CancelRTD, StringComparison.InvariantCultureIgnoreCase))
            {
                //Cancellation requests
                HandleRealTtimeDataCancelRequest();
            }
        }

        private void HandlePingRequest()
        {
            _requestSocket.SendFrame(MessageType.Pong);
        }

        #endregion Event handlers

        /// <summary>
        /// Accept a real time data request
        /// </summary>
        private void HandleRealTimeDataRequest()
        {
            // Two part message: first, "RTD" string.Then the RealTimeDataRequest object.
            using (var ms = new MemoryStream())
            {
                bool hasMore;
                var buffer = _requestSocket.ReceiveFrameBytes(out hasMore);
                var request = MyUtils.ProtoBufDeserialize<RealTimeDataRequest>(buffer, ms);
                // Make sure the ID and data sources are set

                //TODO: separate validator
                var id = request?.Instrument?.ID;
                if (!id.HasValue)
                {
                    SendErrorReply("Instrument had no ID set.", buffer);
                    _logger.Error("Instrument with no ID requested.");

                    return;
                }

                if (request?.Instrument?.Datasource == null)
                {
                    SendErrorReply("Instrument had no data source set.", buffer);
                    _logger.Error("Instrument with no data source requested.");

                    return;
                }

                try
                {
                    //Forward the request to the broker
                    _broker.RequestRealTimeData(request);

                    //And report success back to the requesting client
                    _requestSocket.SendMoreFrame(MessageType.Success);
                    //Along with the request
                    _requestSocket.SendFrame(MyUtils.ProtoBufSerialize(request, ms));
                }
                catch (Exception ex)
                {
                    SendErrorReply(ex.Message, buffer);

                    _logger.Error($"RTDS: Error handling RTD request {request.Instrument.Symbol} @ {request.Instrument.Datasource} ({request.Frequency}): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Accept a request to cancel a real time data stream Obviously we only actually cancel it if
        /// </summary>
        private void HandleRealTtimeDataCancelRequest()
        {
            //Three part message: first: MessageType.CancelRTD. Second: the instrument ID. Third: frequency.
            bool hasMore;
            var buffer = _requestSocket.ReceiveFrameBytes(out hasMore);

            using (var ms = new MemoryStream())
            {
                //parse
                var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);
                var freqBuff = _requestSocket.ReceiveFrameBytes(out hasMore);
                BarSize frequency = MyUtils.ProtoBufDeserialize<BarSize>(freqBuff, ms);

                //cancel
                if (instrument.ID != null)
                {
                    _broker.CancelRTDStream(instrument.ID.Value, frequency);
                }

                // Reply message:
                // 1: MessageType.RTDCanceled
                // 2: the instrument symbol
                // 3: the frequency
                _requestSocket.SendMoreFrame(MessageType.RTDCanceled);
                _requestSocket.SendMoreFrame(instrument.Symbol);
                _requestSocket.SendFrame(freqBuff);
            }
        }

        private void SendErrorReply(string message, byte[] serializedRequest)
        {
            _requestSocket.SendMoreFrame(MessageType.Error);
            _requestSocket.SendMoreFrame(message);
            _requestSocket.SendFrame(serializedRequest);
        }
    }
}