// -----------------------------------------------------------------------
// <copyright file="RealTimeDataBroker.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using NLog;
using ProtoBuf;
using QDMS;
using QDMSServer.DataSources;
using ZeroMQ;
using Timer = System.Timers.Timer;

namespace QDMSServer
{
    public class RealTimeDataBroker : IDisposable
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
        /// Holds the real time data sources.
        /// </summary>
        public Dictionary<string, IRealTimeDataSource> DataSources { get; private set; }

        /// <summary>
        /// Holds the active data streams. They key KVP consists of key: request ID, value: data source name
        /// </summary>
        public ConcurrentNotifierDictionary<KeyValuePair<int, string>, RealTimeDataRequest> ActiveStreams { get; private set; }
        //todo keep track of the requests for data streams and stop receiving them when all clients have canceled

        /// <summary>
        /// Is true if the server is running
        /// </summary>
        public bool ServerRunning { get; private set; }
        
        ///<summary>
        ///When bars arrive, the data source raises an event
        ///the event adds the data to the _arrivedBars
        ///then the publishing server sends out the data
        ///</summary>
        private readonly BlockingCollection<RealTimeDataEventArgs> _arrivedBars;

        private Thread _requestThread;
        private bool _runServer = true;
        private ZmqContext _context;
        private ZmqSocket _pubSocket;
        private ZmqSocket _reqSocket;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private Timer _connectionTimer; //tries to reconnect every once in a while
        private MemoryStream _ms;
        private object _pubSocketLock = new object();

        public void Dispose()
        {
            if (_ms != null)
            {
                _ms.Dispose();
                _ms = null;
            }
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
            if (_connectionTimer != null)
            {
                _connectionTimer.Dispose();
                _connectionTimer = null;
            }
            if (DataSources.ContainsKey("Interactive Brokers"))
            {
                ((IB)DataSources["Interactive Brokers"]).Dispose();
            }
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
            if (_arrivedBars != null)
                _arrivedBars.Dispose();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pubPort">The port to use for the publishing server.</param>
        /// <param name="reqPort">The port to use for the request server.</param>
        public RealTimeDataBroker(int pubPort, int reqPort)
        {
            if (pubPort == reqPort) throw new Exception("Publish and request ports must be different");
            PublisherPort = pubPort;
            RequestPort = reqPort;
            _connectionTimer = new Timer(10000);
            _connectionTimer.Elapsed += ConnectionTimerElapsed;
            _connectionTimer.Start();

            DataSources = new Dictionary<string, IRealTimeDataSource> 
            {
                {"SIM", new RealTimeSim()}, 
                {"Interactive Brokers", new IB()}
            };

            //we need to set the appropriate event methods for every data source
            foreach (IRealTimeDataSource s in DataSources.Values)
            {
                s.DataReceived += RealTimeData;
                s.Disconnected += SourceDisconnects;
                s.Error += s_Error;
            }

            ActiveStreams = new ConcurrentNotifierDictionary<KeyValuePair<int, string>, RealTimeDataRequest>();
            _arrivedBars = new BlockingCollection<RealTimeDataEventArgs>();

            _ms = new MemoryStream();

            //connect to our data sources
            TryConnect();

            //finally start listening and stuff
            StartServer();
        }

        /// <summary>
        /// When one of the data sources has some sort of error, it raises an event which is handled by this method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void s_Error(object sender, ErrorArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() => _logger.Log(LogLevel.Error, string.Format("RTB: {0} - {1}", e.ErrorCode, e.ErrorMessage)));
        }

        /// <summary>
        /// When one of the data sources receives new real time data, it raises an event which is handled by this method,
        /// which then forwards the data over the PUB socket after serializing it.
        /// </summary>
        public void RealTimeData(object sender, RealTimeDataEventArgs e)
        {
            lock (_pubSocketLock)
            {
                _ms.SetLength(0);
                _pubSocket.SendMore(Encoding.UTF8.GetBytes(e.Symbol)); //start by sending the ticker before the data
                Serializer.Serialize(_ms, e);
                _pubSocket.Send(_ms.ToArray()); //then send the serialized bar
            }

#if DEBUG
            Application.Current.Dispatcher.Invoke(() => Log(LogLevel.Trace, 
                string.Format("RTD Received Symbol: {0} O:{1} H:{2} L:{3} C:{4} V:{5} T:{6}",
                    e.Symbol,
                    e.Open,
                    e.High,
                    e.Low,
                    e.Close,
                    e.Volume,
                    e.Time)));
#endif
        }

