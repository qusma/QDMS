// -----------------------------------------------------------------------
// <copyright file="ContinuousFuturesBroker.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// The idea here then is the following:
// Receive a request for data in RequestHistoricalData().
// In there, figure out which actual futures contracts we need, and request their data.
// Keep track of that stuff in _requestIDs.
// When all the data has arrived, figure out how to stich it together in CalcContFutData()
// Finally send it out using the HistoricalDataArrived event

//TODO It's a bit of a mess right now, refactor into something smarter
//TODO if a request for contract data fails we need to take care of that somehow

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NLog;
using QDMS;
using System;
using QLNet;
using Instrument = QDMS.Instrument;

#pragma warning disable 67
namespace QDMSServer
{
    public class ContinuousFuturesBroker : IDisposable, IHistoricalDataSource
    {
        private IDataClient _client;
        private IInstrumentSource _instrumentMgr;

        // Keeps track of how many historical data requests remain until we can calculate the continuous prices
        // Key: request ID, Value: number of requests outstanding
        private readonly Dictionary<int, int> _requestCounts;

        // This keeps track of which futures contract requests belong to which continuous future request
        // Key: contract request ID, Value: continuous future request ID
        private readonly Dictionary<int, int> _histReqIDMap;

        // Key: request ID, Value: the list of contracts used to fulfill this request
        private readonly Dictionary<int, List<Instrument>> _contracts;

        // Keeps track of the requests. Key: request ID, Value: the request
        private readonly Dictionary<int, HistoricalDataRequest> _requests;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly object _reqCountLock = new object();

        //This dictionary uses instrument IDs as keys, and holds the data that we use to construct our futures
        private Dictionary<int, List<OHLCBar>> _data;

        public ContinuousFuturesBroker(IDataClient client = null, IInstrumentSource instrumentMgr = null, string clientName = "")
        {
            if (client == null)
            {
                if(string.IsNullOrEmpty(clientName))
                    clientName = "CONTFUTCLIENT";

                _client = new QDMSClient.QDMSClient(
                    clientName,
                    "localhost",
                    Properties.Settings.Default.rtDBReqPort,
                    Properties.Settings.Default.rtDBPubPort,
                    Properties.Settings.Default.instrumentServerPort,
                    Properties.Settings.Default.hDBPort);
            }
            else
            {
                _client = client;
            }

            _instrumentMgr = instrumentMgr ?? new InstrumentManager();

            _client.HistoricalDataReceived += _client_HistoricalDataReceived;
            _client.Error += _client_Error;

            _client.Connect();
            
            _data = new Dictionary<int, List<OHLCBar>>();
            _contracts = new Dictionary<int, List<Instrument>>();
            _requestCounts = new Dictionary<int, int>();
            _requests = new Dictionary<int, HistoricalDataRequest>();
            _histReqIDMap = new Dictionary<int, int>();

            Name = "ContinuousFutures";
        }

        void _client_Error(object sender, ErrorArgs e)
        {
            
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        private void _client_HistoricalDataReceived(object sender, HistoricalDataEventArgs e)
        {
            if (e.Request.Instrument.ID == null) return;
            int id = e.Request.Instrument.ID.Value;
            if (_data.ContainsKey(id))
            {
                _data.Remove(id);
            }
            _data.Add(id, e.Data);


            lock (_reqCountLock)
            {
                //get the request id of the continuous futures request that caused this contract request
                int cfReqID = _histReqIDMap[e.Request.RequestID];
                _histReqIDMap.Remove(e.Request.RequestID);
                _requestCounts[cfReqID]--;

                if (_requestCounts[cfReqID] == 0) 
                {
                    //we have received all the data we asked for
                    //so now we want to generate the continuous prices
                    _requestCounts.Remove(e.Request.RequestID);
                    CalcContFutData(_requests[cfReqID]);
                }
            }
         
            //TODO if we call it from here though, then that locks up the client thread while we do the calcs
            //not sure what the best approach is
        }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            // add it to the collection of requests so we can access it later
            _requests.Add(request.AssignedID, request);

            var cf = request.Instrument.ContinuousFuture;
            var searchInstrument = new Instrument 
                { 
                    UnderlyingSymbol = request.Instrument.ContinuousFuture.UnderlyingSymbol.Symbol, 
                    Type = InstrumentType.Future,
                    DatasourceID = request.Instrument.DatasourceID,
                    Datasource = request.Instrument.Datasource
                };

            var futures = _instrumentMgr.FindInstruments(search: searchInstrument);

            //order them by ascending expiration date
            futures = futures.OrderBy(x => x.Expiration).ToList();

            //filter the futures months, we may not want all of them.
            for (int i = 1; i <= 12; i++)
            {
                if (!cf.MonthIsUsed(i))
                {
                    futures = futures.Where(x => x.Expiration != null && x.Expiration.Value.Month != i).ToList();
                }
            }

            //nothing found, return with empty hands
            if (futures.Count == 0)
            {
                RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, new List<OHLCBar>()));
                return;
            }

