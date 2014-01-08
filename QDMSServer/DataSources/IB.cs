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
using System.Linq;
using System.Timers;
using System.Windows;
using Krs.Ats.IBNet;
using NLog;
using QDMS;
using LogLevel = NLog.LogLevel;

namespace QDMSServer.DataSources
{
    internal class IB : IHistoricalDataSource, IRealTimeDataSource
    {
        private readonly IBClient _client;
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

        private readonly Queue<int> _realTimeRequestQueue;
        private readonly Queue<int> _historicalRequestQueue;

        private readonly ConcurrentDictionary<int, List<OHLCBar>> _arrivedHistoricalData;

        private readonly Timer _requestRepeatTimer;
        private int _requestCounter;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly int _clientID;
        private readonly object _queueLock = new object();
        private readonly object _subReqMapLock = new object();

        public string Name { get; private set; }

        public bool Connected { get { return _client.Connected; } }

        public IB(int clientID = -1)
        {
            Name = "Interactive Brokers";

            if (clientID < 0)
                clientID = Properties.Settings.Default.ibClientID;
            _clientID = clientID;

            _realTimeDataRequests = new Dictionary<int, RealTimeDataRequest>();
            _realTimeRequestQueue = new Queue<int>();

            _historicalDataRequests = new ConcurrentDictionary<int, HistoricalDataRequest>();
            _historicalRequestQueue = new Queue<int>();

            _arrivedHistoricalData = new ConcurrentDictionary<int, List<OHLCBar>>();

            _subRequestIDMap = new Dictionary<int, int>();
            _subRequestCount = new Dictionary<int, int>();

            _requestRepeatTimer = new Timer(20000); //we wait 20 seconds to repeat failed requests
            _requestRepeatTimer.Elapsed += ReSendRequests;

            _requestCounter = 1;

            _client = new IBClient();
            _client.Error += _client_Error;
            _client.ConnectionClosed += _client_ConnectionClosed;
            _client.RealTimeBar += _client_RealTimeBar;
            _client.HistoricalData += _client_HistoricalData;
        }

        /// <summary>
        /// This event is raised when historical data arrives from TWS
        /// </summary>
        private void _client_HistoricalData(object sender, Krs.Ats.IBNet.HistoricalDataEventArgs e)
        {
            //convert the bar and add it to the Dictionary of arrived data
            var bar = TWSUtils.HistoricalDataEventArgsToOHLCBar(e);
            int id;

            lock (_subReqMapLock)
            {
                //if the data is arriving for a sub-request, we must get the id of the original request first
                //otherwise it's just the same id
                id = _subRequestIDMap.ContainsKey(e.RequestId) 
                    ? _subRequestIDMap[e.RequestId] 
                    : e.RequestId;
            }

            var request = _historicalDataRequests[id];

            //One day or lower frequency means we don't get time data.
            //Instead we provide our own by using that day's session end...
            //perhaps this should be moved to the HistoricalDataBroker instead?
            if (request.Frequency >= QDMS.BarSize.OneDay)
            {
                var utcDT = TimeZoneInfo.ConvertTimeToUtc(bar.DT, TimeZoneInfo.Local);
                var dotw = utcDT.DayOfWeek.ToInt();
                var daysSessionEnd = request.Instrument.Sessions.FirstOrDefault(x => (int)x.ClosingDay == dotw && x.IsSessionEnd);
                if (daysSessionEnd != null)
                {
                    bar.DT = new DateTime(bar.DT.Year, bar.DT.Month, bar.DT.Day) + daysSessionEnd.ClosingTime;
                }
                else
                {
                    bar.DT = new DateTime(bar.DT.Year, bar.DT.Month, bar.DT.Day, 23, 59, 59);
                }
            }
            else
            {
                var exchangeTZ = TimeZoneInfo.FindSystemTimeZoneById(request.Instrument.Exchange.Timezone);
                bar.DT = TimeZoneInfo.ConvertTime(bar.DT, TimeZoneInfo.Local, exchangeTZ);
            }

            //stocks need to have their volumes multiplied by 100, I think all other instrument types do not
            if (request.Instrument.Type == InstrumentType.Stock)
                bar.Volume *= 100;



            _arrivedHistoricalData[id].Add(bar);

            if (e.RecordNumber >= e.RecordTotal - 1) //this was the last item to receive for this request, send it to the broker
            {
                bool requestComplete = true;

                lock (_subReqMapLock)
                {
                    if (_subRequestIDMap.ContainsKey(e.RequestId))
                    {
                        _subRequestIDMap.Remove(e.RequestId);
                        _subRequestCount[id]--;
                        if (_subRequestCount[id] > 0)
                        {
                            //What happens here: this is a subrequest.
                            //We check how many sub-requests in this group have been delivered.
                            //if this is the last one, we want to call HistoricalDataRequestComplete()
                            //otherwise there's more data to come, so we have to wait for it
                            requestComplete = false;
                        }
                        else
                        {
                            //if it is complete, we'll need to sort the data because subrequests may not come in in order
                            _arrivedHistoricalData[id] = _arrivedHistoricalData[id].OrderBy(x => x.DT).ToList();
                        }
                    }
                }

                if(requestComplete)
                    HistoricalDataRequestComplete(id);
            }
        }

