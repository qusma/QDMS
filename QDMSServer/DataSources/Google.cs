// -----------------------------------------------------------------------
// <copyright file="Google.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows;
using NLog;
using QDMS;

namespace QDMSServer.DataSources
{
    public class Google : IHistoricalDataSource
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event PropertyChangedEventHandler PropertyChanged;

        public Google()
        {
            Name = "Google";
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
        public bool Connected { get { return true; } }

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name { get; private set; }

        //Downloads data from Google. First dividends and splits, then actual price data
        private List<OHLCBar> GetData(HistoricalDataRequest request)
        {
            var barSize = request.Frequency;
            var startDate = request.StartingDate;
            var endDate = request.EndingDate;
            var instrument = request.Instrument;
            var symbol = string.IsNullOrEmpty(instrument.DatasourceSymbol) ? instrument.Symbol : instrument.DatasourceSymbol;

            if (barSize < BarSize.OneDay) throw new Exception("Bar size not supporterd"); //google can't give us anything better than 1 day
            if (startDate > endDate) throw new Exception("Start date after end date"); //obvious

            var data = new List<OHLCBar>();

            //start by downloading splits and dividends
            using (WebClient webClient = new WebClient())
            {
                //download the price data
                //csv file comes in the following format:
                //Date, Open, High, Low, Close, Volume
                //2-May-14,221.55,224.85,220.79,222.90,45543215
                string dataURL = string.Format(@"http://finance.google.com/finance/historical?q={0}&startdate={1}&enddate={2}&output=csv",
                    symbol,
                    startDate.ToString("MMM d,yyyy", CultureInfo.InvariantCulture),
                    endDate.ToString("MMM d,yyyy", CultureInfo.InvariantCulture));

                string contents;

                try
                {
                    contents = webClient.DownloadString(dataURL);
                }
                catch (WebException ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        _logger.Log(LogLevel.Error, string.Format("Error downloading price data from Google, symbol {0}: {1} ({2})",
                            instrument.Symbol,
                            ex.Message,
                            dataURL)));

                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        var resp = (HttpWebResponse)ex.Response;
                        RaiseEvent(Error, this, new ErrorArgs((int)resp.StatusCode, ex.Message));
                    }
                    else
                    {
                        RaiseEvent(Error, this, new ErrorArgs(0, ex.Message));
                    }
                    return new List<OHLCBar>();
                }

                //parse the downloaded price data
                string[] rows = contents.Split("\n".ToCharArray());
                for (int j = 1; j < rows.Count() - 1; j++) //start at 1 because the first line's a header
                {
                    string[] items = rows[j].Split(",".ToCharArray());
                    var bar = new OHLCBar();

                    var dt = DateTime.ParseExact(items[0], "d-MMM-yy", CultureInfo.InvariantCulture);
                    bar.DT = dt;
                    bar.Open = decimal.Parse(items[1]);
                    bar.High = decimal.Parse(items[2]);
                    bar.Low = decimal.Parse(items[3]);
                    bar.Close = decimal.Parse(items[4]);
                    bar.Volume = long.Parse(items[5]);

                    data.Add(bar);
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
                _logger.Log(LogLevel.Info, string.Format("Downloaded {0} bars from Google, symbol {1}.",
                    data.Count,
                    instrument.Symbol)));
            data.Reverse(); //data comes sorted newest first, so we need to inverse the order
            return data;
        }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, GetData(request)));
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

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;

        public event EventHandler<ErrorArgs> Error;

        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
    }
}