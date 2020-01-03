// -----------------------------------------------------------------------
// <copyright file="Binance.cs" company="">
// Copyright 2018 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using QDMS.Server.DataSources.Binance.Model;
using WebSocket4Net;
//TODO eventually split off to its own QDMS-BinanceClient package that covers the entire API
//API documentation: https://www.binance.com/restapipub.html
namespace QDMS.Server.DataSources.Binance
{
    public class Binance : IHistoricalDataSource, IRealTimeDataSource
    {
        /// <summary>
        /// Key = request id
        /// </summary>
        private Dictionary<int, WebSocket> _sockets = new Dictionary<int, WebSocket>();
        private readonly Thread _downloaderThread;
        private readonly ConcurrentQueue<HistoricalDataRequest> _queuedRequests = new ConcurrentQueue<HistoricalDataRequest>();
        private bool _runDownloader;
        private HttpClient _httpClient;
        private int _requestCounter;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Key: assignedId, value: internal id
        /// </summary>
        private Dictionary<int, int> _requestIdMap = new Dictionary<int, int>();

        public Binance()
        {
            _downloaderThread = new Thread(DownloaderLoop);
        }

        private async void DownloaderLoop()
        {
            while (_runDownloader)
            {
                while (_queuedRequests.TryDequeue(out HistoricalDataRequest req))
                {
                    try
                    {
                        RaiseEvent(HistoricalDataArrived, this,
                            new HistoricalDataEventArgs(req, await ProcessHistoricalRequest(req)));
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error downloading historical data from binance");
                        RaiseEvent(Error, this,
                            new ErrorArgs(0, "Error downloading historical data from binance: " + ex.Message));
                    }
                }
                Thread.Sleep(15);
            }
        }
        //todo test requesting wrong freq, both hist and rt
        private async Task<List<OHLCBar>> ProcessHistoricalRequest(HistoricalDataRequest req)
        {
            //there's a limit to the number of bars that can be returned per-request, so here we split it up
            //then combine the results
            List<OHLCBar> data = new List<OHLCBar>();
            try
            {
                foreach (var subReq in SplitRequest(req))
                {
                    data.AddRange(await GetData(subReq));
                }
            }
            catch (ArgumentException ex)
            {
                _logger.Error(ex, "Binance historical data request error");
                RaiseEvent(Error, this, new ErrorArgs(-1, ex.Message));
                return new List<OHLCBar>();
            }


            data = data.Distinct((x, y) => x.DTOpen == y.DTOpen).ToList();
            return data;
        }
        //TODO historical tick data! aggTrades
        private async Task<List<OHLCBar>> GetData(HistoricalDataRequest req)
        {
            string symbol = string.IsNullOrEmpty(req.Instrument.DatasourceSymbol)
                ? req.Instrument.Symbol
                : req.Instrument.DatasourceSymbol;
            string interval = BarSizetoInterval(req.Frequency);
            long startTime = MyUtils.ConvertToMillisecondTimestamp(DateTime.SpecifyKind(req.StartingDate, DateTimeKind.Utc));
            long endTime = MyUtils.ConvertToMillisecondTimestamp(DateTime.SpecifyKind(req.EndingDate, DateTimeKind.Utc));
            string url = $"https://www.binance.com/api/v1/klines?symbol={symbol}&interval={interval}&startTime={startTime}&endTime={endTime}";
            _logger.Info("Binance filling historical req from URL: " + url);

            var result = await _httpClient.GetAsync(url);
            result.EnsureSuccessStatusCode();

            string contents = await result.Content.ReadAsStringAsync();
            return ParseHistoricalData(JArray.Parse(contents));
        }


        private List<OHLCBar> ParseHistoricalData(JArray data)
        {
            var result = new List<OHLCBar>();

            foreach (JToken item in data.ToArray())
            {
                result.Add(new OHLCBar()
                {
                    DTOpen = MyUtils.TimestampToDateTimeByMillisecond(long.Parse(item[0].ToString())),
                    Open = decimal.Parse(item[1].ToString()),
                    High = decimal.Parse(item[2].ToString()),
                    Low = decimal.Parse(item[3].ToString()),
                    Close = decimal.Parse(item[4].ToString()),
                    Volume = (long)decimal.Parse(item[5].ToString()),
                    DT = MyUtils.TimestampToDateTimeByMillisecond(long.Parse(item[6].ToString()))
                    //QuoteAssetVolume = decimal.Parse(item[7].ToString()),
                    //NoOfTrades = int.Parse(item[8].ToString()),
                    //TakerBuyBaseAssetVolume = decimal.Parse(item[9].ToString()),
                    //TakerBuyQuoteAssetVolume = decimal.Parse(item[10].ToString())
                });
            }

            return result;
        }