        /// <summary>
        /// This method is called when a historical data request has delivered all its bars
        /// </summary>
        /// <param name="requestID"></param>
        private void HistoricalDataRequestComplete(int requestID)
        {
            //IB doesn't actually allow us to provide a deterministic starting point for our historical data query
            //so sometimes we get data from earlier than we want
            //here we throw it away
            var cutoffDate = _historicalDataRequests[requestID].StartingDate.Date;

            List<OHLCBar> bars;
            var removed = _arrivedHistoricalData.TryRemove(requestID, out bars);
            if (!removed) return;

            //due to the nature of sub-requests, the list may contain the same points multiple times
            //so we grab unique values only
            bars = bars.Distinct((x, y) => x.DT == y.DT).ToList();

            //if the data is daily or lower freq, set adjusted ohlc values for convenience
            if (_historicalDataRequests[requestID].Frequency >= QDMS.BarSize.OneDay)
            {
                foreach (OHLCBar b in bars)
                {
                    b.AdjOpen = b.Open;
                    b.AdjHigh = b.High;
                    b.AdjLow = b.Low;
                    b.AdjClose = b.Close;
                }
            }

            RaiseEvent(HistoricalDataArrived, this, new QDMS.HistoricalDataEventArgs(
                _historicalDataRequests[requestID],
                bars.Where(x => x.DT.Date >= cutoffDate).ToList()));
        }

        //This event is raised when real time data arrives
        //We convert them and pass them on downstream
        private void _client_RealTimeBar(object sender, RealTimeBarEventArgs e)
        {
            RealTimeDataEventArgs args = TWSUtils.RealTimeDataEventArgsConverter(e);
            args.Symbol = _realTimeDataRequests[e.RequestId].Instrument.Symbol;
            RaiseEvent(DataReceived, this, args);
        }

