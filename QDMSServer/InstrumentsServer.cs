// -----------------------------------------------------------------------
// <copyright file="InstrumentsServer.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using LZ4;
using NLog;
using QDMS;
using QDMS.Annotations;
using ZeroMQ;

namespace QDMSServer
{
    public class InstrumentsServer : IDisposable, INotifyPropertyChanged
    {
        private ZmqContext _context;
        private ZmqSocket _socket;
        private Logger _logger = LogManager.GetCurrentClassLogger();
        private bool _runServer;
        private readonly int _socketPort;
        private Thread _serverThread;

        public bool Running { get { return _runServer; } }

        public void Dispose()
        {
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

        public InstrumentsServer(int port)
        {
            _socketPort = port;

            //start the server
            StartServer();
        }

        public void StartServer()
        {
            if (_runServer) return;

            _runServer = true;
            _context = ZmqContext.Create();
            _serverThread = new Thread(ContractServer);
            _serverThread.Name = "Instrument Server Loop";
            _serverThread.Start();
        }

        public void StopServer()
        {
            _runServer = false;
            //if (_serverThread.ThreadState == ThreadState.Running)
                _serverThread.Join();
        }

        private void ContractServer()
        {
            var timeout = new TimeSpan(100000);
            _socket = _context.CreateSocket(SocketType.REP);
            _socket.Bind("tcp://*:" + _socketPort);
            var ms = new MemoryStream();
            List<Instrument> instruments;
            var mgr = new InstrumentManager();

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

                    instruments = mgr.FindInstruments(null, searchInstrument);
                }
                else if (request == "ALL") //if the request is for all the instruments, we don't need to receive anything else
                {
                    instruments = mgr.FindInstruments();
                }
                else if (request == "ADD") //request to add instrument
                {
                    int size;
                    byte[] buffer = _socket.Receive(null, timeout, out size);
                    var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);

                    bool addResult = InstrumentManager.AddInstrument(instrument);
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

     

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
