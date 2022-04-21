using NetMQ;
using NetMQ.Sockets;
using QDMS;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;

namespace QDMSClient
{
	public partial class QDMSClient
	{
		private readonly object _realTimeRequestSocketLock = new object();
		private readonly object _realTimeDataSocketLock = new object();
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
        /// Fires when real time bars are received
        /// </summary>
        public event EventHandler<RealTimeDataEventArgs> RealTimeDataReceived;

        /// <summary>
        /// Fires when real time ticks are received
        /// </summary>
        public event EventHandler<TickEventArgs> RealTimeTickReceived;

        /// <summary>
        /// Keeps track of live real time data streams.
        /// </summary>
        public ObservableCollection<RealTimeDataRequest> RealTimeDataStreams { get; } = new ObservableCollection<RealTimeDataRequest>();

        /// <summary>
        /// Request a new real time data stream. Data will be delivered through the RealTimeDataReceived event.
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
        /// Cancel a live real time data stream.
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

        /// <summary>
        /// Process replies on the request socket. Heartbeats, errors, and subscribing to real time data streams.
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
                        HandleHeartbeatReply();
                    }
                    else if (reply.Equals(MessageType.Error, StringComparison.InvariantCultureIgnoreCase))
                    {
                        HandleErrorReply(ms);
                    }
                    else if (reply.Equals(MessageType.Success, StringComparison.InvariantCultureIgnoreCase))
                    {
                        HandleRtdStreamStartSuccessReply(ms);
                    }
                    else if (reply.Equals(MessageType.RTDCanceled, StringComparison.InvariantCultureIgnoreCase))
                    {
                        HandleRtdCancelationReply();
                    }
                }
            }
        }

        private void HandleRtdCancelationReply()
        {
            // Successful cancelation of a real time data stream Also receive the symbol and then frequency
            var symbol = _realTimeRequestSocket.ReceiveFrameString();
            var freq = _realTimeRequestSocket.ReceiveFrameBytes();
            // Nothing to do?
        }

        private void HandleRtdStreamStartSuccessReply(MemoryStream ms)
        {
            // Successful request to start a new real time data stream Receive the request
            var buffer = _realTimeRequestSocket.ReceiveFrameBytes();
            var request = MyUtils.ProtoBufDeserialize<RealTimeDataRequest>(buffer, ms);
            // Add it to the active streams
            lock (_realTimeDataStreamsLock)
            {
                RealTimeDataStreams.Add(request);
            }
            // TODO: Solve issue with null request Request worked, so we subscribe to the stream
            _realTimeDataSocket.Subscribe(Encoding.UTF8.GetBytes($"{request.Instrument.ID.Value}~{(int)request.Frequency}"));
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
    }
}