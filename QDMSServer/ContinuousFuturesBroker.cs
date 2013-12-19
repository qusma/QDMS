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

        public ContinuousFuturesBroker(IDataClient client = null, IInstrumentSource instrumentMgr = null)
        {
            if (client == null)
            {
                _client = new QDMSClient.QDMSClient(
                    "CONTFUTCLIENT",
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
                    UnderlyingSymbol = request.Instrument.UnderlyingSymbol, 
                    Type = InstrumentType.Future,
                    DatasourceID = request.Instrument.DatasourceID,
                    Datasource = request.Instrument.Datasource
                };

            var futures = _instrumentMgr.FindInstruments(search: searchInstrument);

            //filter the futures months, we may not want all of them.
            for (int i = 1; i <= 12; i++)
            {
                if (cf.MonthIsUsed(i))
                {
                    futures = futures.Where(x => x.Expiration != null && x.Expiration.Value.Month != i).ToList();
                }
            }

            //TODO not 100% certain about this, but I think that the first contract we need
            //is the first one expiring before the start of the request period
            var firstExpiration = futures
                .Where(x => x.Expiration != null && x.Expiration.Value < request.StartingDate)
                .Select(x => x.Expiration.Value).Max();

            futures = futures.Where(x => x.Expiration != null && x.Expiration.Value < firstExpiration).ToList();

            //order them by ascending expiration date
            futures = futures.OrderBy(x => x.Expiration).ToList();

            //save the number of requests we're gonna make
            lock(_reqCountLock)
            {
                _requestCounts.Add(request.AssignedID, futures.Count);
            }
            
            //save the contracts used, we need them later
            _contracts.Add(request.AssignedID, futures);

            //request the data for all futures left
            foreach (Instrument i in futures)
            {
                //grab the entire data series for every contract
                var req = new HistoricalDataRequest(
                    i,
                    request.Frequency,
                    new DateTime(1, 1, 1),
                    i.Expiration.Value,
                    rthOnly: request.RTHOnly);
                int requestID = _client.RequestHistoricalData(req);
                _histReqIDMap.Add(requestID, req.AssignedID);

            }
        }

        private void CalcContFutData(HistoricalDataRequest request)
        {
            //TODO perhaps start by cleaning up the data, it is possible that some of the futures may not have had ANY data returned!
            //copy over the list of contracts that we're gonna be using
            List<Instrument> futures = new List<Instrument>(_contracts[request.AssignedID]);

            var cf = request.Instrument.ContinuousFuture;

            Instrument frontFuture = futures.First();
            Instrument backFuture = futures.ElementAt(1);

            //sometimes the contract will be based on the Xth month
            //this is where we keep track of the actual contract currently being used
            Instrument selectedFuture = futures.ElementAt(cf.Month - 1);

            List<OHLCBar> frontData = _data[frontFuture.ID.Value];
            List<OHLCBar> backData = _data[backFuture.ID.Value];
            List<OHLCBar> selectedData = _data[selectedFuture.ID.Value];

            DateTime currentDate = frontData[0].DT;
            //final date is the earliest of: the last date of data available, or the request's endingdate
            DateTime lastDateAvailable = futures.Last().Expiration.Value;
            DateTime finalDate = request.EndingDate < lastDateAvailable ? request.EndingDate : lastDateAvailable;

            List<OHLCBar> cfData = new List<OHLCBar>();

            //these ints keep track of the index of the "current" date in the contract data
            DateTime tmpDate = currentDate;
            int frontIndex = frontData.IndexOf(x => x.DT >= tmpDate);
            int backIndex = backData.IndexOf(x => x.DT >= tmpDate);
            int selectedIndex = selectedData.IndexOf(x => x.DT >= tmpDate);

            decimal adjustmentFactor = 0;
            if (cf.AdjustmentMode == ContinuousFuturesAdjustmentMode.Ratio)
            {
                adjustmentFactor = 1;
            }
            

            bool switchContract = false;
            int counter = 0; //some rollover rules require multiple consecutive days of greater vol/OI...this keeps track of that

            //is it possible that it might be better to start at "now" and calculate backwards instead?
            while (currentDate < finalDate)
            {
                cfData.Add(selectedData[selectedIndex]);

                //do we need to switch contracts?
                switch (cf.RolloverType)
                {
                    case ContinuousFuturesRolloverType.Time:
                        if ((frontFuture.Expiration.Value - currentDate).TotalDays <= cf.RolloverDays)
                        {
                            switchContract = true;
                        }
                        break;
                    case ContinuousFuturesRolloverType.Volume:
                        if (backData[backIndex].Volume > frontData[frontIndex].Volume)
                            counter++;
                        else
                            counter = 0;
                        switchContract = counter >= cf.RolloverDays;
                        break;
                    case ContinuousFuturesRolloverType.OpenInterest:
                        if (backData[backIndex].OpenInterest > frontData[frontIndex].OpenInterest)
                            counter++;
                        else
                            counter = 0;
                        switchContract = counter >= cf.RolloverDays;
                        break;
                    case ContinuousFuturesRolloverType.VolumeAndOpenInterest:
                        if (backData[backIndex].OpenInterest > frontData[frontIndex].OpenInterest &&
                            backData[backIndex].Volume > frontData[frontIndex].Volume)
                            counter++;
                        else
                            counter = 0;
                        switchContract = counter >= cf.RolloverDays;
                        break;
                    case ContinuousFuturesRolloverType.VolumeOrOpenInterest:
                        if (backData[backIndex].OpenInterest > frontData[frontIndex].OpenInterest ||
                            backData[backIndex].Volume > frontData[frontIndex].Volume)
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

                //finally advance the time and indices
                currentDate = currentDate.Add(request.Frequency.ToTimeSpan());
                frontIndex++;
                backIndex++;
                selectedIndex++;

                //we switch to the next contract
                if (switchContract)
                {
                    //make any required price adjustments
                    if (cf.AdjustmentMode == ContinuousFuturesAdjustmentMode.Difference)
                    {
                        adjustmentFactor = frontData[frontIndex].Close - backData[backIndex].Close;
                        foreach (OHLCBar bar in cfData)
                        {
                            AdjustBar(bar, adjustmentFactor, cf.AdjustmentMode);
                        }
                    }
                    else if (cf.AdjustmentMode == ContinuousFuturesAdjustmentMode.Ratio)
                    {
                        adjustmentFactor = frontData[frontIndex].Close / backData[backIndex].Close;
                        foreach (OHLCBar bar in cfData)
                        {
                            AdjustBar(bar, adjustmentFactor, cf.AdjustmentMode);
                        }
                    }


                    //update the contracts
                    frontFuture = backFuture;
                    backFuture = futures.FirstOrDefault(x => x.Expiration > backFuture.Expiration);
                    selectedFuture = futures.Where(x => x.Expiration >= frontFuture.Expiration).ElementAt(cf.Month - 1);

                    frontData = _data[frontFuture.ID.Value];
                    backData = _data[backFuture.ID.Value];
                    selectedData = _data[selectedFuture.ID.Value];

                    //find the indices of the "current" time in the data series
                    frontIndex = frontData.IndexOf(x => x.DT >= currentDate);
                    backIndex = backData.IndexOf(x => x.DT >= currentDate);
                    selectedIndex = selectedData.IndexOf(x => x.DT >= currentDate);

                    //there's some sort of problem with the data and the date we want isn't found in the series
                    if (frontIndex < 0)
                    {
                        Log(LogLevel.Error, string.Format("Error constructing continuous future ID {0}, no data on contract id {1}",
                            request.Instrument.ContinuousFutureID,
                            frontFuture.ID.Value));
                        break;
                    }

                    if (backIndex < 0)
                    {
                        Log(LogLevel.Error, string.Format("Error constructing continuous future ID {0}, no data on contract id {1}",
                            request.Instrument.ContinuousFutureID,
                            backFuture.ID.Value));
                        break;
                    }

                    if (selectedIndex < 0)
                    {
                        Log(LogLevel.Error, string.Format("Error constructing continuous future ID {0}, no data on contract id {1}",
                            request.Instrument.ContinuousFutureID,
                            selectedFuture.ID.Value));
                        break;
                    }



                    switchContract = false;
                }
            }

            //clean up
            _contracts.Remove(request.AssignedID);
            
            //throw out any data from before the start of the request
            cfData = cfData.Where(x => x.DT >= request.StartingDate && x.DT <= request.EndingDate).ToList();

            //we're done, so just raise the event
            HistoricalDataArrived(this, new HistoricalDataEventArgs(request, cfData));
        }

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="continuousFuture"></param>
        /// <returns>The current front future for this continuous futures contract. Null if it is not found.</returns>
        public Instrument GetCurrentFrontFuture(Instrument continuousFuture)
        {
            var searchInstrument = new Instrument { UnderlyingSymbol = continuousFuture.UnderlyingSymbol, Type = InstrumentType.Future };
            var futures = _instrumentMgr.FindInstruments(search: searchInstrument);

            if (futures.Count == 0) return null;
            //TODO write this
            return new Instrument();
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
    }
}
#pragma warning restore 67