        //this function is here because events may execute on other threads, and therefore can't use the logger on this one and must call the dispatcher
        private void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }

        //tells the servers to stop running and waits for the threads to shut down.
        public void StopServer()
        {
            _runServer = false;

            //clear the socket and context and say it's not running any more
            if (_pubSocket != null)
                _pubSocket.Dispose();

            _requestThread.Join();
        }


        /// <summary>
        /// Starts the publishing and request servers.
        /// </summary>
        public void StartServer()
        {
            if (!ServerRunning) //only start if it isn't running already
            {
                _runServer = true;
                _context = ZmqContext.Create();
                
                //the publisher socket
                _pubSocket = _context.CreateSocket(SocketType.PUB);
                _pubSocket.Bind("tcp://*:" + PublisherPort);

                //the request socket
                _reqSocket = _context.CreateSocket(SocketType.REP);
                _reqSocket.Bind("tcp://*:" + RequestPort);
                
                _requestThread = new Thread(RequestServer) {Name = "RTDB Request Thread"};

                //clear queue before starting?
                _requestThread.Start();
            }
            ServerRunning = true;
        }


        /// <summary>
        /// This method runs on its own thread. The loop receives requests and sends the appropriate reply.
        /// Can request a ping or to open a new real time data stream.
        /// </summary>
        private void RequestServer()
        {
            TimeSpan timeout = new TimeSpan(50000);
            int receivedBytes;
            MemoryStream ms = new MemoryStream();
            while (_runServer)
            {
                string requestType = _reqSocket.Receive(Encoding.UTF8, timeout);
                if (requestType == null) continue;

                //handle ping requests
                if (requestType == "PING")
                {
                    _reqSocket.Send("PONG", Encoding.UTF8);
                    continue;
                }

                //Handle real time data requests
                if (requestType == "RTD") //Two part message: first, "RTD" string. Then the RealTimeDataRequest object.
                {
                    byte[] buffer = _reqSocket.Receive(null, timeout, out receivedBytes);
                    if (receivedBytes > 0)
                    {
                        ms.Write(buffer, 0, receivedBytes);
                        ms.Position = 0;
                        var request = Serializer.Deserialize<RealTimeDataRequest>(ms);

                        //with the current approach we can't handle multiple real time data streams from
                        //the same symbol and data source, but at different frequencies

                        //if there is already an active stream of this instrument
                        if (ActiveStreams.Dictionary.All(x => x.Value.Instrument != request.Instrument) &&
                            DataSources.ContainsKey(request.Instrument.Datasource.Name) &&
                            DataSources[request.Instrument.Datasource.Name].Connected)
                        {
                            //send the request to the correct data source
                            int reqID = DataSources[request.Instrument.Datasource.Name].RequestRealTimeData(request);

                            //log the request
                            Application.Current.Dispatcher.InvokeAsync(() => Log(LogLevel.Info,
                                string.Format("RTD Request: {0} from {1} @ {2} ID:{3}",
                                request.Instrument.Symbol,
                                request.Instrument.Datasource.Name,
                                Enum.GetName(typeof(BarSize), request.Frequency),
                                reqID)));

                            //add the request to the active streams, though it's not necessarily active yet
                            ActiveStreams.TryAdd(new KeyValuePair<int, string>(reqID, request.Instrument.Datasource.Name), request);

                            //and report success back to the requesting client
                            _reqSocket.SendMore("SUCCESS", Encoding.UTF8);
                            //along with the symbol of the instrument
                            _reqSocket.Send(request.Instrument.Symbol, Encoding.UTF8);
                        }
                        else //no new request was made, send the client the reason why
                        {
                            _reqSocket.SendMore("ERROR", Encoding.UTF8);
                            if (!DataSources.ContainsKey(request.Instrument.Datasource.Name))
                                _reqSocket.Send("No such data source", Encoding.UTF8);
                            else if (!DataSources[request.Instrument.Datasource.Name].Connected)
                                _reqSocket.Send("Data source not connected", Encoding.UTF8);
                            else
                                _reqSocket.Send("Stream already exists for this instrument", Encoding.UTF8);
                        }
                    }
                    continue;
                }

                //manage cancellation requests
                if (requestType == "CANCEL")
                {
                    //todo write
                }
            }

            //clear the socket and context and say it's not running any more
            _reqSocket.Dispose();

            ms.Dispose();
            ServerRunning = false;
        }



        /// <summary>
        /// This method is called when a data source disconnects
        /// </summary>
        private void SourceDisconnects(object sender, DataSourceDisconnectEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                Log(LogLevel.Info, string.Format("Real Time Data Broker: Data source {0} disconnected", e.SourceName))
            );
        }

        /// <summary>
        /// There is a timer which periodically calls the tryconnect function to connect to any disconnected data sources
        /// </summary>
        private void ConnectionTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(TryConnect);
        }

        /// <summary>
        /// Loops through data sources and tries to connect to those that are disconnected
        /// </summary>
        private void TryConnect()
        {
            foreach (var s in DataSources)
            {
                if (!s.Value.Connected)
                {
                    _logger.Log(LogLevel.Info, string.Format("Real Time Data Broker: Trying to connect to data source {0}", s.Key));
                    s.Value.Connect();
                }
            }
        }

    }
}