            //the first contract we need is the first one expiring before the start of the request period
            var firstExpiration = futures
                .Where(x => x.Expiration != null && x.Expiration.Value < request.StartingDate)
                .Select(x => x.Expiration.Value).Max();

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


            //save the number of requests we're gonna make
            lock(_reqCountLock)
            {
                _requestCounts.Add(request.AssignedID, futures.Count);
            }
            
            //save the contracts used, we need them later
            _contracts.Add(request.AssignedID, futures);

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
                    daysBack += (int) (i.Expiration.Value - prevInst.Expiration.Value).TotalDays;
                }
                daysBack += 30 * cf.Month;

                var req = new HistoricalDataRequest(
                    i,
                    request.Frequency,
                    i.Expiration.Value.AddDays(-daysBack),
                    i.Expiration.Value,
                    rthOnly: request.RTHOnly);
                int requestID = _client.RequestHistoricalData(req);
                _histReqIDMap.Add(requestID, request.AssignedID);

                prevInst = i;
            }
        }

        private void CalcContFutData(HistoricalDataRequest request)
        {
            //copy over the list of contracts that we're gonna be using
            List<Instrument> futures = new List<Instrument>(_contracts[request.AssignedID]);

            //start by cleaning up the data, it is possible that some of the futures may not have had ANY data returned!
            futures = futures.Where(x => _data[x.ID.Value].Count > 0).ToList();

            var cf = request.Instrument.ContinuousFuture;

            Instrument frontFuture = futures.First();
            Instrument backFuture = futures.ElementAt(1);

            //sometimes the contract will be based on the Xth month
            //this is where we keep track of the actual contract currently being used
            Instrument selectedFuture = futures.ElementAt(cf.Month - 1);

            TimeSeries frontData = new TimeSeries(_data[frontFuture.ID.Value]);
            TimeSeries backData = new TimeSeries(_data[backFuture.ID.Value]);
            TimeSeries selectedData = new TimeSeries(_data[selectedFuture.ID.Value]);

            //starting date: I think it's enough to start a few days before the expiration of the first future
            //as long as it's before the starting date?
            DateTime currentDate = frontData[0].DT;

            //This is a super dirty hack to make non-time based rollovers actually work.
            //The reason is that the starting point will otherwise be a LONG time before the date we're interested in.
            //And at that time both the front and back futures are really far from expiration.
            //As such volumes can be wonky, and thus result in a rollover far before we ACTUALLY would
            //want to roll over if we had access to even earlier data.
            if (frontFuture.Expiration.Value < request.StartingDate)
                currentDate = frontFuture.Expiration.Value.AddDays(-10);
            
            //final date is the earliest of: the last date of data available, or the request's endingdate
            DateTime lastDateAvailable = futures.Last().Expiration.Value;
            DateTime finalDate = request.EndingDate < lastDateAvailable ? request.EndingDate : lastDateAvailable;

            List<OHLCBar> cfData = new List<OHLCBar>();


            decimal adjustmentFactor = 0;
            if (cf.AdjustmentMode == ContinuousFuturesAdjustmentMode.Ratio)
            {
                adjustmentFactor = 1;
            }

            Calendar calendar = MyUtils.GetCalendarFromCountryCode("US"); //TODO make this dynamic

            bool switchContract = false;
            int counter = 0; //some rollover rules require multiple consecutive days of greater vol/OI...this keeps track of that

            //add the first piece of data we have available, and start looping
            cfData.Add(selectedData[0]);

            while (currentDate < finalDate)
            {
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
                        if (backData != null && backData[0].Volume > frontData[0].Volume)
                            counter++;
                        else
                            counter = 0;
                        switchContract = counter >= cf.RolloverDays;
                        break;
                    case ContinuousFuturesRolloverType.OpenInterest:
                        if (backData != null && backData[0].OpenInterest > frontData[0].OpenInterest)
                            counter++;
                        else
                            counter = 0;
                        switchContract = counter >= cf.RolloverDays;
                        break;
                    case ContinuousFuturesRolloverType.VolumeAndOpenInterest:
                        if (backData != null && backData[0].OpenInterest > frontData[0].OpenInterest &&
                            backData[0].Volume > frontData[0].Volume)
                            counter++;
                        else
                            counter = 0;
                        switchContract = counter >= cf.RolloverDays;
                        break;
                    case ContinuousFuturesRolloverType.VolumeOrOpenInterest:
                        if (backData != null && backData[0].OpenInterest > frontData[0].OpenInterest ||
                            backData[0].Volume > frontData[0].Volume)
                            counter++;
                        else
                            counter = 0;
                        switchContract = counter >= cf.RolloverDays;
                        break;
                }
                
                if (frontFuture.Expiration.Value <= currentDate) 
                {
                    //no matter what, obviously we need to switch if the contract expires
                    switchContract = true;
                }

                //finally advance the time and indices...keep moving forward until the selected series has moved
                bool selectedSeriesProgressed = false;
                do
                {
                    currentDate = currentDate.Add(request.Frequency.ToTimeSpan());
                    selectedSeriesProgressed = selectedData.AdvanceTo(currentDate);
                    frontData.AdvanceTo(currentDate);
                    if(backData != null)
                        backData.AdvanceTo(currentDate);
                }
                while (!selectedSeriesProgressed && !switchContract && !selectedData.ReachedEndOfSeries);


                //we switch to the next contract
                if (switchContract)
                {
                    //make any required price adjustments
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
                    frontFuture = backFuture;
                    backFuture = futures.FirstOrDefault(x => x.Expiration > backFuture.Expiration);
                    selectedFuture = futures.Where(x => x.Expiration >= frontFuture.Expiration).ElementAtOrDefault(cf.Month - 1);
                    

                    if (frontFuture == null) break; //no other futures left, get out
                    if (selectedFuture == null) break;

                    frontData = new TimeSeries(_data[frontFuture.ID.Value]);
                    backData = backFuture != null ? new TimeSeries(_data[backFuture.ID.Value]) : null;
                    selectedData = new TimeSeries(_data[selectedFuture.ID.Value]);

                    frontData.AdvanceTo(currentDate);
                    if(backData != null)
                        backData.AdvanceTo(currentDate);
                    selectedData.AdvanceTo(currentDate);

                    //TODO add a bit of error checking here, it's not guaranteed that the data fits perfectly here


                    switchContract = false;
                }

                cfData.Add(selectedData[0]);
            }

            //clean up
            _contracts.Remove(request.AssignedID);
            
            //throw out any data from before the start of the request
            cfData = cfData.Where(x => x.DT >= request.StartingDate && x.DT <= request.EndingDate).ToList();

            //we're done, so just raise the event
            RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, cfData));
        }

        private void Log(LogLevel level, string message)
        {
            Application.Current.Dispatcher.Invoke( () =>
                _logger.Log(level, message));
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
        /// Returns an ID uniquely identifying this request.
        /// The contract is returned asynchronously through the FoundFrontContract event.
        /// </summary>
        public int FindFrontContract(Instrument cfInstrument, int requestID, DateTime? date = null)
        {
            if (!cfInstrument.IsContinuousFuture) throw new Exception("Not a continuous future instrument.");

            DateTime currentDate = date ?? DateTime.Now;

            var cf = cfInstrument.ContinuousFuture;
            if (cf.RolloverType == ContinuousFuturesRolloverType.Time)
            {
                //TODO the cf might not be using all months...

                //if the roll-over is time based, we can find the appropriate contract programmatically
                DateTime currentMonthsExpirationDate = cf.UnderlyingSymbol.ExpirationDate(currentDate.Year, currentDate.Month);
                DateTime switchOverDate = currentMonthsExpirationDate;
                DateTime selectedDate = currentDate;

                Calendar calendar = MyUtils.GetCalendarFromCountryCode("US"); //TODO make this dynamic

                //this month's contract
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
                if (selectedDate >= switchOverDate)
                {
                    while (monthsLeft > 0)
                    {
                        if (cf.MonthIsUsed(selectedDate.AddMonths(count).Month))
                            monthsLeft--;
                        count++;
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

                RaiseEvent(FoundFrontContract, this, new FoundFrontContractEventArgs(requestID, contract));
                //TODO problem: we should be returning the ID before raising the event...
            }
            else //otherwise, we have to actually look at the historical data to figure out which contract is selected
            {
                //TODO this is a tough one, because it needs to be asynchronous (historical
                //data can take a long time to download). Not sure where it fits in, either...
                //move it to the ContinuousFuturesBroker?
            }

            return 0;
        }

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
        public event EventHandler<FoundFrontContractEventArgs> FoundFrontContract;
    }
}
#pragma warning restore 67