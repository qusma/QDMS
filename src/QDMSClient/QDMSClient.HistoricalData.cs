using K4os.Compression.LZ4;
using NetMQ;
using NetMQ.Sockets;
using QDMS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;

namespace QDMSClient
{
    public partial class QDMSClient
    {
        /// <summary>
        /// Queue of historical data requests waiting to be sent out.
        /// </summary>
        private readonly ConcurrentQueue<HistoricalDataRequest> _historicalDataRequests;

        /// <summary>
        /// Periodically sends out requests for historical data and receives data when requests are fulfilled.
        /// </summary>
        private NetMQTimer _historicalDataTimer;

        private readonly object _historicalDataSocketLock = new object();
        private readonly object _pendingHistoricalRequestsLock = new object();

        /// <summary>
        /// Keeps track of historical requests that have been sent but the data has not been received yet.
        /// </summary>
        public ObservableCollection<HistoricalDataRequest> PendingHistoricalRequests { get; } = new ObservableCollection<HistoricalDataRequest>();

        /// <summary>
        /// Fires when historical bars are received
        /// </summary>
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataReceived;

        /// <summary>
        /// This socket sends requests for and receives historical data.
        /// </summary>
        private DealerSocket _historicalDataSocket;

        /// <summary>
        /// Pushes data to local storage.
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
        /// Request historical data. Data will be delivered through the HistoricalDataReceived event.
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
        /// Handling replies to a data push, a historical data request, or an available data request
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

        /// <summary>
        /// Sends out requests for historical data and raises an event when it's received.
        /// </summary>
        private void HistoricalDataTimerElapsed(object sender, NetMQTimerEventArgs e)
        {
            if (!Connected)
            {
                return;
            }

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

        /// <summary>
        /// Called on a reply to a data push
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
        /// Called on a reply to a historical data request
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
    }
}