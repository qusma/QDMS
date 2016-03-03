// -----------------------------------------------------------------------
// <copyright file="ContinuousFuturesBroker.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// This class serves two primary functions: construct continuous futures data,
// and find what the current "front" contract for a continuous future instrument is.
//
// How the data works:
// Receive a request for data in RequestHistoricalData().
// In there, figure out which actual futures contracts we need, and request their data.
// Keep track of that stuff in _requestIDs.
// When all the data has arrived, figure out how to stich it together in CalcContFutData()
// Finally send it out using the HistoricalDataArrived event.
//
// How finding the front contract works:
// Receive request on RequestFrontContract() and return a unique ID identifying this request.
// If it's a time-based switch over, just calculate it on the spot.
// Otherwise we have to grab the data and do the calculations in CalcContFutData() which returns
// the final contract used.
// The result is findally returned through the FoundFrontContract event.


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using NLog;
using QDMS;
using QDMS.Annotations;
using QLNet;
using Instrument = QDMS.Instrument;
using Timer = System.Timers.Timer;

#pragma warning disable 67

namespace QDMSServer
{
    public class ContinuousFuturesBroker : IContinuousFuturesBroker
    {
        private IDataClient _client;
        private readonly IInstrumentSource _instrumentMgr;

        /// <summary>
        /// Keeps track of how many historical data requests remain until we can calculate the continuous prices
        /// Key: request ID, Value: number of requests outstanding
        /// </summary>
        private readonly Dictionary<int, int> _requestCounts;

        /// <summary>
        /// This keeps track of which futures contract requests belong to which continuous future request
        /// Key: contract request ID, Value: continuous future request ID
        /// </summary>
        private readonly Dictionary<int, int> _histReqIDMap;

        /// <summary>
        /// Key: request ID, Value: the list of contracts used to fulfill this request
        /// </summary>
        private readonly Dictionary<int, List<Instrument>> _contracts;

        /// <summary>
        ///  Keeps track of the requests. Key: request ID, Value: the request
        /// </summary>
        private readonly Dictionary<int, HistoricalDataRequest> _requests;

        /// <summary>
        /// Front contract requests that need data to be downloaded, receive a HistoricalDataRequest
        /// that is held in _requests. The same AssignedID also corresponds to a FrontContractRequest held here.
        /// </summary>
        private readonly Dictionary<int, FrontContractRequest> _frontContractRequestMap;

        /// <summary>
        /// keeps track of whether requests are for data, or for the front contract. True = data.
        /// </summary>
        private readonly Dictionary<int, bool> _requestTypes;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly object _reqCountLock = new object();
        private readonly object _requestsLock = new object();
        private readonly object _dataUsesLock = new object();
        private readonly object _dataLock = new object();
        private readonly object _frontContractReturnLock = new object();

        /// <summary>
        /// Holds requests for the front contract of a CF, before they get processed
        /// </summary>
        private readonly BlockingCollection<FrontContractRequest> _frontContractRequests;

        /// <summary>
        /// Used to give unique IDs to front contract requests
        /// </summary>
        private int _lastFrontDontractRequestID;

        /// <summary>
        ///This dictionary uses instrument IDs as keys, and holds the data that we use to construct our futures
        ///Key: a KVP where key: the instrument ID, and value: the data frequency
        ///Value: data
        /// </summary>
        private readonly Dictionary<KeyValuePair<int, BarSize>, List<OHLCBar>> _data;

        /// <summary>
        ///Some times there will be multiple requests for data being filled concurrently.
        ///And sometimes they will be for the same instrument. Thus we can't safely delete data from _data,
        ///because it might still be needed!
        ///So here we keep track of the number of requests that are going to use the data...
        ///and use that number to then finally free up the memory when all related requests are completed
        ///Key: a KVP where key: the instrument ID, and value: the data frequency
        ///Value: data
        /// </summary>
        private readonly Dictionary<KeyValuePair<int, BarSize>, int> _dataUsesPending;

        private readonly Timer _reconnectTimer;

        public ContinuousFuturesBroker(IDataClient client, IInstrumentSource instrumentMgr, bool connectImmediately = true)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            _client = client;

