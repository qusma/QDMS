// -----------------------------------------------------------------------
// <copyright file="InstrumentsServer.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// This class gets requests for the instruments we have in our metadata db:
// either a list of all of them, or a search for a specific instrument.
// It also receives requests to add new instruments.
// Works simply in a loop with a REP socket, and no asynchronous operations.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Xml.Serialization;
using LZ4;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using MetaLinq;
using NLog.Fluent;
using QDMS;

// ReSharper disable once CheckNamespace
namespace QDMSServer
{
    public class InstrumentsServer : IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _connectionString;
        private readonly IInstrumentSource _instrumentManager;
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

        public InstrumentsServer(int port, IInstrumentSource instrumentManager)
        {
            if (instrumentManager == null)
            {
                throw new ArgumentNullException(nameof(instrumentManager), $"{nameof(instrumentManager)} cannot be null");
            }

            _connectionString = $"tcp://*:{port}";
            _instrumentManager = instrumentManager;
        }

        /// <summary>
        ///     Starts the server.
        /// </summary>
        public void StartServer()
        {
            if (ServerRunning)
            {
                return;
            }

            lock (_socketLock)
            {
                _socket = new ResponseSocket(_connectionString);
                _socket.ReceiveReady += SocketReceiveReady;
            }

            _poller = new NetMQPoller { _socket };
            _poller.RunAsync();
        }

        /// <summary>
        ///     Stops the server.
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

        private void SocketReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var hasMore = false;
            var request = string.Empty;

            lock (_socketLock)
            {
                var receiveResult = _socket?.TryReceiveFrameString(out request, out hasMore);

                if (!receiveResult.HasValue || !receiveResult.Value || string.IsNullOrEmpty(request))
                {
                    return;
                }
            }

            // If the request is for a search, receive the instrument w/ the search parameters and pass it to the searcher
            if (request == MessageType.Search && hasMore)
            {
                HandleSearchRequest();
            }
            else if (request == MessageType.PredicateSearch && hasMore)
            {
                HandlePredicateSearchRequest();
            }
            else if (request == MessageType.AllInstruments) // If the request is for all the instruments, we don't need to receive anything else
            {
                HandleAllInstrumentsRequest();
            }
            else if (request == MessageType.AddInstrument && hasMore) // Request to add instrument
            {
                HandleInstrumentAdditionRequest();
            }
        }

        private void HandleInstrumentAdditionRequest()
        {
            using (var ms = new MemoryStream())
            {
                var buffer = _socket.ReceiveFrameBytes();
                var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

                _logger.Info($"Instruments Server: Received instrument addition request. Instrument: {instrument}");

                Instrument addedInstrument;

                try
                {
                    addedInstrument = _instrumentManager.AddInstrument(instrument);
                }
                catch (Exception ex)
                {
                    addedInstrument = null;

                    _logger.Error($"Instruments Server: Instrument addition error: {ex.Message}");
                }

                _socket.SendMoreFrame(addedInstrument != null ? MessageType.Success : MessageType.Error);

                _socket.SendFrame(MyUtils.ProtoBufSerialize(addedInstrument, ms));
            }
        }

        private void HandleAllInstrumentsRequest()
        {
            _logger.Log(LogLevel.Info, "Instruments Server: received request for list of all instruments.");
            var instruments = _instrumentManager.FindInstruments();
            ReplyWithFoundInstruments(instruments);
        }

        private void HandleSearchRequest()
        {
            using (var ms = new MemoryStream())
            {
                List<Instrument> instruments;
                var buffer = _socket.ReceiveFrameBytes();
                var searchInstrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

                _logger.Info($"Instruments Server: Received search request: {searchInstrument}");

                try
                {
                    instruments = _instrumentManager.FindInstruments(null, searchInstrument);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Instruments Server: Instrument search error: {ex.Message}");
                    instruments = new List<Instrument>();
                }

                ReplyWithFoundInstruments(instruments);
            }
        }

        private void HandlePredicateSearchRequest()
        {
            List<Instrument> instruments;
            byte[] buffer = _socket.ReceiveFrameBytes();
            var ms = new MemoryStream(buffer);

            var xs = new XmlSerializer(typeof(EditableExpression),
                new[] { typeof(MetaLinq.Expressions.EditableLambdaExpression) });

            try
            {
                //Deserialize LINQ expression and pass it to the instrument manager
                var editableExp = (EditableExpression)xs.Deserialize(ms);
                var expression = (Expression<Func<Instrument, bool>>)editableExp.ToExpression();
                instruments = _instrumentManager.FindInstruments(expression);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Instruments Server: Expression search error: {ex.Message}");
                instruments = new List<Instrument>();
            }

            ReplyWithFoundInstruments(instruments);
        }

        private void ReplyWithFoundInstruments(List<Instrument> instruments)
        {
            using (var ms = new MemoryStream())
            {
                byte[] uncompressed = MyUtils.ProtoBufSerialize(instruments, ms); //serialize the list of instruments
                ms.Read(uncompressed, 0, (int)ms.Length); //get the uncompressed data
                byte[] result = LZ4Codec.Encode(uncompressed, 0, (int)ms.Length); //compress it

                //before we send the result we must send the length of the uncompressed array, because it's needed for decompression
                _socket.SendMoreFrame(BitConverter.GetBytes(uncompressed.Length));

                //then finally send the results
                _socket.SendFrame(result);
            }
        }
    }
}