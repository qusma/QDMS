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

using K4os.Compression.LZ4;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using ProtoBuf;
using QDMS;
using System;
using System.Collections.Generic;
using System.IO;

// ReSharper disable once CheckNamespace
namespace QDMSApp
{
    public class HistoricalDataServer : IDisposable, IHistoricalDataServer
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _connectionString;
        private readonly IHistoricalDataBroker _broker;
        private readonly object _socketLock = new object();

        private NetMQSocket _socket;
        private NetMQPoller _poller;

        /// <summary>
        ///     Whether the server is running or not.
        /// </summary>
        public bool ServerRunning => _poller != null && _poller.IsRunning;

        #region IDisposable implementation
        public void Dispose()
        {
            StopServer();
        }
        #endregion

        public HistoricalDataServer(ISettings settings, IHistoricalDataBroker broker)
        {
            if (broker == null)
            {
                throw new ArgumentNullException(nameof(broker), $"{nameof(broker)} cannot be null");
            }

            _connectionString = $"tcp://*:{settings.hDBPort}";
            _broker = broker;
            _broker.HistoricalDataArrived += BrokerHistoricalDataArrived;
        }

        /// <summary>
        ///     Start the server.
        /// </summary>
        public void StartServer()
        {
            if (ServerRunning)
            {
                return;
            }

            lock (_socketLock)
            {
                _socket = new RouterSocket(_connectionString);
                _socket.ReceiveReady += SocketReceiveReady;
            }

            _poller = new NetMQPoller { _socket };
            _poller.RunAsync();
        }

        /// <summary>
        ///     Stop the server.
        /// </summary>
        public void StopServer()
        {
            if (!ServerRunning)
            {
                return;
            }

            _poller?.Stop();
            _poller?.Dispose();

            lock (_socketLock)
            {
                if (_socket != null)
                {
                    try
                    {
                        _socket.Disconnect(_connectionString);
                    }
                    finally
                    {
                        _socket.ReceiveReady -= SocketReceiveReady;
                        _socket.Close();
                        _socket = null;
                    }
                }
            }

            _poller = null;
        }

        #region Event handlers
        private void BrokerHistoricalDataArrived(object sender, HistoricalDataEventArgs e)
        {
            SendFilledHistoricalRequest(e.Request, e.Data);
        }

        /// <summary>
        ///     This is called when a new historical data request or data push request is made.
        /// </summary>
        private void SocketReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            string requesterIdentity;
            string text;

            lock (_socketLock)
            {
                // Here we process the first two message parts: first, the identity string of the client
                requesterIdentity = _socket?.ReceiveFrameString() ?? string.Empty;
                // Second: the string specifying the type of request
                text = _socket?.ReceiveFrameString() ?? string.Empty;
            }

