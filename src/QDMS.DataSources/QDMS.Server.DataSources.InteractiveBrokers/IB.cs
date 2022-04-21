// -----------------------------------------------------------------------
// <copyright file="IB.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// The IB system is a bit weird, with limitations both on the amount of data
// that can be requested in a single request, and on the frequency of requests.
// The system keeps track of requests, and if they fail due to pacing constraints
// resends them using the _requestRepeatTimer Elapsed event.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using QDMSIBClient;
using NLog;
using QDMS;
using LogLevel = NLog.LogLevel;

namespace QDMSApp.DataSources
{
    public partial class IB : IHistoricalDataSource, IRealTimeDataSource
    {
        private readonly IIBClient _client;
        private readonly Dictionary<int, RealTimeDataRequest> _realTimeDataRequests = new Dictionary<int, RealTimeDataRequest>();
        private readonly ConcurrentDictionary<int, HistoricalDataRequest> _historicalDataRequests = new ConcurrentDictionary<int, HistoricalDataRequest>();

        /// <summary>
        /// Sub-requests are created when we need to send multiple requests to the 
        /// IB client to fulfill a single data request. This one holds the ID mappings between them.
        /// Key: sub-request ID, Value: the ID of the original request that generated it.
        /// </summary>
        private readonly Dictionary<int, int> _subRequestIDMap = new Dictionary<int, int>();

        /// <summary>
        /// This holds the number of outstanding sub requests.
        /// Key: original request ID, Value: the number of subrequests sent out but not returned.
        /// </summary>
        private readonly Dictionary<int, int> _subRequestCount = new Dictionary<int, int>();

        /// <summary>
        /// Connects two IDs: the AssignedID of the RealTimeDataRequest from the broker, and the ID of the
        /// request at the TWS client.
        /// Key: tws client ID, value: AssignedID
        /// </summary>
        private readonly Dictionary<int, int> _requestIDMap = new Dictionary<int, int>();

        /// <summary>
        /// Reverse of the request ID map.
        /// Key: AssignedID, Value: TWS client ID
        /// </summary>
        private readonly Dictionary<int, int> _reverseRequestIDMap = new Dictionary<int, int>();

        /// <summary>
        /// Used to repeat failed requests after some time has passed.
        /// </summary>
        private Timer _requestRepeatTimer;

        /// <summary>
        /// Periodically updates the Connected property.
        /// </summary>
        private Timer _connectionStatusUpdateTimer;
        
        private int _requestCounter = 1;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly string _host;
        private readonly int _port;
        private readonly int _clientID;
        private readonly bool _ibUseNewRealTimeDataSystem;
        private readonly int _resendRepeatDelay = 20000; //ms
        private readonly int _conectionStatusTimerInterval = 1000;
        private readonly object _queueLock = new object();
        private readonly object _subReqMapLock = new object();
        private readonly object _requestIDMapLock = new object();

        public string Name { get; } = "Interactive Brokers";

        private bool _connected;
        public bool Connected
        {
            get
            {
                return _connected;
            }

            private set
            {
                _connected = value;
                OnPropertyChanged();
            }
        }

        public IB(ISettings settings, IIBClient client = null, int? clientId = null)
        {
            _host = settings.ibClientHost;
            _port = settings.ibClientPort;
            _clientID = clientId.HasValue ? clientId.Value : settings.histClientIBID;
            _ibUseNewRealTimeDataSystem = settings.ibUseNewRealTimeDataSystem;

            _requestRepeatTimer = new Timer(_resendRepeatDelay); //we wait 20 seconds to repeat failed requests
            _requestRepeatTimer.Elapsed += ReSendRequests;

            _connectionStatusUpdateTimer = new Timer(_conectionStatusTimerInterval);
            _connectionStatusUpdateTimer.Elapsed += _connectionStatusUpdateTimer_Elapsed;
            _connectionStatusUpdateTimer.Start();

            _requestCounter = 1;

            _client = client ?? new Client();
            _client.Error += _client_Error;
            _client.ConnectionClosed += _client_ConnectionClosed;
            _client.RealTimeBar += _client_RealTimeBar;
            _client.HistoricalData += _client_HistoricalData;
            _client.HistoricalDataEnd += _client_HistoricalDataEnd;
            _client.HistoricalDataUpdate += _client_HistoricalDataUpdate;
        }