        private string BarSizetoInterval(BarSize barSize)
        {
            //Available intervals: 1m, 3m, 5m, 15m, 30m, 1h, 2h, 4h, 6h, 8h, 12h, 1d, 3d, 1w, 1M
            switch (barSize)
            {
                case BarSize.OneMinute:
                    return "1m";
                case BarSize.FiveMinutes:
                    return "5m";
                case BarSize.FifteenMinutes:
                    return "15m";
                case BarSize.ThirtyMinutes:
                    return "30m";
                case BarSize.OneHour:
                    return "1h";
                case BarSize.OneDay:
                    return "1d";
                case BarSize.OneWeek:
                    return "1w";
                case BarSize.OneMonth:
                return "1M";


                default:
                    throw new ArgumentException("Unsupported barsize: " + barSize);
            }
        }

        /// <summary>
        /// Splits a historical data request into multiple pieces so that they obey the request limits
        /// </summary>
        private List<HistoricalDataRequest> SplitRequest(HistoricalDataRequest request)
        {
            var requests = new List<HistoricalDataRequest>();

            //start at the end, and work backward in increments
            int step = 495; //max bars returned per request
            var freqInterval = request.Frequency.ToTimeSpan().TotalSeconds * step;
            DateTime currentDate = request.EndingDate;
            while (currentDate > request.StartingDate)
            {
                var newReq = (HistoricalDataRequest)request.Clone();
                newReq.EndingDate = currentDate;
                newReq.StartingDate = newReq.EndingDate.AddSeconds(-freqInterval);
                if (newReq.StartingDate < request.StartingDate)
                    newReq.StartingDate = request.StartingDate;

                currentDate = newReq.StartingDate;
                requests.Add(newReq);
            }

            requests.Reverse();
            return requests;
        }

        public void Connect()
        {
            _httpClient = new HttpClient();
            _runDownloader = true;
            _downloaderThread.Start();
        }

        public void Disconnect()
        {
            foreach (var socket in _sockets)
            {
                socket.Value.Close();
            }

            _runDownloader = false;
            _downloaderThread.Join();
            _httpClient?.Dispose();
            _httpClient = null;
        }

        public void RequestRealTimeData(RealTimeDataRequest request)
        {
            int id = _requestCounter++;
            _requestIdMap.Add(request.AssignedID, id);

            if (request.Frequency == BarSize.Tick)
            {
                string symbol = GetSymbol(request).ToLower();
                string url = $"wss://stream.binance.com:9443/ws/{symbol}@aggTrade";
                var tickSocket = new WebSocket(url);
                tickSocket.MessageReceived += (s, e) => RealTimeTick(request.Instrument.ID.Value, request.AssignedID, e);
                _sockets.Add(id, tickSocket);
                tickSocket.Open();
            }
            else
            {
                string symbol = GetSymbol(request).ToLower();
                string interval = BarSizetoInterval(request.Frequency);
                string url = $"wss://stream.binance.com:9443/ws/{symbol}@kline_{interval}";
                var barSocket = new WebSocket(url);
                barSocket.MessageReceived += (s,e) => RealTimeBar(request.Instrument.ID.Value, request.AssignedID, e);
                _sockets.Add(id, barSocket);
                barSocket.Open();
            }
        }

        private void RealTimeTick(int instrumentId, int reqId, MessageReceivedEventArgs e)
        {
            var aggTrade = JsonConvert.DeserializeObject<AggTrade>(e.Message);
            var eventArgs = aggTrade.ToTickEventArgs(instrumentId);

            RaiseEvent(TickReceived, this, eventArgs);
        }

        private void RealTimeBar(int instrumentId, int reqId, MessageReceivedEventArgs e)
        {
            var kline = JsonConvert.DeserializeObject<Kline>(e.Message);
            RaiseEvent(DataReceived, this,
                new RealTimeDataEventArgs(instrumentId, 
                    kline.Bar.StartTime, 
                    kline.Bar.Open, 
                    kline.Bar.High, 
                    kline.Bar.Low,
                    kline.Bar.Close, 
                    (long)kline.Bar.Volume, 
                    0, 
                    (int)kline.Bar.NumberOfTrades, 
                    reqId));
        }

        private static string GetSymbol(RealTimeDataRequest request)
        {
            return string.IsNullOrEmpty(request.Instrument.DatasourceSymbol)
                ? request.Instrument.Symbol
                : request.Instrument.DatasourceSymbol;
        }

        public void CancelRealTimeData(int requestId)
        {
            if (_requestIdMap.TryGetValue(requestId, out int socketId))
            {
                if (_sockets.ContainsKey(socketId))
                {
                    _sockets[socketId].Close();
                    _sockets[socketId].Dispose();
                    _sockets.Remove(socketId);
                }
            }
            else
            {
                RaiseEvent(Error, this, new ErrorArgs(-1, "Real time stream requested for cancelation not found. ID: " + requestId));
            }
        }

        public string Name => "Binance";

        public bool Connected => _runDownloader;

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
        private static void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
            where T : EventArgs
        {
            EventHandler<T> handler = @event;
            handler?.Invoke(sender, e);
        }

        public event EventHandler<RealTimeDataEventArgs> DataReceived;
        public event EventHandler<TickEventArgs> TickReceived;
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
        public event PropertyChangedEventHandler PropertyChanged;
    }
}