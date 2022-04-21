// -----------------------------------------------------------------------
// <copyright file="QDMSClient.cs" company="">
//     Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NetMQ;
using NetMQ.Sockets;
using QDMS;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace QDMSClient
{
    // ReSharper disable once InconsistentNaming
    /// <inheritdoc/>
    public partial class QDMSClient : IDataClient
    {
        private const int HistoricalDataRequestsPeriodInSeconds = 1;
        private const int HeartBeatPeriodInSeconds = 1;

        // Where to connect
        private readonly string _realTimeDataConnectionString;
        private readonly string _realTimeRequestConnectionString;
        private readonly string _historicalDataConnectionString;

        /// <summary>
        /// This holds the zeromq identity string that we'll be using.
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// Pooler class to manage all used sockets.
        /// </summary>
        private NetMQPoller _poller;

        /// <summary>
        /// Periodically sends heartbeat messages to server to ensure the connection is up.
        /// </summary>
        private NetMQTimer _heartBeatTimer;

        /// <summary>
        /// The time when the last heartbeat was received. If it's too long ago we're disconnected.
        /// </summary>
        private DateTime _lastHeartBeat;

        /// <summary>
        /// This int is used to give each historical request a unique RequestID. Keep in mind this is unique to the CLIENT. AssignedID is
        /// unique to the server.
        /// </summary>
        private int _requestCount;

        private bool _connected;

        private bool PollerRunning => (_poller != null) && _poller.IsRunning;

        /// <summary>
        /// Connected to server
        /// </summary>
        public bool Connected
        {
            get => _connected;
            set
            {
                if (value == _connected) return;
                _connected = value;
                OnPropertyChanged();
            }
        }

        #region IDisposable implementation

        /// <inheritdoc/>
        public void Dispose()
        {
            Disconnect();
        }

        #endregion IDisposable implementation

        /// <summary>
        /// Event used to surface errors
        /// </summary>
        public event EventHandler<ErrorArgs> Error;

        /// <summary>
        /// Tries to connect to the QDMS server.
        /// </summary>
        public void Connect()
        {
            if (PollerRunning)
            {
                return;
            }

            lock (_realTimeRequestSocketLock)
            {
                _realTimeRequestSocket = new DealerSocket(_realTimeRequestConnectionString);
                _realTimeRequestSocket.Options.Identity = Encoding.UTF8.GetBytes(_name);
                // Start off by sending a ping to make sure everything is regular
                var reply = string.Empty;

                try
                {
                    _realTimeRequestSocket.SendMoreFrame(string.Empty).SendFrame(MessageType.Ping);

                    if (_realTimeRequestSocket.TryReceiveFrameString(TimeSpan.FromSeconds(1), out reply))
                    {
                        //first reply is an empty frame that starts the REP message, second is "PONG"
                        _realTimeRequestSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(50), out reply);
                    }
                }
                catch
                {
                    Disconnect();
                    return;
                }

                if (reply == null || !reply.Equals(MessageType.Pong, StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        _realTimeRequestSocket.Disconnect(_realTimeRequestConnectionString);
                    }
                    finally
                    {
                        _realTimeRequestSocket.Close();
                        _realTimeRequestSocket = null;
                    }

                    RaiseEvent(Error, this, new ErrorArgs(-1, "Could not connect to server."));

                    return;
                }

                Connected = true;

                _realTimeRequestSocket.ReceiveReady += RealTimeRequestSocketReceiveReady;
            }

            lock (_realTimeDataSocketLock)
            {
                _realTimeDataSocket = new SubscriberSocket(_realTimeDataConnectionString);
                _realTimeDataSocket.Options.Identity = Encoding.UTF8.GetBytes(_name);
                _realTimeDataSocket.ReceiveReady += RealTimeDataSocketReceiveReady;
            }

            lock (_historicalDataSocketLock)
            {
                _historicalDataSocket = new DealerSocket(_historicalDataConnectionString);
                _historicalDataSocket.Options.Identity = Encoding.UTF8.GetBytes(_name);
                _historicalDataSocket.ReceiveReady += HistoricalDataSocketReceiveReady;
            }

            _lastHeartBeat = DateTime.Now;

            _heartBeatTimer = new NetMQTimer(TimeSpan.FromSeconds(HeartBeatPeriodInSeconds));
            _heartBeatTimer.Elapsed += HeartBeatTimerElapsed;

            _historicalDataTimer = new NetMQTimer(TimeSpan.FromSeconds(HistoricalDataRequestsPeriodInSeconds));
            _historicalDataTimer.Elapsed += HistoricalDataTimerElapsed;

            _poller = new NetMQPoller { _realTimeRequestSocket, _realTimeDataSocket, _historicalDataSocket, _heartBeatTimer, _historicalDataTimer };

            _poller.RunAsync();
        }


        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect(bool cancelStreams = true)
        {
            if (!PollerRunning)
            {
                return;
            }
            // Start by canceling all active real time streams
            if (cancelStreams)
            {
                while (RealTimeDataStreams.Count > 0)
                {
                    var stream = RealTimeDataStreams.First();
                    CancelRealTimeData(stream.Instrument, stream.Frequency);
                }
            }
            Connected = false;
            _poller?.Stop();
            _poller?.Dispose();

            CloseSockets();

            _poller = null;

            _apiClient.Dispose();
        }


        private void CloseSockets()
        {
            lock (_realTimeRequestSocketLock)
            {
                if (_realTimeRequestSocket != null)
                {
                    try
                    {
                        _realTimeRequestSocket.Disconnect(_realTimeRequestConnectionString);
                    }
                    finally
                    {
                        _realTimeRequestSocket.ReceiveReady -= RealTimeRequestSocketReceiveReady;
                        _realTimeRequestSocket.Close();
                        _realTimeRequestSocket = null;
                    }
                }
            }

            lock (_realTimeDataSocketLock)
            {
                if (_realTimeDataSocket != null)
                {
                    try
                    {
                        _realTimeDataSocket.Disconnect(_realTimeDataConnectionString);
                    }
                    finally
                    {
                        _realTimeDataSocket.ReceiveReady -= RealTimeDataSocketReceiveReady;
                        _realTimeDataSocket.Close();
                        _realTimeDataSocket = null;
                    }
                }
            }

            lock (_historicalDataSocket)
            {
                if (_historicalDataSocket != null)
                {
                    try
                    {
                        _historicalDataSocket.Disconnect(_historicalDataConnectionString);
                    }
                    finally
                    {
                        _historicalDataSocket.ReceiveReady -= HistoricalDataSocketReceiveReady;
                        _historicalDataSocket.Close();
                        _historicalDataSocket = null;
                    }
                }
            }
        }

        
        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="clientName">The name of this client. Should be unique. Used to route historical data.</param>
        /// <param name="host">The address of the server.</param>
        /// <param name="realTimeRequestPort">The port used for real time data requsts.</param>
        /// <param name="realTimePublishPort">The port used for publishing new real time data.</param>
        /// <param name="historicalDataPort">The port used for historical data.</param>
        /// <param name="httpPort">The port used for the REST API.</param>
        /// <param name="apiKey">The authentication key for the REST API.</param>
        /// <param name="useSsl">Use an encrypted connection for the REST API.</param>
        public QDMSClient(string clientName, string host, int realTimeRequestPort, int realTimePublishPort, int historicalDataPort, int httpPort, string apiKey, bool useSsl = false)
        {
            _name = clientName;

            _realTimeRequestConnectionString = $"tcp://{host}:{realTimeRequestPort}";
            _realTimeDataConnectionString = $"tcp://{host}:{realTimePublishPort}";
            _historicalDataConnectionString = $"tcp://{host}:{historicalDataPort}";

            _historicalDataRequests = new ConcurrentQueue<HistoricalDataRequest>();

            _apiClient = new ApiClient(host, httpPort, apiKey, useSsl);
        }


        private void HandleErrorReply(MemoryStream ms)
        {
            // Something went wrong First the message
            var error = _realTimeRequestSocket.ReceiveFrameString();
            // Then the request
            var buffer = _realTimeRequestSocket.ReceiveFrameBytes();
            var request = MyUtils.ProtoBufDeserialize<RealTimeDataRequest>(buffer, ms);
            // Error event
            RaiseEvent(Error, this, new ErrorArgs(-1, "Real time data request error: " + error, request.RequestID));
        }


        private void HandleHeartbeatReply()
        {
            // Reply to heartbeat message
            _lastHeartBeat = DateTime.Now;
        }


        /// <summary>
        /// Sends heartbeat messages so we know that the server is still up.
        /// </summary>
        private void HeartBeatTimerElapsed(object sender, NetMQTimerEventArgs e)
        {
            lock (_realTimeRequestSocketLock)
            {
                if (PollerRunning && _realTimeRequestSocket != null)
                {
                    _realTimeRequestSocket.SendMoreFrame(string.Empty);
                    _realTimeRequestSocket.SendFrame(MessageType.Ping);
                }
            }

            //update connection status
            Connected = PollerRunning && ((DateTime.Now - _lastHeartBeat).TotalSeconds < 5);
        }

        
        /// <summary>
        /// Called when we get some sort of error reply
        /// </summary>
        private void HandleErrorReply()
        {
            // The request ID
            bool hasMore;
            var buffer = _historicalDataSocket.ReceiveFrameBytes(out hasMore);
            if (!hasMore) return;
            var requestId = BitConverter.ToInt32(buffer, 0);
            // Remove from pending requests
            lock (_pendingHistoricalRequestsLock)
            {
                PendingHistoricalRequests.RemoveAll(x => x.RequestID == requestId);
            }
            // Finally the error message
            var message = _historicalDataSocket.ReceiveFrameString();
            // Raise the error event
            RaiseEvent(Error, this, new ErrorArgs(-1, message, requestId));
        }


        /// <summary>
        /// Raise the event in a threadsafe manner
        /// </summary>
        /// <param name="event"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <typeparam name="T"></typeparam>
        private static void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
            where T : EventArgs
        {
            var handler = @event;

            handler?.Invoke(sender, e);
        }

        /// <summary>
        /// PropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// OnPropertyChanged
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}