        /// <summary>
        /// Update the connection status.
        /// </summary>
        void _connectionStatusUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Connected = _client?.Connected ?? false;
        }

        /// <summary>
        /// Some requests are sub-requests for a bigger request. This returns the top-level req id.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private int GetTopLevelRequestId(int reqId)
        { 
            lock (_subReqMapLock)
            {
                //otherwise it's just the same id
                return _subRequestIDMap.ContainsKey(reqId)
                    ? _subRequestIDMap[reqId]
                    : reqId;
            }
        }

        /// <summary>
        /// Control the dictionaries of subRequests
        /// </summary>
        /// <param name="subId">SubRequestID</param>
        /// <returns>Returns true if the parent request is complete</returns>
        private bool ControlSubRequest(int subId)
        {
            if (!_subRequestIDMap.ContainsKey(subId))
            {
                throw new ArgumentException("Provided id does not represent a subrequest");
            }

            lock (_subReqMapLock)
            {
                int originalID = _subRequestIDMap[subId];
                _subRequestIDMap.Remove(subId);
                _subRequestCount[originalID]--;
                if (_subRequestCount[originalID] > 0)
                {
                    //What happens here: this is a subrequest.
                    //We check how many sub-requests in this group have been delivered.
                    //if this is the last one, we want to call HistoricalDataRequestComplete()
                    //otherwise there's more data to come, so we have to wait for it
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// This event is raised when the connection to TWS client closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _client_ConnectionClosed(object sender, EventArgs e)
        {
            RaiseEvent(Disconnected, this, new DataSourceDisconnectEventArgs(Name, ""));
        }

        /// <summary>
        /// This event is raised in the case of some error
        /// This includes pacing violations, in which case we re-enqueue the request.
        /// </summary>
        private void _client_Error(object sender, ErrorEventArgs e)
        {
            //if we asked for too much real time data at once, we need to re-queue the failed request
            if ((int)e.ErrorCode == 420) //a real time pacing violation
            {
                HandleRealTimePacingViolationError(e);
            }
            else if ((int)e.ErrorCode == 162) //a historical data pacing violation
            {
                HandleHistoricalDataPacingViolationError(e);
            }
            else if ((int)e.ErrorCode == 200) //No security definition has been found for the request.
            {
                HandleNoSecurityDefinitionError(e);
            }
            else if ((int)e.ErrorCode == 10225) //bust event stops real-time bars
            {
                HandleRealtimeBarStop(e);
            }

            //different messages depending on the type of request
            RaiseIBClientError(e);
        }

        /// <summary>
        /// When we get an error from IB we want a different message depending
        /// on whether it's about a historical or a rt request.
        /// </summary>
        /// <param name="e"></param>
        private void RaiseIBClientError(ErrorEventArgs e)
        {
            var errorArgs = TWSUtils.ConvertErrorArguments(e);
            HistoricalDataRequest histReq;
            RealTimeDataRequest rtReq;
            var isHistorical = _historicalDataRequests.TryGetValue(e.TickerId, out histReq);
            if (isHistorical)
            {
                RaiseHistoricalDataReqError(e, errorArgs, histReq);
            }
            else if (_realTimeDataRequests.TryGetValue(e.TickerId, out rtReq)) //it's a real time request
            {
                RaiseRealtimeReqError(errorArgs, rtReq);
            }
        }

        private void RaiseRealtimeReqError(ErrorArgs errorArgs, RealTimeDataRequest rtReq)
        {
            errorArgs.ErrorMessage += string.Format(" RT Req: {0} @ {1}",
                rtReq.Instrument.Symbol,
                rtReq.Frequency);

            errorArgs.RequestID = rtReq.RequestID;
            RaiseEvent(Error, this, errorArgs);
        }

        private void RaiseHistoricalDataReqError(ErrorEventArgs e, ErrorArgs errorArgs, HistoricalDataRequest histReq)
        {
            int origId = _subRequestIDMap.ContainsKey(histReq.RequestID)
                ? _subRequestIDMap[histReq.RequestID]
                : histReq.RequestID;

            errorArgs.ErrorMessage += string.Format(" Historical Req: {0} @ {1} From {2} To {3} - TickerId: {4}  ReqID: {5}",
                histReq.Instrument.Symbol,
                histReq.Frequency,
                histReq.StartingDate,
                histReq.EndingDate,
                e.TickerId,
                histReq.RequestID);

            errorArgs.RequestID = origId;
            RaiseEvent(Error, this, errorArgs);
        }

        private void HandleNoSecurityDefinitionError(ErrorEventArgs e)
        {
            //multiple errors share the same code, we need to filter the wrong ones out
            if (!e.ErrorMsg.Contains("No security definition has been found for the request") &&
                !e.ErrorMsg.Contains("Invalid destination exchange specified"))
            {
                _logger.Error("Unexpected error: " + e.ErrorMsg);
                return;
            }

            //this will happen for example when asking for data on expired futures
            //return an empty data list
            //also handle the case where the error is for a subrequest
            int origId;

            lock (_subReqMapLock)
            {
                //if the data is arriving for a sub-request, we must get the id of the original request first
                //otherwise it's just the same id
                origId = GetTopLevelRequestId(e.TickerId);
            }

            if (origId != e.TickerId)
            {
                //this is a subrequest - only complete the top-level request if this is the last subrequest
                if (ControlSubRequest(e.TickerId))
                {
                    HistoricalDataRequestComplete(origId);
                }
            }
            else
            {
                HistoricalDataRequestComplete(origId);
            }
            
        }

        public void Connect()
        {
            if (_client.Connected) return;

            try
            {
                _client.Connect(_host, _port, _clientID);
            }
            catch (Exception e)
            {
                RaiseEvent(Error, this, new ErrorArgs(0, e.Message));
            }
            _requestRepeatTimer.Start();
        }

        public void Disconnect()
        {
            _client.Disconnect();
            _requestRepeatTimer.Stop();
        }

        /// <summary>
        /// Add a message to the log.
        ///</summary>
        private void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }

        /// <summary>
        /// This event is raised when the _requestRepeatTimer period elapses.
        /// It repeats failed requests for historical or real time data
        /// </summary>
        private void ReSendRequests(object sender, ElapsedEventArgs e)
        {
            if (!_client.Connected) return;
            lock (_queueLock)
            {
                ResendRealTimeRequests();
                ResendHistoricalRequests();
            }
        }

        public void Dispose()
        {
            if(_client != null)
                _client.Dispose();

            if (_requestRepeatTimer != null)
            {
                _requestRepeatTimer.Dispose();
                _requestRepeatTimer = null;
            }

            if (_connectionStatusUpdateTimer != null)
            {
                _connectionStatusUpdateTimer.Dispose();
                _connectionStatusUpdateTimer = null;
            }
        }

        ///<summary>
        /// Raise the event in a threadsafe manner
        ///</summary>
        ///<param name="event"></param>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        ///<typeparam name="T"></typeparam>
        static private void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
        where T : EventArgs
        {
            EventHandler<T> handler = @event;
            if (handler == null) return;
            handler(sender, e);
        }

        public event EventHandler<RealTimeDataEventArgs> DataReceived;
        public event EventHandler<TickEventArgs> TickReceived;

        public event EventHandler<ErrorArgs> Error;

        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

        public event EventHandler<QDMS.HistoricalDataEventArgs> HistoricalDataArrived;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}