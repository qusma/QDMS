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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using QDMSIBClient;
using IBApi;
using NLog;
using QDMS;
using BarSize = QDMS.BarSize;
using LogLevel = NLog.LogLevel;

namespace QDMSApp.DataSources
{
    public class IB : IHistoricalDataSource, IRealTimeDataSource
    {
        private readonly IIBClient _client;
        private readonly Dictionary<int, RealTimeDataRequest> _realTimeDataRequests;
        private readonly ConcurrentDictionary<int, HistoricalDataRequest> _historicalDataRequests;

        /// <summary>
        /// Sub-requests are created when we need to send multiple requests to the 
        /// IB client to fulfill a single data request. This one holds the ID mappings between them.
        /// Key: sub-request ID, Value: the ID of the original request that generated it.
        /// </summary>
        private readonly Dictionary<int, int> _subRequestIDMap;

        /// <summary>
        /// This holds the number of outstanding sub requests.
        /// Key: original request ID, Value: the number of subrequests sent out but not returned.
        /// </summary>
        private readonly Dictionary<int, int> _subRequestCount;

        /// <summary>
        /// Connects two IDs: the AssignedID of the RealTimeDataRequest from the broker, and the ID of the
        /// request at the TWS client.
        /// Key: tws client ID, value: AssignedID
        /// </summary>
        private readonly Dictionary<int, int> _requestIDMap;

        /// <summary>
        /// Reverse of the request ID map.
        /// Key: AssignedID, Value: TWS client ID
        /// </summary>
        private readonly Dictionary<int, int> _reverseRequestIDMap;

        private readonly Queue<int> _realTimeRequestQueue;
        private readonly Queue<int> _historicalRequestQueue;

        private readonly ConcurrentDictionary<int, List<OHLCBar>> _arrivedHistoricalData;

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
        private readonly object _queueLock = new object();
        private readonly object _subReqMapLock = new object();
        private readonly object _requestIDMapLock = new object();

        public string Name { get; private set; }

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
            Name = "Interactive Brokers";

            _host = settings.ibClientHost;
            _port = settings.ibClientPort;
            _clientID = clientId.HasValue ? clientId.Value : settings.histClientIBID;
            _ibUseNewRealTimeDataSystem = settings.ibUseNewRealTimeDataSystem;

            _realTimeDataRequests = new Dictionary<int, RealTimeDataRequest>();
            _realTimeRequestQueue = new Queue<int>();

            _historicalDataRequests = new ConcurrentDictionary<int, HistoricalDataRequest>();
            _historicalRequestQueue = new Queue<int>();

            _arrivedHistoricalData = new ConcurrentDictionary<int, List<OHLCBar>>();

            _subRequestIDMap = new Dictionary<int, int>();
            _subRequestCount = new Dictionary<int, int>();
            _requestIDMap = new Dictionary<int, int>();
            _reverseRequestIDMap = new Dictionary<int, int>();

            _requestRepeatTimer = new Timer(20000); //we wait 20 seconds to repeat failed requests
            _requestRepeatTimer.Elapsed += ReSendRequests;

            _connectionStatusUpdateTimer = new Timer(1000);
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

        private void _client_HistoricalDataUpdate(object sender, QDMSIBClient.HistoricalDataEventArgs e)
        {
            if (e.Bar.Volume < 0) return;

            var originalRequest = _realTimeDataRequests[e.RequestId];
            var args = TWSUtils.HistoricalDataEventArgsToRealTimeDataEventArgs(e, originalRequest.Instrument.ID.Value, _requestIDMap[e.RequestId]);
            RaiseEvent(DataReceived, this, args);
        }

        /// <summary>
        /// Update the connection status.
        /// </summary>
        void _connectionStatusUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Connected = _client != null ? _client.Connected : false;
        }

        /// <summary>
        /// This event is raised when historical data arrives from TWS
        /// </summary>
        private void _client_HistoricalData(object sender, QDMSIBClient.HistoricalDataEventArgs e)
        {
            int id;

            lock (_subReqMapLock)
            {
                //if the data is arriving for a sub-request, we must get the id of the original request first
                //otherwise it's just the same id
                id = _subRequestIDMap.ContainsKey(e.RequestId) 
                    ? _subRequestIDMap[e.RequestId] 
                    : e.RequestId;
            }

            if(!_historicalDataRequests.ContainsKey(id))
            {
                //this means we don't have a request with this key. If all works well,
                //it should mean that this is actually a realtime data request using the new system
                return;
            }

            var bar = TWSUtils.HistoricalDataEventArgsToOHLCBar(e);

            //stocks need to have their volumes multiplied by 100, I think all other instrument types do not
            if (_historicalDataRequests[id].Instrument.Type == InstrumentType.Stock)
                bar.Volume *= 100;

            _arrivedHistoricalData[id].Add(bar);
        }

