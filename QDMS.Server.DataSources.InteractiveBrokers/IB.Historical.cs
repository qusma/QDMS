using NLog;
using QDMS;
using QDMSIBClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace QDMSApp.DataSources
{
    public partial class IB
    {
        private readonly Queue<int> _historicalRequestQueue = new Queue<int>();

        private readonly ConcurrentDictionary<int, List<OHLCBar>> _arrivedHistoricalData = new ConcurrentDictionary<int, List<OHLCBar>>();

        /// <summary>
        /// Called when historical data _updates_ (ie realtime data) are received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _client_HistoricalDataUpdate(object sender, QDMSIBClient.HistoricalDataEventArgs e)
        {
            if (e.Bar.Volume < 0) return;

            var originalRequest = _realTimeDataRequests[e.RequestId];
            var args = TWSUtils.HistoricalDataEventArgsToRealTimeDataEventArgs(e, originalRequest.Instrument.ID.Value, _requestIDMap[e.RequestId]);
            RaiseEvent(DataReceived, this, args);
        }

        /// <summary>
        /// This event is raised when historical data arrives from TWS
        /// </summary>
        private void _client_HistoricalData(object sender, QDMSIBClient.HistoricalDataEventArgs e)
        {
            //if the data is arriving for a sub-request, we must get the id of the original request first
            int id = GetTopLevelRequestId(e.RequestId);

            if (!_historicalDataRequests.ContainsKey(id))
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

        /// <summary>
        /// Raised when a historical data req has completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _client_HistoricalDataEnd(object sender, HistoricalDataEndEventArgs e)
        {
            bool requestComplete = true;

            int id = GetTopLevelRequestId(e.RequestId);

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
                    //If there are multiple sub-requests, here we check if this is the last one
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
        /// This method is called when a historical data request has delivered all its bars
        /// </summary>
        /// <param name="requestID"></param>
        private void HistoricalDataRequestComplete(int requestID)
        {
            var request = _historicalDataRequests[requestID];
            List<OHLCBar> bars;
            var removed = _arrivedHistoricalData.TryRemove(requestID, out bars);
            if (!removed) return;

            bars = IBHistoricalDataCleaner.CleanHistoricalData(request, bars);

            //return the data through the HistoricalDataArrived event
            RaiseEvent(HistoricalDataArrived, this, new QDMS.HistoricalDataEventArgs(request, bars));
        }

        private void HandleHistoricalDataPacingViolationError(ErrorEventArgs e)
        {
            //the same error can mean different things:
            //either no data at all was returned, in which case we complete the req
            //or this is just a pacing violation in hich case we re-queue it
            if (e.ErrorMsg.Contains("HMDS query returned no data") ||
                e.ErrorMsg.Contains("No market data permissions"))
            {
                HandleNoDataReturnedToHistoricalRequest(e);
            }
            else
            {
                //simply a data pacing violation, re-queue
                HandleHistoricalDataPacingViolation(e);
            }
        }

        /// <summary>
        /// We requested too much, just re-queue
        /// </summary>
        /// <param name="e"></param>
        private void HandleHistoricalDataPacingViolation(ErrorEventArgs e)
        {
            lock (_queueLock)
            {
                if (!_historicalRequestQueue.Contains(e.TickerId))
                {
                    //same as above
                    _historicalRequestQueue.Enqueue(e.TickerId);
                }
            }
        }

        /// <summary>
        /// For some reason the historical data request had zero data returned.
        /// </summary>
        /// <param name="e"></param>
        private void HandleNoDataReturnedToHistoricalRequest(ErrorEventArgs e)
        {
            //no data returned = we return an empty data set
            int origId;

            lock (_subReqMapLock)
            {
                //if the data is arriving for a sub-request, we must get the id of the original request first
                //otherwise it's just the same id
                origId = GetTopLevelRequestId(e.TickerId);
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

        /// <summary>
        /// historical data request
        /// </summary>
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            //Historical data limitations: https://www.interactivebrokers.com/en/software/api/apiguide/api/historical_data_limitations.htm
            //the issue here is that the request may not be fulfilled...so we need to keep track of the request
            //and if we get an error regarding its failure, potentially send it again using a timer
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
                SendHistoricalDataSubrequests(request, originalReqID);
            }
        }

        /// <summary>
        /// Splits up a request into valid sub-requests and sends them all
        /// </summary>
        /// <param name="request"></param>
        /// <param name="originalReqID"></param>
        private void SendHistoricalDataSubrequests(HistoricalDataRequest request, int originalReqID)
        {
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
                    TWSUtils.GetDataType(request.Instrument),
                    request.RTHOnly,
                    false
                );
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "IB: Could not send historical data request: " + ex.Message);
                RaiseEvent(Error, this, new ErrorArgs(-1, "Could not send historical data request: " + ex.Message, id));
            }
        }

        private void ResendHistoricalRequests()
        {
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
}
