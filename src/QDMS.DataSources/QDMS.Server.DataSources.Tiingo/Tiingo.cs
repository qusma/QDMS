// -----------------------------------------------------------------------
// <copyright file="Tiingo.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// API Info here:
// https://www.tiingo.com/documentation/end-of-day

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Channels;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using QDMS.Utils;

#pragma warning disable 67

namespace QDMS.Server.DataSources.Tiingo
{
    /// <summary>
    /// Historical end-of-day bars from Tiingo.
    /// Uses the raw (unadjusted) prices plus the per-bar dividends and split factors, and computes adjusted prices with <see cref="PriceAdjuster"/>.
    /// </summary>
    public class Tiingo : IHistoricalDataSource
    {
        private const string BaseUrl = "https://api.tiingo.com/tiingo/daily/";

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _apiKey;
        private readonly HttpClient _client;
        private readonly Channel<HistoricalDataRequest> _requests =
            Channel.CreateUnbounded<HistoricalDataRequest>(new UnboundedChannelOptions { SingleReader = true });
        private readonly object _connectLock = new object();
        private Task _downloader;
        private bool _warnedNoApiKey;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

        public Tiingo(ISettings settings) : this(settings, new HttpClient())
        {
        }

        public Tiingo(ISettings settings, HttpClient client)
        {
            _apiKey = settings.tiingoApiKey;
            _client = client;
        }

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name => "Tiingo";

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// There's no persistent connection: this is true once Connect() has been called with an API key configured.
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// Connect to the data source.
        /// </summary>
        public void Connect()
        {
            lock (_connectLock)
            {
                if (Connected) return;

                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    //the broker retries every few seconds, only complain once
                    if (!_warnedNoApiKey)
                    {
                        _logger.Warn("No API key configured (Qdms:tiingoApiKey), the source stays disconnected.");
                        _warnedNoApiKey = true;
                    }
                    return;
                }

                _downloader ??= Task.Run(ProcessRequests);
                Connected = true;
            }
        }

