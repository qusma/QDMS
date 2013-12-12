// -----------------------------------------------------------------------
// <copyright file="ContinuousFuturesBroker.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using QDMS;
using System;

namespace QDMSServer
{
    public class ContinuousFuturesBroker : IDisposable, IHistoricalDataSource
    {
        private QDMSClient.QDMSClient _client;

        //This dictionary uses instrument IDs as keys, and holds the data that we use to construct our futures
        private Dictionary<int, List<OHLCBar>> _data;

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        public ContinuousFuturesBroker()
        {
            _client = new QDMSClient.QDMSClient(
                "CONTFUTCLIENT",
                "localhost",
                Properties.Settings.Default.rtDBReqPort,
                Properties.Settings.Default.rtDBPubPort,
                Properties.Settings.Default.instrumentServerPort,
                Properties.Settings.Default.hDBPort);

            _client.HistoricalDataReceived += _client_HistoricalDataReceived;
            _data = new Dictionary<int, List<OHLCBar>>();

            Name = "ContinuousFutures";
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
        }

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

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name { get; private set; }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            var cf = request.Instrument.ContinuousFuture;
            var searchInstrument = new Instrument 
                { 
                    UnderlyingSymbol = request.Instrument.UnderlyingSymbol, 
                    Type = InstrumentType.Future,
                    DatasourceID = request.Instrument.DatasourceID,
                    Datasource = request.Instrument.Datasource
                };

            var futures = InstrumentManager.FindInstruments(search: searchInstrument);

            //filter the futures months, we may not want all of them.
            for (int i = 1; i <= 12; i++)
            {
                if (cf.MonthIsUsed(i))
                {
                    futures = futures.Where(x => x.Expiration != null && x.Expiration.Value.Month != i).ToList();
                }
            }

            //order them by ascending expiration date
            futures = futures.OrderBy(x => x.Expiration).ToList();

            //TODO so somewhere around here we need to just wait until all the data has arrived

            //TODO these two are the "candidates"...at each bar we check if it's time to 
            Instrument frontFuture = new Instrument();
            Instrument backFuture = new Instrument();

            List<OHLCBar> frontData = new List<OHLCBar>();
            List<OHLCBar> backData = new List<OHLCBar>();
            
            //TODO starting point...we need to find the switchover point BEFORE the request's starting date
            DateTime currentDate = DateTime.Now;
            //TODO final date is either the last date of data available, or the request's endingdate
            DateTime finalDate = DateTime.Now;

            List<OHLCBar> cfData = new List<OHLCBar>();

            bool switchContract = false;
            //is it possible that it might be better to start at "now" and calculate backwards instead?
            while (currentDate < finalDate)
            {

                //do we need to switch contracts?
                if (cf.RolloverType == ContinuousFuturesRolloverType.Time)
                {
                    if ((frontFuture.Expiration.Value - currentDate).TotalDays <= cf.RolloverDays)
                    {
                        switchContract = true;
                    }
                }
                else if (cf.RolloverType == ContinuousFuturesRolloverType.Volume)
                {

                }
                else if (cf.RolloverType == ContinuousFuturesRolloverType.OpenInterest)
                {

                }
                else if (cf.RolloverType == ContinuousFuturesRolloverType.VolumeAndOpenInterest)
                {

                }
                else if (cf.RolloverType == ContinuousFuturesRolloverType.VolumeOrOpenInterest)
                {

                }


                //we switch to the next contract
                if (switchContract)
                {
                    frontFuture = backFuture;
                    backFuture = futures.FirstOrDefault(x => x.Expiration > backFuture.Expiration);

                    frontData = _data[frontFuture.ID.Value];
                    backData = _data[backFuture.ID.Value];

                    switchContract = false;
                }
            }

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
            var futures = InstrumentManager.FindInstruments(search: searchInstrument);

            if (futures.Count == 0) return null;

            return new Instrument();
        }
    }
}
