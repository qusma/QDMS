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
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using QDMS;
using QDMS.Annotations;
using QDMS.Utils;

#pragma warning disable 67
namespace QDMSServer.DataSources
{
    public sealed class Yahoo : IHistoricalDataSource
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly Thread _downloaderThread;
        private readonly ConcurrentQueue<HistoricalDataRequest> _queuedRequests;
        private bool _runDownloader;
        private HttpClient _client;
        private HttpClientHandler _handler;
        private string _crumb;
        private readonly object _lockObj = new object();

        public Yahoo()
        {
            _queuedRequests = new ConcurrentQueue<HistoricalDataRequest>();
            _downloaderThread = new Thread(DownloaderLoop);
        }

        private async void DownloaderLoop()
        {
            HistoricalDataRequest req;
            while(_runDownloader)
            {
                while(_queuedRequests.TryDequeue(out req))
                {
                    RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(req, await GetData(req)));
                }
                Thread.Sleep(15);
            }
        }

        /// <summary>
        /// Connect to the data source.
        /// </summary>
        public async void Connect()
        {
            //make sure only one connection attempt at a time
            if (!Monitor.TryEnter(_lockObj)) return;
            //make sure any async methods have ConfigureAwait(true) here, otherwise the lock fails

            if (Connected)
            {
                Monitor.Exit(_lockObj);
                return;
            }

            var cookieContainer = new CookieContainer();
            _handler = new HttpClientHandler { CookieContainer = cookieContainer };
            _client = new HttpClient(_handler);

            //Get cookie and crumb...they can be used across all requests

            string url = "https://uk.finance.yahoo.com/quote/AAPL/history";
            var response = await _client.GetAsync(url).ConfigureAwait(true);
            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("Could not get crumb/cookie");
                Monitor.Exit(_lockObj);
                return;
            }
            string cookie = response.Headers.FirstOrDefault(x => x.Key == "Set-Cookie").Value.First().Split(';')[0];
            cookieContainer.Add(new Uri("https://yahoo.com"), new Cookie("Cookie", cookie));

            //get the crumb
            string html = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            var match = Regex.Match(html, "\"CrumbStore\":{\"crumb\":\"(?<crumb>[^\"]+)\"}");
            if (!match.Success)
            {
                _logger.Error("Failed to extract crumb");
                Monitor.Exit(_lockObj);
                return;
            }
            _crumb = Regex.Unescape(match.Groups[1].Value);

            _runDownloader = true;
            _downloaderThread.Start();
            Monitor.Exit(_lockObj);
        }

        /// <summary>
        /// Disconnect from the data source.
        /// </summary>
        public void Disconnect()
        {
            _runDownloader = false;
            _downloaderThread.Join();
            _client?.Dispose();
            _client = null;
        }

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        public bool Connected => _runDownloader;

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name => "Yahoo";

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            _queuedRequests.Enqueue(request);
        }

        //Downloads data from yahoo. First dividends and splits, then actual price data
        private async Task<List<OHLCBar>> GetData(HistoricalDataRequest request)
        {
            var barSize = request.Frequency;
            var startDate = request.StartingDate;
            var endDate = request.EndingDate;
            var instrument = request.Instrument;
            var symbol = string.IsNullOrEmpty(instrument.DatasourceSymbol) ? instrument.Symbol : instrument.DatasourceSymbol;

            if (barSize < BarSize.OneDay) throw new Exception("Bar size not supporterd"); //yahoo can't give us anything better than 1 day
            if (startDate > endDate) throw new Exception("Start date after end date"); //obvious

            var data = new List<OHLCBar>();

            
            //Splits
            string splitURL = string.Format(@"https://query1.finance.yahoo.com/v7/finance/download/{0}?period1={1}&period2={2}&interval=1d&events=split&crumb={3}",
                symbol,
                ToTimestamp(startDate),
                ToTimestamp(endDate),
                _crumb);

            var 

            response = await _client.GetAsync(splitURL).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = string.Format("Error downloading split data from Yahoo, symbol {0}. Status code: {1} Url: ({2})",
                    instrument.Symbol,
                    response.StatusCode,
                    splitURL);
                _logger.Log(LogLevel.Error, errorMessage);

                RaiseEvent(Error, this, new ErrorArgs(0, errorMessage));

                return new List<OHLCBar>();
            }

            string contents = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            //stick dividends and splits into their respective dictionaries to be used later
            //the key is the date in yyyy-MM-dd format

            //Split format:
            //Date,Stock Splits
            //2014-06-09,7/1
            var splits = new Dictionary<string, decimal>();
            string[] rows = contents.Split("\n".ToCharArray());
            for (int j = 1; j < rows.Length - 1; j++) //start at 1 because the first line's a header
            {
                string[] items = rows[j].Split(",".ToCharArray());
                decimal splitNumerator, splitDenominator;
                string date = items[0];

                string[] splitFactors = items[1].Trim().Split("/".ToCharArray());

                if (decimal.TryParse(splitFactors[0], out splitNumerator) && decimal.TryParse(splitFactors[1], out splitDenominator))
                    splits.Add(date, splitNumerator / splitDenominator);
            }


            //Dividends
            string divURL = string.Format(@"https://query1.finance.yahoo.com/v7/finance/download/{0}?period1={1}&period2={2}&interval=1d&events=div&crumb={3}",
                symbol,
                ToTimestamp(startDate),
                ToTimestamp(endDate),
                _crumb);

            response = await _client.GetAsync(divURL).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = string.Format("Error downloading dividend data from Yahoo, symbol {0}. Status code: {1} Url: ({2})",
                    instrument.Symbol,
                    response.StatusCode,
                    divURL);
                _logger.Log(LogLevel.Error, errorMessage);

                RaiseEvent(Error, this, new ErrorArgs(0, errorMessage));

                return new List<OHLCBar>();
            }

            //Dividend Format
            //Date,Dividends
            //2014-11-06,0.47
            contents = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            rows = contents.Split("\n".ToCharArray());
            var dividends = new Dictionary<string, decimal>();
            for (int j = 1; j < rows.Length - 1; j++) //start at 1 because the first line's a header
            {
                decimal dividend;
                string[] items = rows[j].Split(",".ToCharArray());
                string date = items[0];

                if (decimal.TryParse(items[1], out dividend))
                    dividends.Add(date, dividend);
            }


            //now download the actual price data
            //csv file comes in the following format:
            //Date,Open,High,Low,Close,Adj Close,Volume
            //2014-06-02,90.565712,90.690002,88.928574,628.650024,89.807144,92337700
            string dataURL = string.Format(@"https://query1.finance.yahoo.com/v7/finance/download/{0}?period1={1}&period2={2}&interval=1d&events=history&crumb={3}",
                symbol,
                ToTimestamp(startDate),
                ToTimestamp(endDate),
                _crumb);

            response = await _client.GetAsync(dataURL).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = string.Format("Error downloading dividend data from Yahoo, symbol {0}. Status code: {1} Url: ({2})",
                    instrument.Symbol,
                    response.StatusCode,
                    dataURL);
                _logger.Log(LogLevel.Error, errorMessage);

                RaiseEvent(Error, this, new ErrorArgs(0, errorMessage));

                return new List<OHLCBar>();
            }

            contents = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

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


                //The OHL values are actually split adjusted, we want to turn them back
                try
                {
                    var dt = DateTime.ParseExact(items[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    bar.DT = dt;
                    decimal adjRatio = decimal.Parse(items[4]) / decimal.Parse(items[5]);
                    bar.Open = decimal.Parse(items[1]) * adjRatio;
                    bar.High = decimal.Parse(items[2]) * adjRatio;
                    bar.Low = decimal.Parse(items[3]) * adjRatio;
                    bar.Close = decimal.Parse(items[4]);
                    bar.Volume = long.Parse(items[6]);
                    //set adj values so that in case they're not set later (eg if we only get one bar), they're still filled in
                    bar.AdjOpen = bar.Open;
                    bar.AdjHigh = bar.High;
                    bar.AdjLow = bar.Low;
                    bar.AdjClose = bar.Close;

                    data.Add(bar);
                }
                catch
                {
                    _logger.Error("Failed to parse line: " + rows[j]);
                }
            }

            //Note that due to the latest change, the adjusted close value is incorrect (doesn't include divs)
            //so we need to calc adj values ourselves
            PriceAdjuster.AdjustData(ref data);

            _logger.Log(LogLevel.Info, string.Format("Downloaded {0} bars from Yahoo, symbol {1}.",
                data.Count,
                instrument.Symbol));

            return data;
        }

        ///<summary>
        /// Raise the event in a threadsafe manner
        ///</summary>
        ///<param name="event"></param>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        ///<typeparam name="T"></typeparam>
        private static void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
        where T : EventArgs
        {
            EventHandler<T> handler = @event;
            handler?.Invoke(sender, e);
        }

        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int ToTimestamp(DateTime dt)
        {
            return (int)(dt.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
#pragma warning restore 67