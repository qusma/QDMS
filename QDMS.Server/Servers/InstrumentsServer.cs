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

using LZ4;

using NetMQ;
using NetMQ.Sockets;

using NLog;

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

            var instruments = new List<Instrument>();

            using (var ms = new MemoryStream())
            {
                // If the request is for a search, receive the instrument w/ the search parameters and pass it to the searcher
                if (request == "SEARCH" && hasMore)
                {
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
                    }
                }
                else if (request == "ALL") // If the request is for all the instruments, we don't need to receive anything else
                {
                    _logger.Info("Instruments Server: received request for list of all instruments.");

                    try
                    {
                        instruments = _instrumentManager.FindInstruments();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Instruments Server: Instrument search error: {ex.Message}");
                    }
                }
                else if (request == "ADD" && hasMore) // Request to add instrument
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

                    _socket.SendMoreFrame(addedInstrument != null ? "SUCCESS" : "FAILURE");

                    _socket.SendFrame(MyUtils.ProtoBufSerialize(addedInstrument, ms));

                    return;
                }
                else // No request = loop again
                {
                    return;
                }

                var uncompressed = MyUtils.ProtoBufSerialize(instruments, ms); // Serialize the list of instruments

                ms.Read(uncompressed, 0, (int)ms.Length); // Get the uncompressed data

                var result = LZ4Codec.Encode(uncompressed, 0, (int)ms.Length); // Compress it
                // Before we send the result we must send the length of the uncompressed array, because it's needed for decompression
                _socket.SendMoreFrame(BitConverter.GetBytes(uncompressed.Length));
                // Then finally send the results
                _socket.SendFrame(result);
            }
        }
    }
}