            if (text.Equals(MessageType.HistRequest, StringComparison.InvariantCultureIgnoreCase)) // The client wants to request some data
            {
                AcceptHistoricalDataRequest(requesterIdentity);
            }
            else if (text.Equals(MessageType.HistPush, StringComparison.InvariantCultureIgnoreCase)) // The client wants to push some data into the db
            {
                AcceptDataAdditionRequest(requesterIdentity);
            }
            else
            {
                _logger.Info($"Unrecognized request to historical data broker: {text}");
            }
        }
        #endregion

        /// <summary>
        ///     Given a historical data request and the data that fill it,
        ///     send the reply to the client who made the request.
        /// </summary>
        private void SendFilledHistoricalRequest(HistoricalDataRequest request, List<OHLCBar> data)
        {
            lock (_socketLock)
            {
                if (_socket != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        // This is a 5 part message
                        // 1st message part: the identity string of the client that we're routing the data to
                        var clientIdentity = request.RequesterIdentity;

                        _socket.SendMoreFrame(clientIdentity ?? string.Empty);
                        // 2nd message part: the type of reply we're sending
                        _socket.SendMoreFrame(MessageType.HistReply);
                        // 3rd message part: the HistoricalDataRequest object that was used to make the request
                        _socket.SendMoreFrame(MyUtils.ProtoBufSerialize(request, ms));
                        // 4th message part: the size of the uncompressed, serialized data. Necessary for decompression on the client end.
                        var uncompressed = MyUtils.ProtoBufSerialize(data, ms);

                        _socket.SendMoreFrame(BitConverter.GetBytes(uncompressed.Length));
                        // 5th message part: the compressed serialized data.
                        var compressed = new byte[LZ4Codec.MaximumOutputSize(uncompressed.Length)];
                        var targetSpan = new Span<byte>(compressed);

                        int compressedLength = LZ4Codec.Encode(new Span<byte>(uncompressed), targetSpan, LZ4Level.L03_HC);
                        //var compressed = LZ4Codec.EncodeHC(uncompressed, 0, uncompressed.Length); // compress

                        _socket.SendFrame(targetSpan.Slice(0, compressedLength).ToArray());
                    }
                }
            }
        }



        /// <summary>
        ///     Handles incoming data "push" requests: the client sends data for us to add to local storage.
        /// </summary>
        private void AcceptDataAdditionRequest(string requesterIdentity)
        {
            lock (_socketLock)
            {
                if (_socket != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        // Final message part: receive the DataAdditionRequest object
                        bool hasMore;
                        var buffer = _socket.ReceiveFrameBytes(out hasMore);
                        var request = MyUtils.ProtoBufDeserialize<DataAdditionRequest>(buffer, ms);

                        _logger.Info($"Received data push request for {request.Instrument.Symbol}.");
                        // Start building the reply
                        _socket.SendMoreFrame(requesterIdentity);
                        _socket.SendMoreFrame(MessageType.HistPushReply);

                        try
                        {
                            _broker.AddData(request);

                            _socket.SendFrame(MessageType.Success);
                        }
                        catch (Exception ex)
                        {
                            _socket.SendMoreFrame(MessageType.Error);
                            _socket.SendFrame(ex.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Processes incoming historical data requests.
        /// </summary>
        private void AcceptHistoricalDataRequest(string requesterIdentity)
        {
            lock (_socketLock)
            {
                if (_socket != null)
                {
                    // Third: a serialized HistoricalDataRequest object which contains the details of the request
                    bool hasMore;
                    var buffer = _socket.ReceiveFrameBytes(out hasMore);

                    using (var ms = new MemoryStream())
                    {
                        ms.Write(buffer, 0, buffer.Length);
                        ms.Position = 0;

                        var request = Serializer.Deserialize<HistoricalDataRequest>(ms);

                        _logger.Info(
                            "Historical Data Request from client {0}: {7} {1} @ {2} from {3} to {4} Location: {5} {6:;;SaveToLocal}",
                            requesterIdentity,
                            request.Instrument.Symbol,
                            Enum.GetName(typeof(BarSize), request.Frequency),
                            request.StartingDate,
                            request.EndingDate,
                            request.DataLocation,
                            request.SaveDataToStorage ? 0 : 1,
                            request.Instrument.Datasource.Name);

                        request.RequesterIdentity = requesterIdentity;

                        try
                        {
                            _broker.RequestHistoricalData(request);
                        }
                        catch (Exception ex)
                        {
                            // There's some sort of problem with fulfilling the request. Inform the client.
                            SendErrorReply(requesterIdentity, request.RequestID, ex.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     If a historical data request can't be filled,
        ///     this method sends a reply with the relevant error.
        /// </summary>
        private void SendErrorReply(string requesterIdentity, int requestId, string message)
        {
            lock (_socketLock)
            {
                if (_socket != null)
                {
                    // This is a 4 part message
                    // 1st message part: the identity string of the client that we're routing the data to
                    _socket.SendMoreFrame(requesterIdentity);
                    // 2nd message part: the type of reply we're sending
                    _socket.SendMoreFrame(MessageType.Error);
                    // 3rd message part: the request ID
                    _socket.SendMoreFrame(BitConverter.GetBytes(requestId));
                    // 4th message part: the error
                    _socket.SendFrame(message);
                }
            }
        }
    }
}