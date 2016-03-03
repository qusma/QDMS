// -----------------------------------------------------------------------
// <copyright file="Quandl.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

//XML file structure:
//-<dataset>
//<errors> </errors>
//<id type="integer">2298870</id>
//<source-code>OFDP</source-code>
//<code>FUTURE_CL1</code>
//<name>NYMEX Crude Oil Futures, Continuous Contract #1 (CL1) (Front Month)</name>
//<urlize-name>NYMEX-Crude-Oil-Futures-Continuous-Contract-1-CL1-Front-Month</urlize-name>
//<description> <p>Historical Futures Prices: Crude Oil Futures, Continuous Contract #1. Non-adjusted price based on spot-month continuous contract calculations. Raw futures data from New York Mercantile Exchange (NYMEX). </p> </description>
//<updated-at>2013-12-13T01:59:28Z</updated-at>
//<frequency>daily</frequency>
//<from-date>1983-03-30</from-date>
//<to-date>2013-12-12</to-date>
//-<column-names type="array">
//<column-name>Date</column-name>
//<column-name>Open</column-name>
//<column-name>High</column-name>
//<column-name>Low</column-name>
//<column-name>Settle</column-name>
//<column-name>Volume</column-name>
//<column-name>Open Interest</column-name>
//</column-names>
//<private type="boolean">false</private>
//<type nil="true"/>
//<display-url>http://www.ofdp.org/continuous_contracts/data?exchange=NYM&symbol=CL&depth=1</display-url>
//-<data type="array">
//-<datum type="array">
//<datum>2013-12-12</datum>
//<datum type="float">97.55</datum>
//<datum type="float">98.18</datum>
//<datum type="float">97.31</datum>
//<datum type="float">97.5</datum>
//<datum nil="true"/>
//<datum type="float">153787.0</datum>
//</datum>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using NLog;
using QDMS;
using QDMS.Annotations;

namespace QDMSServer.DataSources
{
    public class Quandl : IHistoricalDataSource
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private string authToken;

        public Quandl(string authToken)
        {
            Name = "Quandl";
            this.authToken = authToken;
        }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            if (request.Frequency < BarSize.OneDay)
            {
                throw new Exception("Quandl unsupported bar size. Minimum is one day.");
            }

            string freqString = "daily"; //collapse=none|daily|weekly|monthly|quarterly|annual
            switch (request.Frequency)
            {
                case BarSize.OneWeek:
                    freqString = "weekly";
                    break;
                case BarSize.OneMonth:
                    freqString = "monthly";
                    break;
                case BarSize.OneQuarter:
                    freqString = "quarterly";
                    break;
                case BarSize.OneYear:
                    freqString = "annual";
                    break;
            }

            string requestURL = string.Format(
                "http://www.quandl.com/api/v1/datasets/{0}.xml?trim_start={1}&trim_end={2}&sort_order=asc&collapse={3}",
                request.Instrument.DatasourceSymbol,
                request.StartingDate.ToString("yyyy-MM-dd"),
                request.EndingDate.ToString("yyyy-MM-dd"),
                freqString);

            //if the user has provided an authentication code, we slap it on at the end
            if (!string.IsNullOrEmpty(Properties.Settings.Default.quandlAuthCode))
                requestURL += string.Format("&auth_token={0}", authToken);

            //download the data
            string data;
            using (WebClient webClient = new WebClient())
            {
                try
                {
                    data = webClient.DownloadString(requestURL);
                }
                catch (Exception ex)
                {
                    string errMsg = "Quandl: error downloading data. " + ex.Message;
                    Log(LogLevel.Error, errMsg);
                    RaiseEvent(Error, this, new ErrorArgs(-1, errMsg));
                    return;
                }
            }

            //then parse it
            List<OHLCBar> bars;
            try
            {
                bars = QuandlUtils.ParseXML(data);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, ex.Message);
                RaiseEvent(Error, this, new ErrorArgs(-1, ex.Message));
                return;
            }

            //send back the data using the HistoricalDataArrived event
            RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, bars));
        }

        

        /// <summary>
        /// Connect to the data source.
        /// </summary>
        public void Connect()
        {
            
        }

        private void Log(LogLevel level, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
                _logger.Log(level, message));
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

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
