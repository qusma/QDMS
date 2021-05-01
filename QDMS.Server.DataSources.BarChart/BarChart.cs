// -----------------------------------------------------------------------
// <copyright file="BarChart.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using QDMS;
using QDMS.Server.DataSources.BarChart;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;

namespace QDMSApp.DataSources
{
    public class BarChart : IHistoricalDataSource
    {
        private readonly string _apiKey;
        private Thread _downloaderThread;
        private ConcurrentQueue<HistoricalDataRequest> _queuedRequests;
        private bool _runDownloader;

        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

        public event EventHandler<ErrorArgs> Error;

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;

        public event PropertyChangedEventHandler PropertyChanged;

        public BarChart(ISettings settings)
        {
            _apiKey = settings.barChartApiKey;
            _queuedRequests = new ConcurrentQueue<HistoricalDataRequest>();
            _downloaderThread = new Thread(DownloaderLoop);
        }

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        public bool Connected => _runDownloader;

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name => "BarChart";

        /// <summary>
        /// Connect to the data source.
        /// </summary>
        public void Connect()
        {
            _runDownloader = true;
            _downloaderThread.Start();
        }

        /// <summary>
        /// Disconnect from the data source.
        /// </summary>
        public void Disconnect()
        {
            _runDownloader = false;
            _downloaderThread.Join();
        }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            _queuedRequests.Enqueue(request);
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

        private string BuildRequestUrl(HistoricalDataRequest request)
        {
            string symbol = string.IsNullOrEmpty(request.Instrument.DatasourceSymbol) ? request.Instrument.Symbol : request.Instrument.DatasourceSymbol;
            string type = GetBarType(request.Frequency);
            string startDate = request.StartingDate.ToString("yyyyMMddHHmmss");
            string endDate = request.EndingDate.ToString("yyyyMMddHHmmss");
            int interval = GetInterval(request.Frequency);

            //Here we set splits=0 and dividends=0, meaning the data will NOT be adjusted
            //However we will get no information on dividends or splits! Just a shitty API, not much to do about it right now...
            string url =
                $"http://marketdata.websol.barchart.com/getHistory.json?key={_apiKey}&symbol={symbol}&type={type}&startDate={startDate}&endDate={endDate}&interval={interval}&splits=0&dividends=0";

            return url;
        }

        private void DownloaderLoop()
        {
            HistoricalDataRequest req;
            while (_runDownloader)
            {
                if (_queuedRequests.TryDequeue(out req))
                {
                    var data = GetData(req);
                    if (data != null)
                    {
                        RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(req, data));
                    }
                }
                Thread.Sleep(15);
            }
        }

        private string GetBarType(BarSize barSize)
        {
            switch (barSize)
            {
                case BarSize.OneDay:
                    return "daily";

                case BarSize.OneWeek:
                    return "weekly";

                case BarSize.OneQuarter:
                    return "quarterly";

                case BarSize.OneYear:
                    return "yearly";

                case BarSize.OneMinute:
                case BarSize.TwoMinutes:
                case BarSize.FiveMinutes:
                case BarSize.FifteenMinutes:
                case BarSize.ThirtyMinutes:
                case BarSize.OneHour:
                    return "minutes";

                case BarSize.Tick:
                    return "tick";

                default:
                    throw new Exception("Unsupported frequency");
            }
        }

        private List<OHLCBar> GetData(HistoricalDataRequest request)
        {
            string url = "";
            try
            {
                url = BuildRequestUrl(request);
            }
            catch (Exception ex)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "BarChart Error building URL: " + ex.Message, request.RequestID));
                return null;
            }

            using (var client = new WebClient())
            {
                string data;
                JObject parsedData;

                try
                {
                    data = client.DownloadString(url);
                    parsedData = JObject.Parse(data);
                    if (parsedData["status"]["code"].ToString() != "200")
                    {
                        throw new Exception(parsedData["status"]["message"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    RaiseEvent(Error, this, new ErrorArgs(-1, "BarChart data download error: " + ex.Message, request.RequestID));
                    return null;
                }

                try
                {
                    var bars = BarChartUtils.ParseJson(parsedData, request);
                    return bars;
                }
                catch (Exception ex)
                {
                    RaiseEvent(Error, this, new ErrorArgs(-1, "BarChart data parsing error: " + ex.Message, request.RequestID));
                    return null;
                }
            }
        }
        private int GetInterval(BarSize frequency)
        {
            switch (frequency)
            {
                case BarSize.TwoMinutes:
                    return 2;

                case BarSize.FiveMinutes:
                    return 5;

                case BarSize.FifteenMinutes:
                    return 15;

                case BarSize.ThirtyMinutes:
                    return 30;

                case BarSize.OneHour:
                    return 60;

                default:
                    return 1;
            }
        }
    }
}