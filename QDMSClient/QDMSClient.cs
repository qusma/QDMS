﻿// -----------------------------------------------------------------------
// <copyright file="QDMSClient.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using K4os.Compression.LZ4;
using NetMQ;
using NetMQ.Sockets;
using QDMS;

namespace QDMSClient
{
    // ReSharper disable once InconsistentNaming
    /// <inheritdoc />
    public partial class QDMSClient : IDataClient
    {
        private const int HistoricalDataRequestsPeriodInSeconds = 1;
        private const int HeartBeatPeriodInSeconds = 1;

        #region Variables
        // Where to connect
        private readonly string _realTimeRequestConnectionString;
        private readonly string _realTimeDataConnectionString;
        private readonly string _historicalDataConnectionString;
        /// <summary>
        /// This holds the zeromq identity string that we'll be using.
        /// </summary>
        private readonly string _name;
        /// <summary>
        /// Queue of historical data requests waiting to be sent out.
        /// </summary>
        private readonly ConcurrentQueue<HistoricalDataRequest> _historicalDataRequests;
        private readonly object _realTimeRequestSocketLock = new object();
        private readonly object _realTimeDataSocketLock = new object();
        private readonly object _historicalDataSocketLock = new object();
        private readonly object _pendingHistoricalRequestsLock = new object();
        private readonly object _realTimeDataStreamsLock = new object();

        /// <summary>
        /// This socket sends requests for real time data.
        /// </summary>
        private DealerSocket _realTimeRequestSocket;
        /// <summary>
        /// This socket receives real time data.
        /// </summary>
        private SubscriberSocket _realTimeDataSocket;
        /// <summary>
        /// This socket sends requests for and receives historical data.
        /// </summary>
        private DealerSocket _historicalDataSocket;
        /// <summary>
        /// Pooler class to manage all used sockets.
        /// </summary>
        private NetMQPoller _poller;
        /// <summary>
        /// Periodically sends out requests for historical data and receives data when requests are fulfilled.
        /// </summary>
        private NetMQTimer _historicalDataTimer;
        /// <summary>
        /// Periodically sends heartbeat messages to server to ensure the connection is up.
        /// </summary>
        private NetMQTimer _heartBeatTimer;
        /// <summary>
        /// The time when the last heartbeat was received. If it's too long ago we're disconnected.
        /// </summary>
        private DateTime _lastHeartBeat;
        /// <summary>
        /// This int is used to give each historical request a unique RequestID.
        /// Keep in mind this is unique to the CLIENT. AssignedID is unique to the server.
        /// </summary>
        private int _requestCount;

        private bool _connected;

        #endregion

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

        /// <summary>
        /// Keeps track of historical requests that have been sent but the data has not been received yet.
        /// </summary>
        public ObservableCollection<HistoricalDataRequest> PendingHistoricalRequests { get; } = new ObservableCollection<HistoricalDataRequest>();

        /// <summary>
        /// Keeps track of live real time data streams.
        /// </summary>
        public ObservableCollection<RealTimeDataRequest> RealTimeDataStreams { get; } = new ObservableCollection<RealTimeDataRequest>();

        #region IDisposable implementation

        /// <inheritdoc />
        public void Dispose()
        {
            Disconnect();
        }
        #endregion

        #region IDataClient implementation

        /// <summary>
        /// Fires when real time bars are received
        /// </summary>
        public event EventHandler<RealTimeDataEventArgs> RealTimeDataReceived;

        /// <summary>
        /// Fires when real time ticks are received
        /// </summary>
        public event EventHandler<TickEventArgs> RealTimeTickReceived;

        /// <summary>
        /// Fires when historical bars are received
        /// </summary>
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataReceived;

        /// <summary>
        /// Event used to surface errors
        /// </summary>
        public event EventHandler<ErrorArgs> Error;

        /// <summary>
        ///     Pushes data to local storage.
        /// </summary>
        public void PushData(DataAdditionRequest request)
        {
            if (request == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Request cannot be null."));

                return;
            }

            if (request.Instrument?.ID == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Instrument must be set and have an ID."));

                return;
            }

            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not push historical data to local storage - not connected."));

                return;
            }

