using IBApi;
using NLog;
using QDMS;
using QDMSIBClient;
using System;
using System.Collections.Generic;

namespace QDMSApp.DataSources
{
    public partial class IB
    {
        private readonly Queue<int> _realTimeRequestQueue = new Queue<int>();

        /// <summary>
        /// This event is raised when real time data arrives
        /// We convert them and pass them on downstream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _client_RealTimeBar(object sender, RealTimeBarEventArgs e)
        {
            var originalRequest = _realTimeDataRequests[e.RequestId];
            RealTimeDataEventArgs args = TWSUtils.RealTimeDataEventArgsConverter(e, originalRequest.Frequency);
            args.InstrumentID = originalRequest.Instrument.ID.Value;
            args.RequestID = _requestIDMap[e.RequestId];
            RaiseEvent(DataReceived, this, args);
        }

        /// <summary>
        /// Sometime realtime bars will stop, and we get the associated error. 
        /// We handle this situation here.
        /// </summary>
        /// <param name="e"></param>
        private void HandleRealtimeBarStop(ErrorEventArgs e)
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

        private void RestartRealtimeStream(RealTimeDataRequest req)
        {
            RequestRealTimeData(req);
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
            catch (Exception ex)
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

        private void ResendRealTimeRequests()
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
        }
    }
}
