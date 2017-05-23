using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace QDMS.Server.DataSources
{
    /// <summary>
    /// Note that this source does not have ETFs
    /// </summary>
    public class QuandlEODStocks : IHistoricalDataSource
    {
        //TODO continuous futures db: https://www.quandl.com/api/v3/datasets/CHRIS/ASX_XT2.json?api_key=APIKEY&start_date=2016-05-17&end_date=2016-11-13
        //Commodity prices: https://www.quandl.com/api/v3/datasets/COM/COFFEE_CLMB.json?api_key=APIKEY&start_date=2016-01-11&end_date=2016-07-10
        //see metadata for available codes
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private string _authToken;

        private HttpClient _client = new HttpClient();
        private ConcurrentQueue<HistoricalDataRequest> _queuedRequests = new ConcurrentQueue<HistoricalDataRequest>();
        private Thread _downloaderThread;
        private bool _runDownloader;

        public QuandlEODStocks(string authToken)
        {
            this._authToken = authToken;
            _downloaderThread = new Thread(DownloaderLoop);
        }

        private async void DownloaderLoop()
        {
            HistoricalDataRequest req;
            while (_runDownloader)
            {
                while (_queuedRequests.TryDequeue(out req))
                {
                    RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(req, await GetData(req)));
                }
                Thread.Sleep(15);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public bool Connected => _runDownloader;
        public string Name => "QuandlEODStocks";

        private async Task<List<OHLCBar>> GetData(HistoricalDataRequest request)
        {
            if (request.Frequency != BarSize.OneDay)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Only OneDay BarSize is supported", request.AssignedID));
                return new List<OHLCBar>();
            }

            var instrument = request.Instrument;
            var symbol = string.IsNullOrEmpty(instrument.DatasourceSymbol) ? instrument.Symbol : instrument.DatasourceSymbol;

            string url = string.Format("https://www.quandl.com/api/v3/datatables/WIKI/PRICES.json?date.gte={0:yyyyMMdd}&date.lt={1:yyyyMMdd}&ticker={2}&api_key={3}",
                request.StartingDate,
                request.EndingDate,
                symbol,
                _authToken);

            var result = await _client.GetAsync(url);
            if (!result.IsSuccessStatusCode)
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, $"Download failed on URL {url}, StatusCode: {result.StatusCode}", request.AssignedID));
                return new List<OHLCBar>();
            }

            var body = await result.Content.ReadAsStringAsync();
            return ParseJson(body);
        }

        private List<OHLCBar> ParseJson(string json)
        {
            var bars = new List<OHLCBar>();
            var jObj = JObject.Parse(json);
            var data = jObj.Root["datatable"]["data"];
            foreach (JArray jsonBar in data)
            {
                var strData = jsonBar.ToObject<string[]>();

                var bar = new OHLCBar();
                bar.DT = DateTime.ParseExact(strData[1], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                bar.Open = decimal.Parse(strData[2]);
                bar.High = decimal.Parse(strData[3]);
                bar.Low = decimal.Parse(strData[4]);
                bar.Close = decimal.Parse(strData[5]);
                bar.Volume = long.Parse(strData[6]);
                bar.Dividend = strData[7] != "0" ? (decimal?)decimal.Parse(strData[7]) : null;
                bar.Split = strData[8] != "1" ? (decimal?)decimal.Parse(strData[8]) : null;
                bar.AdjOpen = decimal.Parse(strData[9]);
                bar.AdjHigh = decimal.Parse(strData[10]);
                bar.AdjLow = decimal.Parse(strData[11]);
                bar.AdjClose = decimal.Parse(strData[12]);
                bar.Frequency = BarSize.OneDay;

                bars.Add(bar);
            }

            return bars;
        }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            _queuedRequests.Enqueue(request);
        }

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;

        public event EventHandler<ErrorArgs> Error;

        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

        ///<summary>
        /// Raise the event in a threadsafe manner
        ///</summary>
        private static void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
            where T : EventArgs
        {
            EventHandler<T> handler = @event;
            handler?.Invoke(sender, e);
        }
    }
}