            lock (_historicalDataSocketLock)
            {
                if (_historicalDataSocket != null)
                {
                    _historicalDataSocket.SendMoreFrame(MessageType.HistPush);

                    using (var ms = new MemoryStream())
                    {
                        _historicalDataSocket.SendFrame(MyUtils.ProtoBufSerialize(request, ms));
                    }
                }
            }
        }


        /// <summary>
        ///     Request historical data. Data will be delivered through the HistoricalDataReceived event.
        /// </summary>
        /// <returns>An ID uniquely identifying this historical data request. -1 if there was an error.</returns>
        public int RequestHistoricalData(HistoricalDataRequest request)
        {
            if (request == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Historical Data Request Failed: Request cannot be null."));

                return -1;
            }

            if (request.EndingDate < request.StartingDate)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Historical Data Request Failed: Starting date must be after ending date."));

                return -1;
            }

            if (request.Instrument == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Historical Data Request Failed: Instrument cannot be null."));

                return -1;
            }

            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not request historical data - not connected."));

                return -1;
            }

            if (!request.RTHOnly && request.Frequency >= BarSize.OneDay && request.DataLocation != DataLocation.ExternalOnly)
            {
                RaiseEvent(
                    Error,
                    this,
                    new ErrorArgs(
                        -1,
                        "Warning: Requesting low-frequency data outside RTH should be done with DataLocation = ExternalOnly, data from local storage will be incorrect."));
            }

            request.RequestID = Interlocked.Increment(ref _requestCount);

            lock (_pendingHistoricalRequestsLock)
            {
                PendingHistoricalRequests.Add(request);
            }

            _historicalDataRequests.Enqueue(request);

            return request.RequestID;
        }

        /// <summary>
        ///     Request a new real time data stream. Data will be delivered through the RealTimeDataReceived event.
        /// </summary>
        /// <returns>An ID uniquely identifying this real time data request. -1 if there was an error.</returns>
        public int RequestRealTimeData(RealTimeDataRequest request)
        {
            if (request == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Real Time Data Request Failed: Request cannot be null."));

                return -1;
            }

            if (request.Instrument == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Real Time Data Request Failed: null Instrument."));

                return -1;
            }

            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not request real time data - not connected."));

                return -1;
            }

            request.RequestID = Interlocked.Increment(ref _requestCount);

            lock (_realTimeRequestSocketLock)
            {
                if (_realTimeRequestSocket != null)
                {
                    // Two part message:
                    // 1: "RTD"
                    // 2: serialized RealTimeDataRequest
                    _realTimeRequestSocket.SendMoreFrame(string.Empty);
                    _realTimeRequestSocket.SendMoreFrame(MessageType.RTDRequest);

                    using (var ms = new MemoryStream())
                    {
                        _realTimeRequestSocket.SendFrame(MyUtils.ProtoBufSerialize(request, ms));
                    }
                }
            }

            return request.RequestID;
        }

        /// <summary>
        ///     Tries to connect to the QDMS server.
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
        ///     Disconnects from the server.
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

            _poller = null;

            _apiClient.Dispose();
        }


        /// <summary>
        ///     Cancel a live real time data stream.
        /// </summary>
        public void CancelRealTimeData(Instrument instrument, BarSize frequency)
        {
            if (instrument == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Instrument cannot be null."));

                return;
            }

            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not cancel real time data - not connected."));

                return;
            }

            lock (_realTimeRequestSocketLock)
            {
                if (_realTimeRequestSocket != null)
                {
                    // Two part message:
                    // 1: "CANCEL"
                    // 2: serialized Instrument object
                    // 3: frequency
                    _realTimeRequestSocket.SendMoreFrame(string.Empty);
                    _realTimeRequestSocket.SendMoreFrame(MessageType.CancelRTD);

                    using (var ms = new MemoryStream())
                    {
                        _realTimeRequestSocket.SendMoreFrame(MyUtils.ProtoBufSerialize(instrument, ms));
                        _realTimeRequestSocket.SendFrame(MyUtils.ProtoBufSerialize(frequency, ms));
                    }
                }
            }

            lock (_realTimeDataSocketLock)
            {
                _realTimeDataSocket?.Unsubscribe(Encoding.UTF8.GetBytes($"{instrument.ID.Value}~{(int)frequency}"));
            }

            lock (_realTimeDataStreamsLock)
            {
                RealTimeDataStreams.RemoveAll(x => x.Instrument.ID == instrument.ID && x.Frequency == frequency);
            }
        }

        #endregion

        /// <summary>
        ///     Initialization constructor.
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
        

        #region Event handlers
        /// <summary>
        ///     Process replies on the request socket.
        ///     Heartbeats, errors, and subscribing to real time data streams.
        /// </summary>
        private void RealTimeRequestSocketReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            lock (_realTimeRequestSocketLock)
            {
                using (var ms = new MemoryStream())
                {
                    var reply = _realTimeRequestSocket?.ReceiveFrameString();

                    if (reply == null)
                    {
                        return;
                    }

                    reply = _realTimeRequestSocket.ReceiveFrameString();

                    if (reply.Equals(MessageType.Pong, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Reply to heartbeat message
                        _lastHeartBeat = DateTime.Now;
                    }
                    else if (reply.Equals(MessageType.Error, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Something went wrong
                        // First the message
                        var error = _realTimeRequestSocket.ReceiveFrameString();
                        // Then the request
                        var buffer = _realTimeRequestSocket.ReceiveFrameBytes();
                        var request = MyUtils.ProtoBufDeserialize<RealTimeDataRequest>(buffer, ms);
                        // Error event
                        RaiseEvent(Error, this, new ErrorArgs(-1, "Real time data request error: " + error, request.RequestID));
                    }
                    else if (reply.Equals(MessageType.Success, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Successful request to start a new real time data stream
                        // Receive the request
                        var buffer = _realTimeRequestSocket.ReceiveFrameBytes();
                        var request = MyUtils.ProtoBufDeserialize<RealTimeDataRequest>(buffer, ms);
                        // Add it to the active streams
                        lock (_realTimeDataStreamsLock)
                        {
                            RealTimeDataStreams.Add(request);
                        }
                        // TODO: Solve issue with null request
                        // Request worked, so we subscribe to the stream
                        _realTimeDataSocket.Subscribe(Encoding.UTF8.GetBytes($"{request.Instrument.ID.Value}~{(int)request.Frequency}"));
                    }
                    else if (reply.Equals(MessageType.RTDCanceled, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Successful cancelation of a real time data stream
                        // Also receive the symbol and then frequency
                        var symbol = _realTimeRequestSocket.ReceiveFrameString();
                        var freq = _realTimeRequestSocket.ReceiveFrameBytes();
                        // Nothing to do?
                    }
                }
            }
        }

        private void RealTimeDataSocketReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            lock (_realTimeDataSocketLock)
            {
                if (_realTimeDataSocket == null)
                {
                    return;
                }

                _realTimeDataSocket.ReceiveFrameBytes(out bool hasMore);
                if (!hasMore) return;

                var type = _realTimeDataSocket.ReceiveFrameString();
                var buffer = _realTimeDataSocket.ReceiveFrameBytes();

                using (var ms = new MemoryStream())
                {
                    if (type == MessageType.RealTimeBars)
                    {
                        var bar = MyUtils.ProtoBufDeserialize<RealTimeDataEventArgs>(buffer, ms);

                        RaiseEvent(RealTimeDataReceived, null, bar);
                    }
                    else if (type == MessageType.RealTimeTick)
                    {
                        var bar = MyUtils.ProtoBufDeserialize<TickEventArgs>(buffer, ms);

                        RaiseEvent(RealTimeTickReceived, null, bar);
                    }
                }
            }
        }

        /// <summary>
        ///     Handling replies to a data push, a historical data request, or an available data request
        /// </summary>
        private void HistoricalDataSocketReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            lock (_historicalDataSocketLock)
            {
                if (_historicalDataSocket == null)
                {
                    return;
                }
                // 1st message part: what kind of stuff we're receiving
                var type = _historicalDataSocket.ReceiveFrameString();

                if (type.Equals(MessageType.HistPushReply, StringComparison.InvariantCultureIgnoreCase))
                {
                    HandleDataPushReply();
                }
                else if (type.Equals(MessageType.HistReply, StringComparison.InvariantCultureIgnoreCase))
                {
                    HandleHistoricalDataRequestReply();
                }
                else if (type.Equals(MessageType.Error, StringComparison.InvariantCultureIgnoreCase))
                {
                    HandleErrorReply();
                }
            }
        }
        #endregion

        #region Timer handlers
        /// <summary>
        ///     Sends heartbeat messages so we know that the server is still up.
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
        ///     Sends out requests for historical data and raises an event when it's received.
        /// </summary>
        private void HistoricalDataTimerElapsed(object sender, NetMQTimerEventArgs e)
        {
            if (!Connected)
            {
                return;
            }
            // TODO: Solve issue with _poller and socket in Disconnect method and here
            while (!_historicalDataRequests.IsEmpty)
            {
                HistoricalDataRequest request;

                if (_historicalDataRequests.TryDequeue(out request))
                {
                    using (var ms = new MemoryStream())
                    {
                        var buffer = MyUtils.ProtoBufSerialize(request, ms);

                        lock (_historicalDataSocketLock)
                        {
                            if (PollerRunning && _historicalDataSocket != null)
                            {
                                _historicalDataSocket.SendMoreFrame(MessageType.HistRequest);
                                _historicalDataSocket.SendFrame(buffer);
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        /// <summary>
        ///     Called when we get some sort of error reply
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
        ///     Called on a reply to a data push
        /// </summary>
        private void HandleDataPushReply()
        {
            var result = _historicalDataSocket.ReceiveFrameString();

            if (result == MessageType.Success) // Everything is alright
            { }
            else if (result == MessageType.Error)
            {
                // Receive the error
                var error = _historicalDataSocket.ReceiveFrameString();

                RaiseEvent(Error, this, new ErrorArgs(-1, "Data push error: " + error));
            }
        }

        /// <summary>
        ///     Called on a reply to a historical data request
        /// </summary>
        private void HandleHistoricalDataRequestReply()
        {
            using (var ms = new MemoryStream())
            {
                // 2nd message part: the HistoricalDataRequest object that was used to make the request
                bool hasMore;
                var requestBuffer = _historicalDataSocket.ReceiveFrameBytes(out hasMore);
                if (!hasMore) return;

                var request = MyUtils.ProtoBufDeserialize<HistoricalDataRequest>(requestBuffer, ms);
                // 3rd message part: the size of the uncompressed, serialized data. Necessary for decompression.
                var sizeBuffer = _historicalDataSocket.ReceiveFrameBytes(out hasMore);
                if (!hasMore) return;

                var outputSize = BitConverter.ToInt32(sizeBuffer, 0);
                // 4th message part: the compressed serialized data.
                var dataBuffer = _historicalDataSocket.ReceiveFrameBytes();
                var decompressed = new byte[outputSize];
                LZ4Codec.Decode(dataBuffer, 0, dataBuffer.Length, decompressed, 0, outputSize);
                var data = MyUtils.ProtoBufDeserialize<List<OHLCBar>>(decompressed, ms);
                // Remove from pending requests
                lock (_pendingHistoricalRequestsLock)
                {
                    PendingHistoricalRequests.RemoveAll(x => x.RequestID == request.RequestID);
                }

                RaiseEvent(HistoricalDataReceived, this, new HistoricalDataEventArgs(request, data));
            }
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

        //These can be removed in a subsequent version
        /// <summary>
        /// Obsolete
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use GetInstruments instead")]
        public List<Instrument> GetAllInstruments() => GetInstruments().Result.Result;
        /// <summary>
        /// Obsolete
        /// </summary>
        /// <param name="pred"></param>
        /// <returns></returns>
        [Obsolete("Use GetInstruments instead")]
        public List<Instrument> FindInstruments(Expression<Func<Instrument, bool>> pred) =>
            GetInstruments(pred).Result.Result;
        /// <summary>
        /// Obsolete
        /// </summary>
        /// <param name="instrument"></param>
        /// <returns></returns>
        [Obsolete("Use GetInstruments instead")]
        public List<Instrument> FindInstruments(Instrument instrument = null) =>
            GetInstruments(instrument).Result.Result;

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