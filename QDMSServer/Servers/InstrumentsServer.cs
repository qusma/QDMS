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
using System.Threading.Tasks;
using System.Windows;
using LZ4;
using NetMQ.zmq;
using NLog;
using QDMS;
using NetMQ;
using Poller = NetMQ.Poller;

namespace QDMSServer
{
    public class InstrumentsServer : IDisposable
    {
        private NetMQContext _context;
        private NetMQSocket _socket;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private bool _runServer;
        private readonly int _socketPort;
        private readonly IInstrumentSource _instrumentManager;
        private Poller _poller;

        public bool Running { get { return _runServer; } }

        public void Dispose()
        {
            if (Running)
                StopServer();

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        public InstrumentsServer(int port, IInstrumentSource instrumentManager = null)
        {
            _socketPort = port;
            
            _instrumentManager = instrumentManager ?? new InstrumentManager();
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void StartServer()
        {
            if (_runServer) return;

            _runServer = true;
            _context = NetMQContext.Create();

            _socket = _context.CreateSocket(NetMQ.zmq.ZmqSocketType.Rep);
            _socket.Bind("tcp://*:" + _socketPort);
            _socket.ReceiveReady += _socket_ReceiveReady;
            _poller = new Poller(new[] { _socket });

            Task.Factory.StartNew(_poller.Start, TaskCreationOptions.LongRunning);
        }

        void _socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var ms = new MemoryStream();
            List<Instrument> instruments;
            bool hasMore;
            string request = _socket.ReceiveString(SendReceiveOptions.DontWait, out hasMore);
            if (request == null) return;

            //if the request is for a search, receive the instrument w/ the search parameters and pass it to the searcher
            if (request == "SEARCH" && hasMore)
            {
                byte[] buffer = _socket.Receive();
                var searchInstrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

                Log(LogLevel.Info, string.Format("Instruments Server: Received search request: {0}",
                    searchInstrument));

                try
                {
                    instruments = _instrumentManager.FindInstruments(null, searchInstrument);
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, string.Format("Instruments Server: Instrument search error: {0}",
                        ex.Message));
                    instruments = new List<Instrument>();
                }
            }
            else if (request == "ALL") //if the request is for all the instruments, we don't need to receive anything else
            {
                Log(LogLevel.Info, "Instruments Server: received request for list of all instruments.");
                instruments = _instrumentManager.FindInstruments();
            }
            else if (request == "ADD" && hasMore) //request to add instrument
            {
                byte[] buffer = _socket.Receive();
                var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

                Log(LogLevel.Info, string.Format("Instruments Server: Received instrument addition request. Instrument: {0}",
                    instrument));

                Instrument addedInstrument;
                try
                {
                    addedInstrument = _instrumentManager.AddInstrument(instrument);
                }
                catch (Exception ex)
                {
                    addedInstrument = null;
                    Log(LogLevel.Error, string.Format("Instruments Server: Instrument addition error: {0}",
                        ex.Message));
                }
                _socket.SendMore(addedInstrument != null ? "SUCCESS" : "FAILURE");

                _socket.Send(MyUtils.ProtoBufSerialize(addedInstrument, ms));

                return;
            }
            else //no request = loop again
            {
                return;
            }

            byte[] uncompressed = MyUtils.ProtoBufSerialize(instruments, ms);//serialize the list of instruments
            ms.Read(uncompressed, 0, (int)ms.Length); //get the uncompressed data
            byte[] result = LZ4Codec.Encode(uncompressed, 0, (int)ms.Length); //compress it

            //before we send the result we must send the length of the uncompressed array, because it's needed for decompression
            _socket.SendMore(BitConverter.GetBytes(uncompressed.Length));

            //then finally send the results
            _socket.Send(result);
        }

        /// <summary>
        /// Stops the server from running.
        /// </summary>
        public void StopServer()
        {
            _runServer = false;
            if (_poller != null && _poller.IsStarted)
            {
                _poller.Stop(true);
            }
        }

        /// <summary>
        /// Add a log item.
        /// </summary>
        private void Log(LogLevel level, string message)
        {
            if(Application.Current != null)
                Application.Current.Dispatcher.InvokeAsync(() =>
                    _logger.Log(level, message));
        }
    }
}