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
using System.Text;
using System.Threading;
using System.Windows;
using LZ4;
using NLog;
using QDMS;
using ZeroMQ;


namespace QDMSServer
{
    public class InstrumentsServer : IDisposable
    {
        private ZmqContext _context;
        private ZmqSocket _socket;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private bool _runServer;
        private readonly int _socketPort;
        private Thread _serverThread;
        private readonly IInstrumentSource _instrumentManager;

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

            if (instrumentManager == null)
                _instrumentManager = new InstrumentManager();

            //start the server
            StartServer();
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void StartServer()
        {
            if (_runServer) return;

            _runServer = true;
            _context = ZmqContext.Create();
            _serverThread = new Thread(ContractServer) {Name = "Instrument Server Loop"};
            _serverThread.Start();
        }

        /// <summary>
        /// Stops the server from running.
        /// </summary>
        public void StopServer()
        {
            _runServer = false;
            _serverThread.Join();
        }

        /// <summary>
        /// The main loop. Runs on its own thread. Accepts requests on the REP socket, gets results from the InstrumentManager,
        /// and sends them back right away.
        /// </summary>
        private void ContractServer()
        {
            var timeout = new TimeSpan(100000);
            _socket = _context.CreateSocket(SocketType.REP);
            _socket.Bind("tcp://*:" + _socketPort);
            var ms = new MemoryStream();
            List<Instrument> instruments;

            while (_runServer)
            {
                string request = _socket.Receive(Encoding.UTF8, timeout);
                if(request == null) continue;
                
                //if the request is for a search, receive the instrument w/ the search parameters and pass it to the searcher
                if (request == "SEARCH" && _socket.ReceiveMore)
                {
                    int size;
                    byte[] buffer = _socket.Receive(null, timeout, out size);
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
                else if (request == "ADD") //request to add instrument
                {
                    int size;
                    byte[] buffer = _socket.Receive(null, timeout, out size);
                    var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

                    bool addResult;
                    try
                    {
                        addResult = InstrumentManager.AddInstrument(instrument);
                    }
                    catch (Exception ex)
                    {
                        addResult = false;
                        Log(LogLevel.Error, string.Format("Instruments Server: Instrument addition error: {0}",
                            ex.Message));
                    }
                    _socket.Send(addResult ? "SUCCESS" : "FAILURE", Encoding.UTF8);

                    continue;
                }
                else //no request = loop again
                {
                    continue;
                }

                byte[] uncompressed = MyUtils.ProtoBufSerialize(instruments, ms);//serialize the list of instruments
                ms.Read(uncompressed, 0, (int)ms.Length); //get the uncompressed data
                byte[] result = LZ4Codec.Encode(uncompressed, 0, (int)ms.Length); //compress it

                //before we send the result we must send the length of the uncompressed array, because it's needed for decompression
                _socket.SendMore(BitConverter.GetBytes(uncompressed.Length));

                //then finally send the results
                _socket.Send(result);
            }

            _socket.Dispose();
        }

        /// <summary>
        /// Add a log item.
        /// </summary>
        private void Log(LogLevel level, string message)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
                _logger.Log(level, message));
        }
    }
}
