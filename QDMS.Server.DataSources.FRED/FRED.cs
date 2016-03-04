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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Xml.Linq;
using NLog;
using QDMS;

#pragma warning disable 67

namespace QDMSServer.DataSources
{
    public class FRED : IHistoricalDataSource
    {
        private const string ApiKey = "f8d71bdcf1d7153e157e0baef35f67db";
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event PropertyChangedEventHandler PropertyChanged;

        public FRED()
        {
            Name = "FRED";
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

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, GetData(request)));
        }

        private List<OHLCBar> GetData(HistoricalDataRequest request)
        {
            var instrument = request.Instrument;
            var symbol = string.IsNullOrEmpty(instrument.DatasourceSymbol) ? instrument.Symbol : instrument.DatasourceSymbol;

            string dataURL = string.Format("http://api.stlouisfed.org/fred/series/observations?series_id={0}&api_key={1}&observation_start={2}&observation_end={3}&frequency={4}",
                symbol,
                ApiKey,
                request.StartingDate.ToString("yyyy-MM-dd"),
                request.EndingDate.ToString("yyyy-MM-dd"),
                FredUtils.FrequencyToRequestString(request.Frequency));
            string contents;

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    contents = webClient.DownloadString(dataURL);
                }
                catch (WebException ex)
                {
                    _logger.Log(LogLevel.Error, string.Format("Error downloading price data from FRED, symbol {0}: {1} ({2})",
                        instrument.Symbol,
                        ex.Message,
                        dataURL));

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

            XDocument xdoc;
            using (var sr = new StringReader(contents))
            {
                xdoc = XDocument.Load(sr);
            }

            //check if an error was returned instead of data
            //format: <error code="400" message="Bad Request. Value of frequency is not one of: 'm', 'q', 'sa', 'a'."/>
            if (xdoc.Descendants("error").Any())
            {
                XElement error = xdoc.Descendants("error").First();

                string errorText = string.Format("Error downloading price data from FRED, symbol: {0}. URL: {1} Error Code: {2} Message: {3}",
                    symbol,
                    dataURL,
                    error.Attribute("code").Value,
                    error.Attribute("message").Value);

                Log(LogLevel.Error, errorText);

                RaiseEvent(Error, this, new ErrorArgs(0, errorText));

                return new List<OHLCBar>();
            }

            //Parse the data and return it
            List<OHLCBar> data = ParseData(xdoc);

            Log(LogLevel.Info, string.Format("Downloaded {0} bars from FRED, symbol {1}.",
                data.Count,
                instrument.Symbol));

            return data;
        }

        /// <summary>
        /// Parse observations XML into OHLCBars
        /// </summary>
        /// <param name="xdoc"></param>
        /// <returns></returns>
        private static List<OHLCBar> ParseData(XDocument xdoc)
        {
            //format:
            //<observation realtime_start="2013-08-14" realtime_end="2013-08-14" date="1996-01-01" value="10595.1"/>
            var data = new List<OHLCBar>();
            foreach (XElement obs in xdoc.Descendants("observation"))
            {
                DateTime date;

                if (!DateTime.TryParseExact(obs.Attribute("date").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
                {
                    continue;
                }

                decimal value;
                if (!decimal.TryParse(obs.Attribute("value").Value, out value))
                {
                    continue;
                }

                var bar = new OHLCBar
                {
                    DT = date,
                    Open = value,
                    High = value,
                    Low = value,
                    Close = value
                };

                data.Add(bar);
            }
            return data;
        }

        /// <summary>
        /// Add a message to the log.
        ///</summary>
        private void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
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

#pragma warning restore 67