        //This event is raised when the connection to TWS client closed.
        private void _client_ConnectionClosed(object sender, ConnectionClosedEventArgs e)
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
                lock (_queueLock)
                {
                    if (!_realTimeRequestQueue.Contains(e.TickerId))
                    {
                        //since the request did not succeed, what we do is re-queue it and it gets requested again by the timer
                        _realTimeRequestQueue.Enqueue(e.TickerId);
                    }
                }
            }
            else if ((int)e.ErrorCode == 162) //a historical data pacing violation
            {
                //turns out that more than one error uses the same error code! What were they thinking?
                if (e.ErrorMsg.StartsWith("Historical Market Data Service error message:HMDS query returned no data"))
                {
                    //no data returned = we return an empty data set
                    RaiseEvent(HistoricalDataArrived, this, new QDMS.HistoricalDataEventArgs(
                        _historicalDataRequests[e.TickerId],
                        new List<OHLCBar>()));
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
            else if ((int)e.ErrorCode == 200) //No security definition has been found for the request.
            {
                //this will happen for example when asking for data on expired futures
                //return an empty data list
                HistoricalDataRequestComplete(e.TickerId);
            }

            //different messages depending on the type of request
            var errorArgs = TWSUtils.ConvertErrorArguments(e);
            HistoricalDataRequest histReq;
            RealTimeDataRequest rtReq;
            var isHistorical = _historicalDataRequests.TryGetValue(e.TickerId, out histReq);
            if (isHistorical)
            {
                errorArgs.ErrorMessage += string.Format(" Historical Req: {0} @ {1} From {2} To {3} - ID {4}",
                    histReq.Instrument.Symbol,
                    histReq.Frequency,
                    histReq.StartingDate,
                    histReq.EndingDate,
                    histReq.RequestID);
            }
            else if (_realTimeDataRequests.TryGetValue(e.TickerId, out rtReq)) //it's a real time request
            {
                errorArgs.ErrorMessage += string.Format(" RT Req: {0} @ {1}",
                    rtReq.Instrument.Symbol,
                    rtReq.Frequency);
            }

            RaiseEvent(Error, this, errorArgs);
        }

        /// <summary>
        /// historical data request
        /// </summary>
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            //Historical data limitations: https://www.interactivebrokers.com/en/software/api/apiguide/api/historical_data_limitations.htm
            //the issue here is that the request may not be fulfilled...so we need to keep track of the request
            //and if we get an error regarding its failure, send it again using a timer
            int originalReqID = ++_requestCounter;
            _historicalDataRequests.TryAdd(originalReqID, request);
            _arrivedHistoricalData.TryAdd(originalReqID, new List<OHLCBar>());

            //limit the ending date to the present
            DateTime endDate = request.EndingDate > DateTime.Now ? DateTime.Now : request.EndingDate;
            request.EndingDate = endDate; //perhaps this should be moved to the historical data broker, instead

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
                lock (_subReqMapLock)
                {
                    foreach (HistoricalDataRequest subReq in subRequests)
                    {
                        _requestCounter++;
                        SendHistoricalRequest(_requestCounter, subReq);
                        _subRequestIDMap.Add(_requestCounter, originalReqID);
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

            _client.RequestHistoricalData
            (
                id,
                TWSUtils.InstrumentToContract(request.Instrument),
                request.EndingDate,
                TWSUtils.TimespanToDurationString((request.EndingDate - request.StartingDate), request.Frequency),
                TWSUtils.BarSizeConverter(request.Frequency),
                HistoricalDataType.Trades,
                request.RTHOnly ? 1 : 0
            );
        }

        public void Connect()
        {
            try
            {
                _client.Connect(Properties.Settings.Default.ibClientHost, Properties.Settings.Default.ibClientPort, _clientID);
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
        public int RequestRealTimeData(RealTimeDataRequest request)
        {
            _realTimeDataRequests.Add(++_requestCounter, request);
            Contract contract = TWSUtils.InstrumentToContract(request.Instrument);

            _client.RequestRealTimeBars(
                _requestCounter,
                contract,
                (int)TWSUtils.BarSizeConverter(request.Frequency),
                RealTimeBarType.Trades, request.RTHOnly);
            return _requestCounter;
        }

        public void CancelRealTimeData(int requestID)
        {
            _client.CancelRealTimeBars(requestID);
        }

        /// <summary>
        /// Add a message to the log.
        ///</summary>
        private void Log(LogLevel level, string message)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
                _logger.Log(level, message));
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

                    RequestHistoricalData(_historicalDataRequests[requestID]);
                    Log(LogLevel.Info, string.Format("IB Repeating historical data request for {0} @ {1}",
                            symbol,
                            _historicalDataRequests[requestID].Frequency));
                }
            }
        }

        public void Dispose()
        {
            Properties.Settings.Default.ibClientRequestCounter = _requestCounter;
            _client.Dispose();
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

        public event EventHandler<ErrorArgs> Error;

        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

        public event EventHandler<QDMS.HistoricalDataEventArgs> HistoricalDataArrived;
    }
}