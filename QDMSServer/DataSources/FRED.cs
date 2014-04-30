// -----------------------------------------------------------------------
// <copyright file="FRED.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// API Info here:
// http://api.stlouisfed.org/docs/fred/
// http://api.stlouisfed.org/docs/fred/series_search.html
// http://api.stlouisfed.org/docs/fred/series_observations.html

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Windows;
using NLog;
using QDMS;

namespace QDMSServer.DataSources
{
    public class FRED : IHistoricalDataSource
    {
        private string _apiKey = "f8d71bdcf1d7153e157e0baef35f67db";
        private Logger _logger = LogManager.GetCurrentClassLogger();

        public event PropertyChangedEventHandler PropertyChanged;

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
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, GetData(request)));
        }

        private List<OHLCBar> GetData(HistoricalDataRequest request)
        {
            var startDate = request.StartingDate;
            var endDate = request.EndingDate;
            var instrument = request.Instrument;
            var symbol = string.IsNullOrEmpty(instrument.DatasourceSymbol) ? instrument.Symbol : instrument.DatasourceSymbol;

            var data = new List<OHLCBar>();

            //todo how to handle frequency? not given in observations query

            using (WebClient webClient = new WebClient())
            {
                string dataURL = "", contents = "";
                try
                {
                    contents = webClient.DownloadString(dataURL);
                }
                catch (WebException ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        _logger.Log(LogLevel.Error, string.Format("Error downloading price data from FRED, symbol {0}: {1} ({2})",
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
            }

            Log(LogLevel.Info, string.Format("Downloaded {0} bars from FRED, symbol {1}.",
                data.Count,
                instrument.Symbol));
                    data.Reverse(); //TODO do we neeed to inverse this data?
            return data;
        }

        /// <summary>
        /// Add a message to the log.
        ///</summary>
        private void Log(LogLevel level, string message)
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.InvokeAsync(() =>
                    _logger.Log(level, message));
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