        private void _client_HistoricalDataEnd(object sender, HistoricalDataEndEventArgs e)
        {
            bool requestComplete = true;

            int id = _subRequestIDMap.ContainsKey(e.RequestId)
                    ? _subRequestIDMap[e.RequestId]
                    : e.RequestId;

            if (!_historicalDataRequests.ContainsKey(id))
            {
                //this means we don't have a request with this key. If all works well,
                //it should mean that this is actually a realtime data request using the new system
                return;
            }

            lock (_subReqMapLock)
            {
                if (_subRequestIDMap.ContainsKey(e.RequestId))
                {
                    //If there are sub-requests, here we check if this is the last one
                    requestComplete = ControlSubRequest(e.RequestId);

                    if (requestComplete)
                    {
                        //If it was the last one, we need to order the data because sub-requests can arrive out of order
                        _arrivedHistoricalData[id] = _arrivedHistoricalData[id].OrderBy(x => x.DTOpen).ToList();
                    }
                }
            }

            if (requestComplete)
            {
                HistoricalDataRequestComplete(id);
            }
        }

        /// <summary>
        /// Control the dictionaries of subRequests
        /// </summary>
        /// <param name="subId">SubRequestID</param>
        /// <returns>Returns true if the parent request is complete</returns>
        private bool ControlSubRequest(int subId)
        {
            bool requestComplete = false;

            lock (_subReqMapLock)
            {
                if (_subRequestIDMap.ContainsKey(subId))
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
                        requestComplete = false;
                    }
                    else
                    {
                        requestComplete = true;
                    }
                }
            }

            return requestComplete;
        }

        /// <summary>
        /// This method is called when a historical data request has delivered all its bars
        /// </summary>
        /// <param name="requestID"></param>
        private void HistoricalDataRequestComplete(int requestID)
        {
            var request = _historicalDataRequests[requestID];

            //IB doesn't actually allow us to provide a deterministic starting point for our historical data query
            //so sometimes we get data from earlier than we want
            //here we throw it away
            var cutoffDate = request.StartingDate.Date;

            List<OHLCBar> bars;
            var removed = _arrivedHistoricalData.TryRemove(requestID, out bars);
            if (!removed) return;

            //due to the nature of sub-requests, the list may contain the same points multiple times
            //so we grab unique values only
            bars = bars.Distinct((x, y) => x.DTOpen == y.DTOpen).ToList();

            //we have to make adjustments to the times as well as derive the bar closing times
            AdjustBarTimes(bars, request);

            //if the data is daily or lower freq, and a stock, set adjusted ohlc values for convenience
            if (request.Frequency >= BarSize.OneDay && 
                request.Instrument.Type == InstrumentType.Stock)
            {
                foreach (OHLCBar b in bars)
                {
                    b.AdjOpen = b.Open;
                    b.AdjHigh = b.High;
                    b.AdjLow = b.Low;
                    b.AdjClose = b.Close;
                }
            }

            //return the data through the HistoricalDataArrived event
            RaiseEvent(HistoricalDataArrived, this, new QDMS.HistoricalDataEventArgs(
                request,
                bars.Where(x => x.DT.Date >= cutoffDate).ToList()));
        }

        /// <summary>
        /// Fixes bar closing times
        /// </summary>
        private void AdjustBarTimes(List<OHLCBar> bars, HistoricalDataRequest request)
        {
            //One day or lower frequency means we don't get time data.
            //Instead we provide our own by using that day's session end...
            if (request.Frequency < BarSize.OneDay)
            {
                GenerateIntradayBarClosingTimes(bars, request.Frequency);
            }
            else
            {
                AdjustDailyBarTimes(bars);
            }
        }

        /// <summary>
        /// Sets closing times
        /// </summary>
        private void AdjustDailyBarTimes(IEnumerable<OHLCBar> bars)
        {
            // For daily data, IB does not provide us with bar opening/closing times.
            // But the IB client does shift the timezone from UTC to local.
            // So to get the correct day we have to shift it back to UTC first.
            foreach (OHLCBar bar in bars)
            {
                bar.DT = bar.DTOpen.Value;
            }
        }


        /// <summary>
        /// Sets the appropriate closing time for each bar, since IB only gives us the opening time.
        /// </summary>
        private void GenerateIntradayBarClosingTimes(List<OHLCBar> bars, BarSize frequency)
        {
            TimeSpan freqTS = frequency.ToTimeSpan();
            for (int i = 0; i < bars.Count; i++)
            {
                var bar = bars[i];

                if (i == bars.Count - 1)
                {
                    //if it's the last bar we are basically just guessing the 
                    //closing time by adding the duration of the frequency
                    bar.DT = bar.DTOpen.Value + freqTS;
                }
                else
                {
                    //if it's not the last bar, we set the closing time to the
                    //earliest of the open of the next bar and the period of the frequency
                    //e.g. if hourly bar opens at 9:30 and the next bar opens at 10:00
                    //we set the close at the earliest of 10:00 and 10:30
                    DateTime openPlusBarSize = bar.DTOpen.Value + freqTS;
                    bar.DT = bars[i + 1].DTOpen.Value < openPlusBarSize ? bars[i + 1].DTOpen.Value : openPlusBarSize;
                }

            }
        }

        //This event is raised when real time data arrives
        //We convert them and pass them on downstream
        private void _client_RealTimeBar(object sender, RealTimeBarEventArgs e)
        {
            
            var originalRequest = _realTimeDataRequests[e.RequestId];
            RealTimeDataEventArgs args = TWSUtils.RealTimeDataEventArgsConverter(e, originalRequest.Frequency);
            args.InstrumentID = originalRequest.Instrument.ID.Value;
            args.RequestID = _requestIDMap[e.RequestId];
            RaiseEvent(DataReceived, this, args);
        }

        //This event is raised when the connection to TWS client closed.
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
                RealTimeDataRequest req;
                if (_realTimeDataRequests.TryGetValue(e.TickerId, out req))
                {
                    _logger.Error(string.Format(" RT Req: {0} @ {1}. Restarting stream.",
                        req.Instrument.Symbol,
                        req.Frequency));

                    _client.CancelRealTimeBars(e.TickerId);

                    RestartRealtimeStream(req);
                }
                else
                {
                    _logger.Error(e.ErrorMsg + " - Unable to find corresponding request");
                }
                
                
            }

            //different messages depending on the type of request
            var errorArgs = TWSUtils.ConvertErrorArguments(e);
            HistoricalDataRequest histReq;
            RealTimeDataRequest rtReq;
            var isHistorical = _historicalDataRequests.TryGetValue(e.TickerId, out histReq);
            if (isHistorical)
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
            }
            else if (_realTimeDataRequests.TryGetValue(e.TickerId, out rtReq)) //it's a real time request
            {
                errorArgs.ErrorMessage += string.Format(" RT Req: {0} @ {1}",
                    rtReq.Instrument.Symbol,
                    rtReq.Frequency);

                errorArgs.RequestID = rtReq.RequestID;
            }

            RaiseEvent(Error, this, errorArgs);
        }

        private void RestartRealtimeStream(RealTimeDataRequest req)
        {
            RequestRealTimeData(req);
        }

        private void HandleNoSecurityDefinitionError(ErrorEventArgs e)
        {
            //multiple errors share the same code...
            if (e.ErrorMsg.Contains("No security definition has been found for the request") ||
                e.ErrorMsg.Contains("Invalid destination exchange specified"))
            {
                //this will happen for example when asking for data on expired futures
                //return an empty data list
                //also handle the case where the error is for a subrequest
                int origId;

                lock (_subReqMapLock)
                {
                    //if the data is arriving for a sub-request, we must get the id of the original request first
                    //otherwise it's just the same id
                    origId = _subRequestIDMap.ContainsKey(e.TickerId)
                        ? _subRequestIDMap[e.TickerId]
                        : e.TickerId;
                }

                if (origId != e.TickerId)
                {
                    //this is a subrequest - only complete the
                    if (ControlSubRequest(e.TickerId))
                    {
                        HistoricalDataRequestComplete(origId);
                    }
                }
                else
                {
                    HistoricalDataRequestComplete(origId); //TODO: crash here when the request causing the secdef error is from RT
                }
            }
            else
            {
                _logger.Error("Unexpected error: " + e.ErrorMsg);
            }
        }

        private void HandleRealTimePacingViolationError(ErrorEventArgs e)
        {
            lock (_queueLock)
            {
                if (!_realTimeRequestQueue.Contains(e.TickerId))
                {
                    //since the request did not succeed, what we do is re-queue it and it gets requested again by the timer
                    _realTimeRequestQueue.Enqueue(e.TickerId);
                }
            }
        }

        private void HandleHistoricalDataPacingViolationError(ErrorEventArgs e)
        {
            if (e.ErrorMsg.StartsWith("Historical Market Data Service error message:HMDS query returned no data") ||
                e.ErrorMsg.StartsWith("Historical Market Data Service error message:No market data permissions"))
            {
                //no data returned = we return an empty data set
                int origId;

                lock (_subReqMapLock)
                {
                    //if the data is arriving for a sub-request, we must get the id of the original request first
                    //otherwise it's just the same id
                    origId = _subRequestIDMap.ContainsKey(e.TickerId)
                        ? _subRequestIDMap[e.TickerId]
                        : e.TickerId;
                }

                if (origId != e.TickerId)
                {
                    //this is a subrequest - only complete the
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

            else
            {
                //simply a data pacing violation
                lock (_queueLock)
                {
                    if (!_historicalRequestQueue.Contains(e.TickerId))
                    {
                        //same as above
                        _historicalRequestQueue.Enqueue(e.TickerId);
                    }
                }
            }
        }

        /// <summary>
        /// historical data request
        /// </summary>
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            //Historical data limitations: https://www.interactivebrokers.com/en/software/api/apiguide/api/historical_data_limitations.htm
            //the issue here is that the request may not be fulfilled...so we need to keep track of the request
            //and if we get an error regarding its failure, send it again using a timer
            int originalReqID = _requestCounter++;
            _historicalDataRequests.TryAdd(originalReqID, request);
            
            _arrivedHistoricalData.TryAdd(originalReqID, new List<OHLCBar>());
            
            //if necessary, chop up the request into multiple chunks so as to abide
            //the historical data limitations
            if (TWSUtils.RequestObeysLimits(request))
            {
                //send the request, no need for subrequests
                SendHistoricalRequest(originalReqID, request);
            }
            else
            {
                //create subrequests, add them to the ID map, and send them to TWS
                var subRequests = SplitRequest(request);
                _subRequestCount.Add(originalReqID, subRequests.Count);

                foreach (HistoricalDataRequest subReq in subRequests)
                {
                    lock (_subReqMapLock)
                    {
                        _requestCounter++;
                        _historicalDataRequests.TryAdd(_requestCounter, subReq);
                        _subRequestIDMap.Add(_requestCounter, originalReqID);
                        SendHistoricalRequest(_requestCounter, subReq);
                    }
                }
            }
        }

        /// <summary>
        /// Splits a historical data request into multiple pieces so that they obey the request limits
        /// </summary>
        private List<HistoricalDataRequest> SplitRequest(HistoricalDataRequest request)
        {
            var requests = new List<HistoricalDataRequest>();

            //start at the end, and work backward in increments slightly lower than the max allowed time
            int step = (int)(TWSUtils.MaxRequestLength(request.Frequency) * .95);
            DateTime currentDate = request.EndingDate;
            while (currentDate > request.StartingDate)
            {
                var newReq = (HistoricalDataRequest)request.Clone();
                newReq.EndingDate = currentDate;
                newReq.StartingDate = newReq.EndingDate.AddSeconds(-step);
                if (newReq.StartingDate < request.StartingDate)
                    newReq.StartingDate = request.StartingDate;

                currentDate = currentDate.AddSeconds(-step);
                requests.Add(newReq);
            }

            return requests;
        }

        private void SendHistoricalRequest(int id, HistoricalDataRequest request)
        {
            Log(LogLevel.Info, string.Format("Sent historical data request to TWS. ID: {0}, Symbol: {1}, {2} back from {3}",
                id,
                request.Instrument.Symbol,
                TWSUtils.TimespanToDurationString((request.EndingDate - request.StartingDate), request.Frequency),
                request.EndingDate.ToString("yyyy-MM-dd hh:mm:ss")));

            TimeZoneInfo exchangeTZ = request.Instrument.GetTZInfo();
            //we need to convert time from the exchange TZ to Local...the ib client then converts it to UTC
            var endingDate = TimeZoneInfo.ConvertTime(request.EndingDate, exchangeTZ, TimeZoneInfo.Utc);

            try
            {
                _client.RequestHistoricalData
                (
                    id,
                    TWSUtils.InstrumentToContract(request.Instrument),
                    endingDate.ToString("yyyyMMdd HH:mm:ss") + " GMT",
                    TWSUtils.TimespanToDurationString(request.EndingDate - request.StartingDate, request.Frequency),
                    TWSUtils.BarSizeConverter(request.Frequency),
                    GetDataType(request.Instrument),
                    request.RTHOnly,
                    false
                );
            }
            catch(Exception ex)
            {
                Log(LogLevel.Error, "IB: Could not send historical data request: " + ex.Message);
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not send historical data request: " + ex.Message, id));
            }
        }

        private HistoricalDataType GetDataType(Instrument instrument)
        {
            //when it comes to FOREX, IB doesn't return any data if you request Trades, you have to ask for Bid/Ask/Mid
            if (instrument.Type == InstrumentType.Cash ||
                instrument.Type == InstrumentType.Commodity)
            {
                return HistoricalDataType.Midpoint;
            }

            return HistoricalDataType.Trades;
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
        /// real time data request
        /// </summary>
        public void RequestRealTimeData(RealTimeDataRequest request)
        {
            var id = _requestCounter++;
            lock (_requestIDMapLock)
            {
                _realTimeDataRequests.Add(id, request);
                _requestIDMap.Add(id, request.AssignedID);
                if (_reverseRequestIDMap.ContainsKey(request.AssignedID))
                {
                    _reverseRequestIDMap[request.AssignedID] = id;
                }
                else
                {
                    _reverseRequestIDMap.Add(request.AssignedID, id);
                }
            }

            try
            {
                Contract contract = TWSUtils.InstrumentToContract(request.Instrument);

                if (_ibUseNewRealTimeDataSystem)
                {
                    //the new system uses the historical data update endpoint instead of realtime data
                    _client.RequestHistoricalData(id, contract, "", 
                        "60 S", QDMSIBClient.BarSize.FiveSeconds, HistoricalDataType.Trades, request.RTHOnly, true);

                    //todo: write test
                }
                else
                {
                    _client.RequestRealTimeBars(
                        id,
                        contract,
                        "TRADES",
                        request.RTHOnly);
                }
            }
            catch(Exception ex)
            {
                Log(LogLevel.Error, "IB: Could not send real time data request: " + ex.Message);
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not send real time data request: " + ex.Message));
            }
        }

        public void CancelRealTimeData(int requestID)
        {
            if (_reverseRequestIDMap.TryGetValue(requestID, out int twsId))
            {
                if (_ibUseNewRealTimeDataSystem)
                {
                    _client.CancelHistoricalData(twsId);
                }
                else
                {
                    _client.CancelRealTimeBars(twsId);
                }
            }
            else
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Real time stream requested for cancelation not found. ID: " + requestID));
            }
            
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
                while (_realTimeRequestQueue.Count > 0)
                {
                    var requestID = _realTimeRequestQueue.Dequeue();
                    var symbol = string.IsNullOrEmpty(_realTimeDataRequests[requestID].Instrument.DatasourceSymbol)
                        ? _realTimeDataRequests[requestID].Instrument.Symbol
                        : _realTimeDataRequests[requestID].Instrument.DatasourceSymbol;

                    RequestRealTimeData(_realTimeDataRequests[requestID]);
                    Log(LogLevel.Info, string.Format("IB Repeating real time data request for {0} @ {1}",
                            symbol,
                            _realTimeDataRequests[requestID].Frequency));
                }

                while (_historicalRequestQueue.Count > 0)
                {
                    var requestID = _historicalRequestQueue.Dequeue();

                    var symbol = string.IsNullOrEmpty(_historicalDataRequests[requestID].Instrument.DatasourceSymbol)
                        ? _historicalDataRequests[requestID].Instrument.Symbol
                        : _historicalDataRequests[requestID].Instrument.DatasourceSymbol;

                    // We repeat the request _with the same id_ as used previously. This means the previous
                    // sub request mapping will still work
                    SendHistoricalRequest(requestID, _historicalDataRequests[requestID]);

                    Log(LogLevel.Info, string.Format("IB Repeating historical data request for {0} @ {1} with ID {2}",
                            symbol,
                            _historicalDataRequests[requestID].Frequency,
                            requestID));
                }
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