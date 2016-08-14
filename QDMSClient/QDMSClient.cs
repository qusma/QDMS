// -----------------------------------------------------------------------
// <copyright file="QDMSClient.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using LZ4;
using MetaLinq;
using NetMQ;
using NetMQ.Sockets;
using ProtoBuf;
using QDMS;

// TODO: Fix comments at the end of lines
namespace QDMSClient
{
    // ReSharper disable once InconsistentNaming
    public class QDMSClient : IDataClient
    {
        private const int HistoricalDataRequestsPeriodInSeconds = 1;
        private const int HeartBeatPeriodInSeconds = 1;

        #region Variables
        // Where to connect
        private readonly string _realTimeRequestConnectionString;
        private readonly string _realTimeDataConnectionString;
        private readonly string _instrumentServerConnectionString;
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
        #endregion

        private bool PollerRunning => (_poller != null) && _poller.IsRunning;

        public bool Connected => PollerRunning && ((DateTime.Now - _lastHeartBeat).TotalSeconds < 5);

        /// <summary>
        /// Keeps track of historical requests that have been sent but the data has not been received yet.
        /// </summary>
        public ObservableCollection<HistoricalDataRequest> PendingHistoricalRequests { get; } = new ObservableCollection<HistoricalDataRequest>();

        /// <summary>
        /// Keeps track of live real time data streams.
        /// </summary>
        public ObservableCollection<RealTimeDataRequest> RealTimeDataStreams { get; } = new ObservableCollection<RealTimeDataRequest>();

        #region IDisposable implementation
        public void Dispose()
        {
            Disconnect();
        }
        #endregion

        #region IDataClient implementation
        public event EventHandler<RealTimeDataEventArgs> RealTimeDataReceived;

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataReceived;

        public event EventHandler<LocallyAvailableDataInfoReceivedEventArgs> LocallyAvailableDataInfoReceived;

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
        ///     Requests information on what historical data is available in local storage for this instrument.
        /// </summary>
        public void GetLocallyAvailableDataInfo(Instrument instrument)
        {
            if (instrument == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Instrument cannot be null."));

                return;
            }

            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not request local historical data - not connected."));

                return;
            }

            lock (_historicalDataSocketLock)
            {
                if (_historicalDataSocket != null)
                {
                    _historicalDataSocket.SendMoreFrame(MessageType.AvailableDataRequest);

                    using (var ms = new MemoryStream())
                    {
                        _historicalDataSocket.SendFrame(MyUtils.ProtoBufSerialize(instrument, ms));
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
            if (Connected)
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
                    CancelRealTimeData(RealTimeDataStreams.First().Instrument);
                }
            }

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
        }

        /// <summary>
        ///     Query the server for contracts matching a particular set of features.
        /// </summary>
        /// <param name="instrument">An Instrument object; any features that are not null will be search parameters. If null, all instruments are returned.</param>
        /// <returns>A list of instruments matching these features.</returns>
        public List<Instrument> FindInstruments(Instrument instrument = null)
        {
            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not request instruments - not connected."));

                return new List<Instrument>();
            }

            using (var s = new RequestSocket(_instrumentServerConnectionString))
            {
                using (var ms = new MemoryStream())
                {
                    if (instrument == null) // All contracts
                    {
                        s.SendFrame(MessageType.AllInstruments);
                    }
                    else // An actual search
                    {
                        s.SendMoreFrame(MessageType.Search); // First we send a search request
                        // Then we need to serialize and send the instrument
                        s.SendFrame(MyUtils.ProtoBufSerialize(instrument, ms));
                    }

                    return ReceiveInstrumentSearchResults(s);
                }
            }
        }

        /// <summary>
        ///     Query the server for contracts using a predicate
        /// </summary>
        /// <param name="pred">Predicate to match instruments against</param>
        /// <returns>A list of instruments matching these features.</returns>
        public List<Instrument> FindInstruments(Expression<Func<Instrument, bool>> pred)
        {
            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not request instruments - not connected."));
                return new List<Instrument>();
            }