            _instrumentMgr = instrumentMgr;

            _client.HistoricalDataReceived += _client_HistoricalDataReceived;
            _client.Error += _client_Error;
            if(connectImmediately)
                _client.Connect();

            
            _data = new Dictionary<KeyValuePair<int, BarSize>, List<OHLCBar>>();
            _contracts = new Dictionary<int, List<Instrument>>();
            _requestCounts = new Dictionary<int, int>();
            _requests = new Dictionary<int, HistoricalDataRequest>();
            _histReqIDMap = new Dictionary<int, int>();
            _frontContractRequests = new BlockingCollection<FrontContractRequest>();
            _requestTypes = new Dictionary<int, bool>();
            _frontContractRequestMap = new Dictionary<int, FrontContractRequest>();
            _dataUsesPending = new Dictionary<KeyValuePair<int, BarSize>, int>();

            _reconnectTimer = new Timer(1000);
            _reconnectTimer.Elapsed += _reconnectTimer_Elapsed;
            _reconnectTimer.Start();

            Name = "ContinuousFutures";
        }

        void _reconnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_client.Connected)
            {
                _reconnectTimer.Stop();
                Log(LogLevel.Info, "CFB: trying to reconnect.");
                try
                {
                    _client.Connect();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, "CFB error when trying to connect: " + ex.Message);
                }
            }
        }



        private void _client_Error(object sender, ErrorArgs e)
        {
            Log(LogLevel.Error, "Continuous futures broker client error: " + e.ErrorMessage);
        }

        public void Dispose()
        {
            if (_reconnectTimer != null)
                _reconnectTimer.Dispose();
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
            if (_frontContractRequests != null)
                _frontContractRequests.Dispose();
        }

        /// <summary>
        /// Historical data has arrived. 
        /// Add it to our data store, then check if all requests have been 
        /// fulfilled for a particular continuous futures request. If they 
        /// have, go do the calculations.
        /// </summary>
        private void _client_HistoricalDataReceived(object sender, HistoricalDataEventArgs e)
        {
            if (e.Request.Instrument.ID == null) return;
            int id = e.Request.Instrument.ID.Value;
            var kvpID = new KeyValuePair<int, BarSize>(id, e.Request.Frequency);

            lock (_data)
            {
                if (_data.ContainsKey(kvpID))
                {
                    //We already have data on this instrument ID/Frequency combo.
                    //Add the arrived data, then discard any doubles, then order.
                    _data[kvpID].AddRange(e.Data);
                    _data[kvpID] = _data[kvpID].Distinct((x, y) => x.DT == y.DT).ToList();
                    _data[kvpID] = _data[kvpID].OrderBy(x => x.DT).ToList();
                }
                else
                {
                    //We have nothing on this instrument ID/Frequency combo.
                    //Just add a new entry in the dictionary.
                    _data.Add(kvpID, e.Data);
                }
            }

            //Here we check if all necessary requests have arrived. If they have, do some work.
            lock (_reqCountLock)
            {
                //get the request id of the continuous futures request that caused this contract request
                int cfReqID = _histReqIDMap[e.Request.RequestID];
                _histReqIDMap.Remove(e.Request.RequestID);
                _requestCounts[cfReqID]--;

                if (_requestCounts[cfReqID] == 0)
                {
                    //we have received all the data we asked for
                    _requestCounts.Remove(e.Request.RequestID);
                    HistoricalDataRequest req;
                    lock (_requestsLock)
                    {
                        req = _requests[cfReqID];
                        _requests.Remove(cfReqID);
                    }

                    if (_requestTypes[cfReqID])
                    {
                        //This request originates from a CF data request
                        //so now we want to generate the continuous prices
                        GetContFutData(req);
                    }
                    else
                    {
                        //This request originates from a front contract request
                        Instrument frontContract = GetContFutData(req, false);
                        FrontContractRequest originalReq = _frontContractRequestMap[cfReqID];
                        lock (_frontContractReturnLock)
                        {
                            RaiseEvent(FoundFrontContract, this, new FoundFrontContractEventArgs(originalReq.ID, frontContract, originalReq.Date == null ? DateTime.Now : originalReq.Date.Value));
                        }
                    }

                    _requestTypes.Remove(e.Request.AssignedID);
                }
            }
        }

        /// <summary>
        /// Taking a historical data request for a continuous futures instrument,
        /// it returns a list of requests for the underlying contracts that are needed to fulfill it.
        /// </summary>
        private List<HistoricalDataRequest> GetRequiredRequests(HistoricalDataRequest request)
        {
            var requests = new List<HistoricalDataRequest>();

            var cf = request.Instrument.ContinuousFuture;
            var searchInstrument = new Instrument
            {
                UnderlyingSymbol = request.Instrument.ContinuousFuture.UnderlyingSymbol.Symbol,
                Type = InstrumentType.Future,
                DatasourceID = request.Instrument.DatasourceID
            };

            var futures = _instrumentMgr.FindInstruments(search: searchInstrument);

            if (futures == null)
            {
                Log(LogLevel.Error, "CFB: Error in GetRequiredRequests, failed to return any contracts to historical request ID: " + request.AssignedID);
                return requests;
            }

            //remove any continuous futures
            futures = futures.Where(x => !x.IsContinuousFuture).ToList();

            //order them by ascending expiration date
            futures = futures.OrderBy(x => x.Expiration).ToList();

            //filter the futures months, we may not want all of them.
            for (int i = 1; i <= 12; i++)
            {
                if (cf.MonthIsUsed(i)) continue;
                int i1 = i;
                futures = futures.Where(x => x.Expiration.HasValue && x.Expiration.Value.Month != i1).ToList();
            }

            //nothing found, return with empty hands
            if (futures.Count == 0)
            {
                return requests;
            }

            //the first contract we need is the first one expiring before the start of the request period
            var expiringBeforeStart = futures
                .Where(x => x.Expiration != null && x.Expiration.Value < request.StartingDate)
                .Select(x => x.Expiration.Value).ToList();

            DateTime firstExpiration = 
                expiringBeforeStart.Count > 0 
                    ? expiringBeforeStart.Max() 
                    : futures.Select(x => x.Expiration.Value).Min();

            futures = futures.Where(x => x.Expiration != null && x.Expiration.Value >= firstExpiration).ToList();

            //I think the last contract we need is the one that is N months after the second contract that expires after the request period end
            //where N is the number of months away from the front contract that the CF uses
            var firstExpAfterEnd = futures.Where(x => x.Expiration > request.EndingDate).ElementAtOrDefault(1);
            if (firstExpAfterEnd != null)
            {
                DateTime limitDate = firstExpAfterEnd.Expiration.Value.AddMonths(request.Instrument.ContinuousFuture.Month - 1);
                futures = futures.Where(x => x.Expiration.Value.Year < limitDate.Year ||
                    (x.Expiration.Value.Year == limitDate.Year && x.Expiration.Value.Month <= limitDate.Month)).ToList();
            }

            //Make sure each month's contract is allowed only once, even if there are multiple copies in the db
            //Sometimes you might get two versions of the same contract with 1 day difference in expiration date
            //so this step is necessary to clean that up
            futures = futures.Distinct(x => x.Expiration.Value.ToString("yyyyMM").GetHashCode()).ToList();

            //save the number of requests we're gonna make
            lock (_reqCountLock)
            {
                _requestCounts.Add(request.AssignedID, futures.Count);
            }

            //save the contracts used, we need them later
            _contracts.Add(request.AssignedID, futures);

            Log(LogLevel.Info, string.Format("CFB: fulfilling historical request ID {0}, requested data on contracts: {1}",
                request.AssignedID,
                string.Join(", ", futures.Select(x => x.Symbol))));

            Instrument prevInst = null;
            //request the data for all futures left
            foreach (Instrument i in futures)
            {
                //the question of how much data, exactly, to ask for is complicated...
                //I'm going with: difference of expiration dates (unless it's the first one, in which case 30 days)
                //plus a month
                //plus the CF selected month
                int daysBack = 30;
                if (prevInst == null)
                {
                    daysBack += 30;
                }
                else
                {
                    daysBack += (int)(i.Expiration.Value - prevInst.Expiration.Value).TotalDays;
                }
                daysBack += 30 * cf.Month;

                var endDate = i.Expiration.Value > DateTime.Now.Date ? DateTime.Now.Date : i.Expiration.Value;

                var req = new HistoricalDataRequest(
                    i,
                    request.Frequency,
                    endDate.AddDays(-daysBack),
                    endDate,
                    rthOnly: request.RTHOnly,
                    dataLocation: request.DataLocation == DataLocation.LocalOnly ? DataLocation.LocalOnly : DataLocation.Both);

                requests.Add(req);

                prevInst = i;
            }

            return requests;
        }

        /// <summary>
        /// Make a request for historical continuous futures data.
        /// The data is returned through the HistoricalDataArrived event.
        /// </summary>
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            Log(LogLevel.Info,
                string.Format("CFB: Received historical data request: {0} @ {1} ({2}) from {3} to {4} - ID: {5}",
                request.Instrument.Symbol,
                request.Frequency,
                request.Instrument.Datasource.Name,
                request.StartingDate,
                request.EndingDate,
                request.AssignedID));

            // add it to the collection of requests so we can access it later
            lock (_requestsLock)
            {
                _requests.Add(request.AssignedID, request);
            }
            _requestTypes.Add(request.AssignedID, true);

            //find what contracts we need
            var reqs = GetRequiredRequests(request);

            //if nothing is found, return right now with no data
            if (reqs.Count == 0)
            {
                RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, new List<OHLCBar>()));
                return;
            }

            //send out the requests
            foreach (HistoricalDataRequest req in reqs)
            {
                lock (_dataUsesLock)
                {
                    var kvp = new KeyValuePair<int, BarSize>(req.Instrument.ID.Value, req.Frequency);
                    if (_dataUsesPending.ContainsKey(kvp))
                        _dataUsesPending[kvp]++;
                    else
                        _dataUsesPending.Add(kvp, 1);
                }
                int requestID = _client.RequestHistoricalData(req);
                _histReqIDMap.Add(requestID, request.AssignedID);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The last contract used in the construction of this continuous futures instrument.</returns>
        private Instrument GetContFutData(HistoricalDataRequest request, bool raiseDataEvent = true)
        {
            //copy over the list of contracts that we're gonna be using
            List<Instrument> futures = new List<Instrument>(_contracts[request.AssignedID]);

            //start by cleaning up the data, it is possible that some of the futures may not have had ANY data returned!
            lock (_dataLock)
            {
                futures = futures.Where(x => _data[new KeyValuePair<int, BarSize>(x.ID.Value, request.Frequency)].Count > 0).ToList();
            }

            var cf = request.Instrument.ContinuousFuture;

            Instrument frontFuture = futures.FirstOrDefault();
            Instrument backFuture = futures.ElementAt(1);

            if (frontFuture == null)
            {
                if (raiseDataEvent)
                {
                    RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, new List<OHLCBar>()));
                }
                return null;
            }

            //sometimes the contract will be based on the Xth month
            //this is where we keep track of the actual contract currently being used
            Instrument selectedFuture = futures.ElementAt(cf.Month - 1);
            Instrument lastUsedSelectedFuture = selectedFuture;


            //final date is the earliest of: the last date of data available, or the request's endingdate
            DateTime lastDateAvailable = new DateTime(1,1,1);
            

            TimeSeries frontData, backData, selectedData;
            lock (_dataLock)
            {
                frontData = new TimeSeries(_data[new KeyValuePair<int, BarSize>(frontFuture.ID.Value, request.Frequency)]);
                backData = new TimeSeries(_data[new KeyValuePair<int, BarSize>(backFuture.ID.Value, request.Frequency)]);
                selectedData = new TimeSeries(_data[new KeyValuePair<int, BarSize>(selectedFuture.ID.Value, request.Frequency)]);

                lastDateAvailable = _data[new KeyValuePair<int, BarSize>(futures.Last().ID.Value, request.Frequency)].Last().DT;
            }

            DateTime finalDate = request.EndingDate < lastDateAvailable ? request.EndingDate : lastDateAvailable;

            //This is a super dirty hack to make non-time based rollovers actually work.
            //The reason is that the starting point will otherwise be a LONG time before the date we're interested in.
            //And at that time both the front and back futures are really far from expiration.
            //As such volumes can be wonky, and thus result in a rollover far before we ACTUALLY would
            //want to roll over if we had access to even earlier data.
            DateTime currentDate = frontFuture.Expiration.Value.AddDays(-20 - cf.RolloverDays);

            frontData.AdvanceTo(currentDate);
            backData.AdvanceTo(currentDate);
            selectedData.AdvanceTo(currentDate);

            List<OHLCBar> cfData = new List<OHLCBar>();

            Calendar calendar = MyUtils.GetCalendarFromCountryCode("US");

            bool switchContract = false;
            int counter = 0; //some rollover rules require multiple consecutive days of greater vol/OI...this keeps track of that
            List<long> frontDailyVolume = new List<long>(); //keeps track of how much volume has occured in each day
            List<int> frontDailyOpenInterest = new List<int>(); //keeps track of open interest on a daily basis
            List<long> backDailyVolume = new List<long>();
            List<int> backDailyOpenInterest = new List<int>();
            long frontTodaysVolume = 0, backTodaysVolume = 0;

            //add the first piece of data we have available, and start looping
            cfData.Add(selectedData[0]);

            //the first time we go from one day to the next we don't want to check for switching conditions
            //because we need to ensure that we use an entire day's worth of volume data.
            bool firstDaySwitchover = true;

            while (currentDate < finalDate)
            {
                //keep track of total volume "today"
                if (frontData[0].Volume.HasValue) frontTodaysVolume += frontData[0].Volume.Value;
                if (backData != null && backData[0].Volume.HasValue) backTodaysVolume += backData[0].Volume.Value;

                if (frontData.CurrentBar > 0 && frontData[0].DT.Day != frontData[1].DT.Day)
                {
                    if (firstDaySwitchover)
                    {
                        firstDaySwitchover = false;
                        frontTodaysVolume = 0;
                        backTodaysVolume = 0;
                    }

                    frontDailyVolume.Add(frontTodaysVolume);
                    backDailyVolume.Add(backTodaysVolume);

                    if (frontData[0].OpenInterest.HasValue) frontDailyOpenInterest.Add(frontData[0].OpenInterest.Value);
                    if (backData != null && backData[0].OpenInterest.HasValue) backDailyOpenInterest.Add(backData[0].OpenInterest.Value);

                    frontTodaysVolume = 0;
                    backTodaysVolume = 0;

                    //do we need to switch contracts?
                    switch (cf.RolloverType)
                    {
                        case ContinuousFuturesRolloverType.Time:
                            if (MyUtils.BusinessDaysBetween(currentDate, frontFuture.Expiration.Value, calendar) <= cf.RolloverDays)
                            {
                                switchContract = true;
                            }
                            break;

                        case ContinuousFuturesRolloverType.Volume:
                            if (backData != null && backDailyVolume.Last() > frontDailyVolume.Last())
                                counter++;
                            else
                                counter = 0;
                            switchContract = counter >= cf.RolloverDays;
                            break;

                        case ContinuousFuturesRolloverType.OpenInterest:
                            if (backData != null && backDailyOpenInterest.Last() > frontDailyOpenInterest.Last())
                                counter++;
                            else
                                counter = 0;
                            switchContract = counter >= cf.RolloverDays;
                            break;

                        case ContinuousFuturesRolloverType.VolumeAndOpenInterest:
                            if (backData != null && backDailyOpenInterest.Last() > frontDailyOpenInterest.Last() &&
                                backDailyVolume.Last() > frontDailyVolume.Last())
                                counter++;
                            else
                                counter = 0;
                            switchContract = counter >= cf.RolloverDays;
                            break;

                        case ContinuousFuturesRolloverType.VolumeOrOpenInterest:
                            if (backData != null && backDailyOpenInterest.Last() > frontDailyOpenInterest.Last() ||
                                backDailyVolume.Last() > frontDailyVolume.Last())
                                counter++;
                            else
                                counter = 0;
                            switchContract = counter >= cf.RolloverDays;
                            break;
                    }
                }

                if (frontFuture.Expiration.Value <= currentDate)
                {
                    //no matter what, obviously we need to switch if the contract expires
                    switchContract = true;
                }

                //finally if we have simply run out of data, we're forced to switch
                if (frontData.ReachedEndOfSeries)
                {
                    switchContract = true;
                }

                //finally advance the time and indices...keep moving forward until the selected series has moved
                frontData.NextBar();
                currentDate = frontData[0].DT;
                if (backData != null)
                {
                    backData.AdvanceTo(currentDate);
                }
                selectedData.AdvanceTo(currentDate);

                //this next check here is necessary for the time-based switchover to work after weekends or holidays
                if (cf.RolloverType == ContinuousFuturesRolloverType.Time &&
                    MyUtils.BusinessDaysBetween(currentDate, frontFuture.Expiration.Value, calendar) <= cf.RolloverDays)
                {
                    switchContract = true;
                }

                //we switch to the next contract
                if (switchContract)
                {
                    //make any required price adjustments
                    decimal adjustmentFactor;
                    if (cf.AdjustmentMode == ContinuousFuturesAdjustmentMode.Difference)
                    {
                        adjustmentFactor = backData[0].Close - frontData[0].Close;
                        foreach (OHLCBar bar in cfData)
                        {
                            AdjustBar(bar, adjustmentFactor, cf.AdjustmentMode);
                        }
                    }
                    else if (cf.AdjustmentMode == ContinuousFuturesAdjustmentMode.Ratio)
                    {
                        adjustmentFactor = backData[0].Close / frontData[0].Close;
                        foreach (OHLCBar bar in cfData)
                        {
                            AdjustBar(bar, adjustmentFactor, cf.AdjustmentMode);
                        }
                    }

                    //update the contracts
                    var prevFront = frontFuture;
                    frontFuture = backFuture;
                    backFuture = futures.FirstOrDefault(x => x.Expiration > backFuture.Expiration);
                    var prevSelected = selectedFuture;
                    selectedFuture = futures.Where(x => x.Expiration >= frontFuture.Expiration).ElementAtOrDefault(cf.Month - 1);
                    
                    Log(LogLevel.Info,
                        string.Format("CFB Filling request for {0}: switching front contract from {1} to {2} (selected contract from {3} to {4}) at {5}",
                        request.Instrument.Symbol,
                        prevFront.Symbol,
                        frontFuture.Symbol,
                        prevSelected.Symbol,
                        selectedFuture == null
                            ? ""
                            : selectedFuture.Symbol,
                        currentDate.ToString("yyyy-MM-dd")));



                    if (frontFuture == null) break; //no other futures left, get out
                    if (selectedFuture == null) break;

                    lock (_dataLock)
                    {
                        frontData = new TimeSeries(_data[new KeyValuePair<int, BarSize>(frontFuture.ID.Value, request.Frequency)]);
                        backData = backFuture != null ? new TimeSeries(_data[new KeyValuePair<int, BarSize>(backFuture.ID.Value, request.Frequency)]) : null;
                        selectedData = new TimeSeries(_data[new KeyValuePair<int, BarSize>(selectedFuture.ID.Value, request.Frequency)]);
                    }

                    frontData.AdvanceTo(currentDate);
                    if (backData != null)
                        backData.AdvanceTo(currentDate);
                    selectedData.AdvanceTo(currentDate);

                    //TODO make sure that the data series actually cover the current date

                    switchContract = false;
                    lastUsedSelectedFuture = selectedFuture;
                }

                cfData.Add(selectedData[0]);
            }

            //clean up
            _contracts.Remove(request.AssignedID);

            //throw out any data from before the start of the request
            cfData = cfData.Where(x => x.DT >= request.StartingDate && x.DT <= request.EndingDate).ToList();

            //we're done, so just raise the event
            if (raiseDataEvent)
                RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, cfData));

            //clean up some data!
            lock (_dataUsesLock)
            {
                foreach (Instrument i in futures)
                {
                    var kvp = new KeyValuePair<int, BarSize>(i.ID.Value, request.Frequency);
                    if (_dataUsesPending[kvp] == 1) //this data isn't needed anywhere else, we can delete it
                    {
                        _dataUsesPending.Remove(kvp);
                        lock (_dataLock)
                        {
                            _data.Remove(kvp);
                        }
                    }
                    else
                    {
                        _dataUsesPending[kvp]--;
                    }
                }
            }

            return lastUsedSelectedFuture;
        }

        private void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }

        private void AdjustBar(OHLCBar bar, decimal adjustmentFactor, ContinuousFuturesAdjustmentMode mode)
        {
            if (mode == ContinuousFuturesAdjustmentMode.NoAdjustment) return;
            if (mode == ContinuousFuturesAdjustmentMode.Difference)
            {
                bar.Open += adjustmentFactor;
                bar.High += adjustmentFactor;
                bar.Low += adjustmentFactor;
                bar.Close += adjustmentFactor;
            }
            else if (mode == ContinuousFuturesAdjustmentMode.Ratio)
            {
                bar.Open *= adjustmentFactor;
                bar.High *= adjustmentFactor;
                bar.Low *= adjustmentFactor;
                bar.Close *= adjustmentFactor;
            }
        }

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Connect to the data source.
        /// </summary>
        public void Connect()
        {
        }

        /// <summary>
        /// Disconnect from the data source.
        /// </summary>
        public void Disconnect()
        {
        }

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        public bool Connected
        {
            get
            {
                return true;
            }
        }

        static private void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
            where T : EventArgs
        {
            EventHandler<T> handler = @event;
            if (handler == null) return;
            handler(sender, e);
        }

        /// <summary>
        /// Finds the currently active futures contract for a continuous futures instrument.
        /// The contract is returned asynchronously through the FoundFrontContract event.
        /// </summary>
        /// <returns>Returns an ID uniquely identifying this request.</returns>
        public int RequestFrontContract(Instrument cfInstrument, DateTime? date = null)
        {
            if (!cfInstrument.IsContinuousFuture) throw new Exception("Not a continuous future instrument.");

            _lastFrontDontractRequestID++;
            var req = new FrontContractRequest
            {
                ID = _lastFrontDontractRequestID,
                Instrument = cfInstrument,
                Date = date
            };

            ProcessFrontContractRequest(req);

            return _lastFrontDontractRequestID;
        }

        /// <summary>
        /// Process a FrontContractRequest
        /// CFs with a time-based switchover are calculated on the spot
        /// CFs with other types of switchover require data, so we send off the appropriate data requests here
        /// </summary>
        private void ProcessFrontContractRequest(FrontContractRequest request)
        {
            Log(LogLevel.Info, 
                string.Format("Processing front contract request for symbol: {0} at: {1}",
                request.Instrument.Symbol,
                request.Date.HasValue ? request.Date.ToString() : "Now"));


            if (request.Instrument.ContinuousFuture.RolloverType == ContinuousFuturesRolloverType.Time)
            {
                ProcessTimeBasedFrontContractRequest(request);
            }
            else //otherwise, we have to actually look at the historical data to figure out which contract is selected
            {
                ProcessDataBasedFrontContractRequest(request);
            }
        }

        /// <summary>
        /// Finds the front contract for continuous futures with non-time-based roll.
        /// </summary>
        private void ProcessDataBasedFrontContractRequest(FrontContractRequest request)
        {
            DateTime currentDate = request.Date ?? DateTime.Now;

            //this is a tough one, because it needs to be asynchronous (historical
            //data can take a long time to download).
            var r = new Random();

            //we use GetRequiredRequests to get the historical requests we need to make
            var tmpReq = new HistoricalDataRequest
            {
                Instrument = request.Instrument,
                StartingDate = currentDate.AddDays(-1),
                EndingDate = currentDate,
                Frequency = BarSize.OneDay
            };

            //give the request a unique id
            lock (_requestsLock)
            {
                int id;
                do
                {
                    id = r.Next();
                } while (_requests.ContainsKey(id));
                tmpReq.AssignedID = id;
                _requests.Add(tmpReq.AssignedID, tmpReq);
            }

            var reqs = GetRequiredRequests(tmpReq);
            //make sure the request is fulfillable with the available contracts, otherwise return empty-handed
            if (reqs.Count == 0 || reqs.Count(x => x.Instrument.Expiration.HasValue && x.Instrument.Expiration.Value >= request.Date) == 0)
            {
                lock (_frontContractReturnLock)
                {
                    RaiseEvent(FoundFrontContract, this, new FoundFrontContractEventArgs(request.ID, null, currentDate));
                }
                lock (_requestsLock)
                {
                    _requests.Remove(tmpReq.AssignedID);
                }
                return;
            }

            // add it to the collection of requests so we can access it later
            _requestTypes.Add(tmpReq.AssignedID, false);

            //add it to the front contract requests map
            _frontContractRequestMap.Add(tmpReq.AssignedID, request);

            //finally send out a request for all the data...when it arrives,
            //we process it and return the required front future
            foreach (HistoricalDataRequest req in reqs)
            {
                lock (_dataUsesLock)
                {
                    var kvp = new KeyValuePair<int, BarSize>(req.Instrument.ID.Value, req.Frequency);
                    if (_dataUsesPending.ContainsKey(kvp))
                        _dataUsesPending[kvp]++;
                    else
                        _dataUsesPending.Add(kvp, 1);
                }
                int requestID = _client.RequestHistoricalData(req);
                _histReqIDMap.Add(requestID, tmpReq.AssignedID);
            }
        }

        /// <summary>
        /// Finds the front contract for continuous futures with time-based roll.
        /// </summary>
        private void ProcessTimeBasedFrontContractRequest(FrontContractRequest request)
        {
            DateTime currentDate = request.Date ?? DateTime.Now;
            var cf = request.Instrument.ContinuousFuture;

            //if the roll-over is time based, we can find the appropriate contract programmatically
            DateTime selectedDate = currentDate;

            while (!cf.MonthIsUsed(selectedDate.Month))
            {
                selectedDate = selectedDate.AddMonths(1);
            }

            DateTime currentMonthsExpirationDate = cf.UnderlyingSymbol.ExpirationDate(selectedDate.Year, selectedDate.Month);
            DateTime switchOverDate = currentMonthsExpirationDate;

            Calendar calendar = MyUtils.GetCalendarFromCountryCode("US");

            //the front contract
            //find the switchover date
            int daysBack = cf.RolloverDays;
            while (daysBack > 0)
            {
                switchOverDate = switchOverDate.AddDays(-1);
                if (calendar.isBusinessDay(switchOverDate))
                    daysBack--;
            }

            //this month's contract has already been switched to the next one
            int monthsLeft = 1;
            int count = 0;
            if (currentDate >= switchOverDate)
            {
                while (monthsLeft > 0)
                {
                    count++;
                    if (cf.MonthIsUsed(selectedDate.AddMonths(count).Month))
                        monthsLeft--;
                }
                selectedDate = selectedDate.AddMonths(count);
            }

            //we found the "front" month, no go back the required number of months
            //while skipping unused months
            monthsLeft = cf.Month - 1;
            count = 0;
            while (monthsLeft > 0)
            {
                if (cf.MonthIsUsed(selectedDate.AddMonths(count).Month))
                    monthsLeft--;
                count++;
            }
            selectedDate = selectedDate.AddMonths(count);

            //we got the month we want! find the contract
            var searchFunc = new Func<Instrument, bool>(
                x =>
                    x.Expiration.HasValue &&
                    x.Expiration.Value.Month == selectedDate.Month &&
                    x.Expiration.Value.Year == selectedDate.Year &&
                    x.UnderlyingSymbol == cf.UnderlyingSymbol.Symbol);

            var contract = _instrumentMgr.FindInstruments(pred: searchFunc).FirstOrDefault();


            var timer = new Timer(50) { AutoReset = false };
            timer.Elapsed += (sender, e) =>
            {
                lock (_frontContractReturnLock)
                {
                    RaiseEvent(FoundFrontContract, this, new FoundFrontContractEventArgs(request.ID, contract, currentDate));
                }
            };
            timer.Start();
        }

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;

        public event EventHandler<ErrorArgs> Error;

        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

        public event EventHandler<FoundFrontContractEventArgs> FoundFrontContract;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

#pragma warning restore 67