        /// <summary>
        /// Disconnect from the data source.
        /// </summary>
        public void Disconnect()
        {
            Connected = false;
        }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            _requests.Writer.TryWrite(request);
        }

        //requests are handled one at a time, which also keeps us away from Tiingo's rate limits
        private async Task ProcessRequests()
        {
            await foreach (var request in _requests.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                List<OHLCBar> data;
                try
                {
                    data = await GetData(request).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    string message = $"Tiingo: failed to download data for {request.Instrument.Symbol}: {ex.Message}";
                    _logger.Error(ex, message);
                    RaiseEvent(Error, this, new ErrorArgs(0, message, request.RequestID));
                    data = new List<OHLCBar>();
                }

                //the request is answered even if it failed, so it doesn't hang around in the broker
                try
                {
                    RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, data));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error while handling data for {request.Instrument.Symbol}");
                }
            }
        }

        private async Task<List<OHLCBar>> GetData(HistoricalDataRequest request)
        {
            var instrument = request.Instrument;
            var symbol = string.IsNullOrEmpty(instrument.DatasourceSymbol) ? instrument.Symbol : instrument.DatasourceSymbol;

            string resampleFreq = FrequencyToResampleFreq(request.Frequency);
            if (resampleFreq == null)
            {
                string error = $"Tiingo: bar size {request.Frequency} is not supported (daily or lower frequencies only). Symbol {instrument.Symbol}.";
                _logger.Error(error);
                RaiseEvent(Error, this, new ErrorArgs(0, error, request.RequestID));
                return new List<OHLCBar>();
            }

            string url = string.Format(CultureInfo.InvariantCulture,
                "{0}{1}/prices?startDate={2:yyyy-MM-dd}&endDate={3:yyyy-MM-dd}&resampleFreq={4}",
                BaseUrl,
                Uri.EscapeDataString(symbol.ToLowerInvariant()),
                request.StartingDate,
                request.EndingDate,
                resampleFreq);

            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _client.SendAsync(message).ConfigureAwait(false);
            string contents = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                //errors come as {"detail":"Error: Ticker 'XYZ' not found"}, rate limits may be plain text
                string error = $"Tiingo: error downloading data for {instrument.Symbol} ({symbol}): {(int)response.StatusCode} {ErrorDetail(contents)}";
                _logger.Error(error);
                RaiseEvent(Error, this, new ErrorArgs((int)response.StatusCode, error, request.RequestID));
                return new List<OHLCBar>();
            }

            List<OHLCBar> data = ParseData(contents, instrument.Symbol);

            _logger.Info($"Downloaded {data.Count} bars from Tiingo, symbol {instrument.Symbol}.");

            return data;
        }

        /// <summary>
        /// Parse the prices JSON into bars with raw prices, dividends and splits, then compute the adjusted prices.
        /// </summary>
        private List<OHLCBar> ParseData(string json, string symbol)
        {
            var rows = JsonConvert.DeserializeObject<List<TiingoPrice>>(json, new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Decimal
            }) ?? new List<TiingoPrice>();

            var data = new List<OHLCBar>(rows.Count);
            foreach (var row in rows)
            {
                //format: 2020-08-31T00:00:00.000Z; the time is always midnight and the date is the exchange-local trading day
                if (row.Date == null || row.Date.Length < 10 ||
                    !DateTime.TryParseExact(row.Date.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    _logger.Warn($"Skipping bar with unparseable date '{row.Date}', symbol {symbol}");
                    continue;
                }

                if (row.Open == 0 || row.High == 0 || row.Low == 0 || row.Close == 0)
                {
                    _logger.Warn($"Skipping bar with zero prices at {date:yyyy-MM-dd}, symbol {symbol}");
                    continue;
                }

                data.Add(new OHLCBar
                {
                    DT = date,
                    Open = row.Open,
                    High = row.High,
                    Low = row.Low,
                    Close = row.Close,
                    Volume = row.Volume.HasValue ? (long)row.Volume.Value : (long?)null,
                    Dividend = row.DivCash != 0 ? row.DivCash : (decimal?)null,
                    Split = row.SplitFactor != 0 && row.SplitFactor != 1 ? row.SplitFactor : (decimal?)null,
                    //in case AdjustData leaves them alone (a single bar)
                    AdjOpen = row.Open,
                    AdjHigh = row.High,
                    AdjLow = row.Low,
                    AdjClose = row.Close
                });
            }

            data = data.OrderBy(x => x.DT).ToList();
            PriceAdjuster.AdjustData(ref data);

            return data;
        }

        /// <summary>
        /// Search Tiingo's tickers (stocks, ETFs, mutual funds) by ticker or name.
        /// </summary>
        public async Task<List<TiingoSearchResult>> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("No Tiingo API key configured (Qdms:tiingoApiKey)");

            string url = "https://api.tiingo.com/tiingo/utilities/search?query=" + Uri.EscapeDataString(query);
            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _client.SendAsync(message).ConfigureAwait(false);
            string contents = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Tiingo search failed: {(int)response.StatusCode} {ErrorDetail(contents)}");

            return JsonConvert.DeserializeObject<List<TiingoSearchResult>>(contents) ?? new List<TiingoSearchResult>();
        }

        /// <summary>
        /// Ticker metadata: name, exchange and the date range of the available prices.
        /// </summary>
        public async Task<TiingoTickerMeta> GetMeta(string ticker)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("No Tiingo API key configured (Qdms:tiingoApiKey)");

            using var message = new HttpRequestMessage(HttpMethod.Get, BaseUrl + Uri.EscapeDataString(ticker.ToLowerInvariant()));
            message.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _client.SendAsync(message).ConfigureAwait(false);
            string contents = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Tiingo metadata request failed: {(int)response.StatusCode} {ErrorDetail(contents)}");

            return JsonConvert.DeserializeObject<TiingoTickerMeta>(contents);
        }

        private static string FrequencyToResampleFreq(BarSize frequency)
        {
            switch (frequency)
            {
                case BarSize.OneDay: return "daily";
                case BarSize.OneWeek: return "weekly";
                case BarSize.OneMonth: return "monthly";
                case BarSize.OneYear: return "annually";
                default: return null;
            }
        }

        private static string ErrorDetail(string contents)
        {
            try
            {
                var error = JsonConvert.DeserializeObject<TiingoError>(contents);
                if (!string.IsNullOrEmpty(error?.Detail)) return error.Detail;
            }
            catch (JsonException)
            {
            }

            return contents.Length > 200 ? contents.Substring(0, 200) : contents;
        }

        ///<summary>
        /// Raise the event in a threadsafe manner
        ///</summary>
        private static void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
        where T : EventArgs
        {
            EventHandler<T> handler = @event;
            handler?.Invoke(sender, e);
        }

        private class TiingoPrice
        {
            [JsonProperty("date")] public string Date { get; set; }
            [JsonProperty("open")] public decimal Open { get; set; }
            [JsonProperty("high")] public decimal High { get; set; }
            [JsonProperty("low")] public decimal Low { get; set; }
            [JsonProperty("close")] public decimal Close { get; set; }
            [JsonProperty("volume")] public decimal? Volume { get; set; }
            [JsonProperty("divCash")] public decimal DivCash { get; set; }
            [JsonProperty("splitFactor")] public decimal SplitFactor { get; set; }
        }

        public class TiingoSearchResult
        {
            [JsonProperty("ticker")] public string Ticker { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("assetType")] public string AssetType { get; set; }
            [JsonProperty("isActive")] public bool IsActive { get; set; }
            [JsonProperty("countryCode")] public string CountryCode { get; set; }
            [JsonProperty("permaTicker")] public string PermaTicker { get; set; }
        }

        public class TiingoTickerMeta
        {
            [JsonProperty("ticker")] public string Ticker { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("exchangeCode")] public string ExchangeCode { get; set; }
            [JsonProperty("startDate")] public string StartDate { get; set; }
            [JsonProperty("endDate")] public string EndDate { get; set; }
            [JsonProperty("description")] public string Description { get; set; }
        }

        private class TiingoError
        {
            [JsonProperty("detail")] public string Detail { get; set; }
        }
    }
}

#pragma warning restore 67