            using (var s = new RequestSocket(_instrumentServerConnectionString))
            {
                using (var ms = new MemoryStream())
                {
                    EditableExpression mutable = EditableExpression.CreateEditableExpression(pred);
                    XmlSerializer xs = new XmlSerializer(typeof(EditableExpression),
                        new[] { typeof(MetaLinq.Expressions.EditableLambdaExpression) });
                    xs.Serialize(ms, mutable);

                    //Send the request
                    s.SendMoreFrame(MessageType.PredicateSearch);
                    s.SendFrame(ms.ToArray());
                }

                //And return the result
                return ReceiveInstrumentSearchResults(s);
            }
        }

        private List<Instrument> ReceiveInstrumentSearchResults(RequestSocket s)
        {
            using (var ms = new MemoryStream())
            {
                // First we receive the size of the final uncompressed byte[] array
                bool hasMore;
                var sizeBuffer = s.ReceiveFrameBytes(out hasMore);

                if (sizeBuffer.Length == 0)
                {
                    RaiseEvent(Error, this, new ErrorArgs(-1, "Contract request failed, received no reply."));

                    return new List<Instrument>();
                }

                var outputSize = BitConverter.ToInt32(sizeBuffer, 0);
                // Then the actual data
                var buffer = s.ReceiveFrameBytes(out hasMore);

                if (buffer.Length == 0)
                {
                    RaiseEvent(Error, this, new ErrorArgs(-1, "Contract request failed, received no data."));

                    return new List<Instrument>();
                }

                try
                {
                    // Then we process it by first decompressing
                    ms.SetLength(0);
                    var decoded = LZ4Codec.Decode(buffer, 0, buffer.Length, outputSize);
                    ms.Write(decoded, 0, decoded.Length);
                    ms.Position = 0;
                    // And finally deserializing
                    return Serializer.Deserialize<List<Instrument>>(ms);
                }
                catch (Exception ex)
                {
                    RaiseEvent(Error, this, new ErrorArgs(-1, "Error processing instrument data: " + ex.Message));

                    return new List<Instrument>();
                }
            }
        }

        /// <summary>
        ///     Cancel a live real time data stream.
        /// </summary>
        public void CancelRealTimeData(Instrument instrument)
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
                    _realTimeRequestSocket.SendMoreFrame(string.Empty);
                    _realTimeRequestSocket.SendMoreFrame(MessageType.CancelRTD);

                    using (var ms = new MemoryStream())
                    {
                        _realTimeRequestSocket.SendFrame(MyUtils.ProtoBufSerialize(instrument, ms));
                    }
                }
            }

            lock (_realTimeDataSocketLock)
            {
                _realTimeDataSocket?.Unsubscribe(Encoding.UTF8.GetBytes(instrument.Symbol));
            }

            lock (_realTimeDataStreamsLock)
            {
                RealTimeDataStreams.RemoveAll(x => x.Instrument.ID == instrument.ID);
            }
        }

        /// <summary>
        ///     Get a list of all available instruments
        /// </summary>
        /// <returns></returns>
        public List<Instrument> GetAllInstruments()
        {
            return FindInstruments();
        }
        #endregion

        /// <summary>
        ///     Initialization constructor.
        /// </summary>
        /// <param name="clientName">The name of this client. Should be unique. Used to route historical data.</param>
        /// <param name="host">The address of the server.</param>
        /// <param name="realTimeRequestPort">The port used for real time data requsts.</param>
        /// <param name="realTimePublishPort">The port used for publishing new real time data.</param>
        /// <param name="instrumentServerPort">The port used by the instruments server.</param>
        /// <param name="historicalDataPort">The port used for historical data.</param>
        public QDMSClient(string clientName, string host, int realTimeRequestPort, int realTimePublishPort, int instrumentServerPort, int historicalDataPort)
        {
            _name = clientName;

            _realTimeRequestConnectionString = $"tcp://{host}:{realTimeRequestPort}";
            _realTimeDataConnectionString = $"tcp://{host}:{realTimePublishPort}";
            _instrumentServerConnectionString = $"tcp://{host}:{instrumentServerPort}";
            _historicalDataConnectionString = $"tcp://{host}:{historicalDataPort}";

            _historicalDataRequests = new ConcurrentQueue<HistoricalDataRequest>();
        }

        /// <summary>
        ///     Add an instrument to QDMS.
        /// </summary>
        /// <param name="instrument"></param>
        /// <returns>The instrument with its ID set if successful, null otherwise.</returns>
        public Instrument AddInstrument(Instrument instrument)
        {
            if (!Connected)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not add instrument - not connected."));

                return null;
            }

            if (instrument == null)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not add instrument - instrument is null."));

                return null;
            }

            using (var s = new RequestSocket(_instrumentServerConnectionString))
            {
                using (var ms = new MemoryStream())
                {
                    s.SendMoreFrame(MessageType.AddInstrument); // First we send an "ADD" request
                    // Then we need to serialize and send the instrument
                    s.SendFrame(MyUtils.ProtoBufSerialize(instrument, ms));
                    // Then get the reply
                    var result = s.ReceiveFrameString();

                    if (!result.Equals(MessageType.Success, StringComparison.InvariantCultureIgnoreCase))
                    {
                        RaiseEvent(Error, this, new ErrorArgs(-1, "Instrument addition failed: received no reply."));

                        return null;
                    }
                    // Addition was successful, receive the instrument and return it
                    var serializedInstrument = s.ReceiveFrameBytes();

                    return MyUtils.ProtoBufDeserialize<Instrument>(serializedInstrument, ms);
                }
            }
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
                        _realTimeDataSocket.Subscribe(BitConverter.GetBytes(request.Instrument.ID.Value));
                    }
                    else if (reply.Equals(MessageType.RTDCanceled, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Successful cancelation of a real time data stream
                        // Also receive the symbol
                        var symbol = _realTimeRequestSocket.ReceiveFrameString();
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

                bool hasMore;

                _realTimeDataSocket.ReceiveFrameBytes(out hasMore);

                if (hasMore)
                {
                    var buffer = _realTimeDataSocket.ReceiveFrameBytes();

                    using (var ms = new MemoryStream())
                    {
                        var bar = MyUtils.ProtoBufDeserialize<RealTimeDataEventArgs>(buffer, ms);

                        RaiseEvent(RealTimeDataReceived, null, bar);
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
                else if (type.Equals(MessageType.AvailableDataReply, StringComparison.InvariantCultureIgnoreCase))
                {
                    HandleAvailabledataReply();
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
        ///     Called when we get a reply on a request for available data in local storage.
        /// </summary>
        private void HandleAvailabledataReply()
        {
            // First the instrument
            using (var ms = new MemoryStream())
            {
                var buffer = _historicalDataSocket.ReceiveFrameBytes();
                var instrument = MyUtils.ProtoBufDeserialize<Instrument>(buffer, ms);
                // Second the number of items
                buffer = _historicalDataSocket.ReceiveFrameBytes();
                var count = BitConverter.ToInt32(buffer, 0);
                // Then actually get the items, if any
                if (count == 0)
                {
                    RaiseEvent(LocallyAvailableDataInfoReceived, this, new LocallyAvailableDataInfoReceivedEventArgs(instrument, new List<StoredDataInfo>()));
                }
                else
                {
                    var storageInfo = new List<StoredDataInfo>();

                    for (var i = 0; i < count; i++)
                    {
                        bool hasMore;
                        buffer = _historicalDataSocket.ReceiveFrameBytes(out hasMore);
                        var info = MyUtils.ProtoBufDeserialize<StoredDataInfo>(buffer, ms);
                        storageInfo.Add(info);

                        if (!hasMore) break;
                    }

                    RaiseEvent(LocallyAvailableDataInfoReceived, this, new LocallyAvailableDataInfoReceivedEventArgs(instrument, storageInfo));
                }
            }
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
        ///     Called ona reply to a historical data request
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
                var decompressed = LZ4Codec.Decode(dataBuffer, 0, dataBuffer.Length, outputSize);
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
    }
}