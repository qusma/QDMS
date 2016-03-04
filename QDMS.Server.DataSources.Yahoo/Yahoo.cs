// -----------------------------------------------------------------------
// <copyright file="Yahoo.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using NLog;
using QDMS;
using QDMS.Annotations;

#pragma warning disable 67
namespace QDMSServer.DataSources
{
    public sealed class Yahoo : IHistoricalDataSource
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Thread _downloaderThread;
        private ConcurrentQueue<HistoricalDataRequest> _queuedRequests;
        private bool _runDownloader;

        public Yahoo()
        {
            Name = "Yahoo";
            _queuedRequests = new ConcurrentQueue<HistoricalDataRequest>();
            _downloaderThread = new Thread(DownloaderLoop);
        }

        private void DownloaderLoop()
        {
            HistoricalDataRequest req;
            while(_runDownloader)
            {
                while(_queuedRequests.TryDequeue(out req))
                {
                    RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(req, GetData(req)));
                }
                Thread.Sleep(15);
            }
        }

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

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        public bool Connected { get { return _runDownloader; } }

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name { get; private set; }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            _queuedRequests.Enqueue(request);
        }

        //Downloads data from yahoo. First dividends and splits, then actual price data
        private List<OHLCBar> GetData(HistoricalDataRequest request)
        {
            var barSize = request.Frequency;
            var startDate = request.StartingDate;
            var endDate = request.EndingDate;
            var instrument = request.Instrument;
            var symbol = string.IsNullOrEmpty(instrument.DatasourceSymbol) ? instrument.Symbol : instrument.DatasourceSymbol;

            if (barSize < BarSize.OneDay) throw new Exception("Bar size not supporterd"); //yahoo can't give us anything better than 1 day
            if (startDate > endDate) throw new Exception("Start date after end date"); //obvious

            var data = new List<OHLCBar>();

            //start by downloading splits and dividends
            using (WebClient webClient = new WebClient())
            {
                string splitURL = string.Format("http://ichart.finance.yahoo.com/x?s={0}&a={1}&b={2}&c={3}&d={4}&e={5}&f={6}&g=v&y=0&z=30000",
                    symbol,
                    startDate.Month - 1,
                    startDate.Day,
                    startDate.Year,
                    endDate.Month - 1,
                    endDate.Day,
                    endDate.Year);
                string contents;

                try
                {
                    contents = webClient.DownloadString(splitURL);
                }
                catch (WebException ex)
                {
                    _logger.Log(LogLevel.Error, string.Format("Error downloading price data from Yahoo, symbol {0}: {1} ({2})",
                        instrument.Symbol,
                        ex.Message,
                        splitURL));

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

                //stick dividends and splits into their respective dictionaries to be used later
                //the key is the date in yyyy-MM-dd format
                
                //CSV file comes in the following format:
                //DIVIDEND, 20031224,0.014000
                //SPLIT, 20000320,2:1
                var dividends = new Dictionary<string, decimal>();
                var splits = new Dictionary<string, decimal>();
                string[] rows = contents.Split("\n".ToCharArray());
                for (int j = 1; j < rows.Count() - 1; j++) //start at 1 because the first line's a header
                {
                    string[] items = rows[j].Split(",".ToCharArray());


                    if (items[0] == "DIVIDEND")
                    {
                        decimal dividend;
                        string unformattedDate = items[1].Trim();
                        string date = string.Format("{0}-{1}-{2}", unformattedDate.Substring(0, 4), unformattedDate.Substring(4, 2), unformattedDate.Substring(6, 2));

                        if (decimal.TryParse(items[2], out dividend))
                            dividends.Add(date, dividend);
                    }
                    else if (items[0] == "SPLIT")
                    {
                        decimal splitNumerator, splitDenominator;
                        string unformattedDate = items[1].Trim();
                        string date = string.Format("{0}-{1}-{2}", unformattedDate.Substring(0, 4), unformattedDate.Substring(4, 2), unformattedDate.Substring(6, 2));

                        string[] splitFactors = items[2].Trim().Split(":".ToCharArray());

                        if (decimal.TryParse(splitFactors[0], out splitNumerator) && decimal.TryParse(splitFactors[1], out splitDenominator))
                            splits.Add(date, splitNumerator/splitDenominator);
                    }
                }

                //now download the actual price data
                //csv file comes in the following format:
                //Date,Open,High,Low,Close,Volume,Adj Close
                //2013-10-25,175.51,176.00,175.17,175.95,93505700,175.95
                string dataURL = string.Format(@"http://ichart.finance.yahoo.com/table.csv?s={0}&a={1}&b={2}&c={3}&d={4}&e={5}&f={6}&g=d&ignore=.csv",
                    symbol,
                    startDate.Month - 1,
                    startDate.Day,
                    startDate.Year,
                    endDate.Month - 1,
                    endDate.Day,
                    endDate.Year);

                try
                {
                    contents = webClient.DownloadString(dataURL);
                }
                catch (WebException ex)
                {
                    _logger.Log(LogLevel.Error, string.Format("Error downloading price data from Yahoo, symbol {0}: {1} ({2})",
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

                //parse the downloaded price data
                rows = contents.Split("\n".ToCharArray());
                for (int j = 1; j < rows.Count() - 1; j++) //start at 1 because the first line's a header
                {
                    string[] items = rows[j].Split(",".ToCharArray());
                    var bar = new OHLCBar();

                    if (dividends.ContainsKey(items[0]))
                        bar.Dividend = dividends[items[0]];

                    if (splits.ContainsKey(items[0]))
                        bar.Split = splits[items[0]];

                    var dt = DateTime.ParseExact(items[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    bar.DT = dt;
                    bar.Open = decimal.Parse(items[1]);
                    bar.High = decimal.Parse(items[2]);
                    bar.Low = decimal.Parse(items[3]);
                    bar.Close = decimal.Parse(items[4]);
                    bar.Volume = long.Parse(items[5]);
                    bar.AdjClose = decimal.Parse(items[6]);
                    decimal adjFactor = bar.AdjClose.Value / bar.Close;
                    bar.AdjOpen = bar.Open * adjFactor;
                    bar.AdjHigh = bar.High * adjFactor;
                    bar.AdjLow = bar.Low * adjFactor;

                    data.Add(bar);
                }
            }

            _logger.Log(LogLevel.Info, string.Format("Downloaded {0} bars from Yahoo, symbol {1}.",
                data.Count,
                instrument.Symbol));
            data.Reverse(); //data comes sorted newest first, so we need to inverse the order
            return data;
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

        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
#pragma warning restore 67