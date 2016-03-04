// -----------------------------------------------------------------------
// <copyright file="ForexFeed.cs" company="">
// Copyright 2016 Leonhard Schick (leonhard.schick@gmail.com)
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using forexfeed.net;
using QDMS;
using NLog;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Collections;
using System.Globalization;
using QDMS.Server.DataSources.ForexFeed;

namespace QDMSServer.DataSources
{
    /// <summary>
    /// 
    /// 
    /// see:
    /// http://forexfeed.net/developer/dot-net-forex-data-feed-api
    /// http://forexfeed.net/developer/forex-api-docs
    /// </summary>
    public class ForexFeed : IRealTimeDataSource, IDisposable
    {
        public enum PriceType
        {
            Bid,
            Mid,
            Ask
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private feedapi _api;
        private ConcurrentDictionary<BarSize, ConcurrentDictionary<int, RealTimeDataRequest>> _connectedRequests = new ConcurrentDictionary<BarSize, ConcurrentDictionary<int, RealTimeDataRequest>>();
        private ConcurrentDictionary<BarSize, FeedTimer> _timers = new ConcurrentDictionary<BarSize, FeedTimer>();
        private int _requestIDs;
        private Dictionary<BarSize, int> _intervalCache = new Dictionary<BarSize, int>();
        
        public bool Connected { get; private set; }
        public string Name => "ForexFeed";

        void StartAllPossibleTimers()
        {
            lock(_timers)
            {
                FeedTimer timer;
                var barSizes = Enum.GetValues(typeof(BarSize));
                foreach (var barSize in barSizes)
                {
                    ConcurrentDictionary<int, RealTimeDataRequest> requrestDict;
                    if (_connectedRequests.TryGetValue(((BarSize)barSize), out requrestDict))
                        if (requrestDict.Count > 0)
                            if (_timers.TryGetValue((BarSize)barSize, out timer))
                                if (!timer.Active)
                                    timer.Start();
                }
            }
        }

        void StopAllTimers()
        {
            lock(_timers)
            {
                FeedTimer timer;
                var barSizes = Enum.GetValues(typeof(BarSize));
                foreach (var barSize in barSizes)
                {
                    if (_timers.TryGetValue(((BarSize)barSize), out timer))
                        if (timer.Active)
                            timer.Stop();
                }
            }
        }

        public void Dispose()
        {
            StopAllTimers();
            _timers.Clear();
        }
        
        static double GetTimerInterval(BarSize barSize)
        {
            DateTime now = DateTime.Now;

            switch (barSize)
            {
                case BarSize.OneSecond:
                    return 1000
                        - now.Millisecond;
                case BarSize.OneMinute:
                    return (60 - now.Second) * 1000
                        - now.Millisecond;
                case BarSize.OneHour:
                    return (60 - now.Minute) * 60 * 1000
                        - (60 - now.Second) * 1000
                        - now.Millisecond;
                case BarSize.OneDay:
                    return (24 - now.Hour) * 24 * 60 * 1000
                        - (60 - now.Minute) * 60 * 1000
                        - (60 - now.Second) * 1000
                        - now.Millisecond;
                default:
                    throw new NotImplementedException($"BarSize {barSize} is currently not supported by the ForexFeed data source");
            }
        }

        public ForexFeed()
        {
            var barSizes = Enum.GetValues(typeof(BarSize));//.Where(b => b <= BarSize.OneDay); ... linq missin
            foreach (var barSize in barSizes)
            {
                if (((BarSize)barSize) > BarSize.OneDay)
                    break;

                var timer = new FeedTimer((BarSize)barSize);
                timer.Elapsed += TimerElapsed;
                
                _timers.TryAdd(((BarSize)barSize), timer);
            }
        }

        public ForexFeed(string accessKey, PriceType price)
            : this()
        {
            if (!string.IsNullOrEmpty(accessKey))
                Initialize(accessKey, price);
        }

        ~ForexFeed()
        {
            if (_timers.Count != 0 && Connected)
            {
                Disconnect();
                Dispose();
            }
        }

        public void Connect()
        {
            if(_api == null)
            {
                _logger.Log(LogLevel.Error, $"Can't connect to {Name} - access key is missing.");
                return;
            }

            ReloadAvailableIntervalCache();
            StartAllPossibleTimers();

            Connected = true;
        }

        public void Disconnect()
        {
            Connected = false;
            
            StopAllTimers();
        }

        private void LogError(string function)
        {
            _logger.Log(LogLevel.Error,
            $"ForexFeed {function}: {_api.getStatus()} : {_api.getErrorCode()} : {_api.getErrorMessage()}"
            );
        }

        public int RequestRealTimeData(RealTimeDataRequest request)
        {
            if (!Connected)
                throw new Exception("The datasource is not connected. A request is not possible.");

            if (!request.Instrument.ID.HasValue)
                throw new Exception("ID doesn't have value.");
            if (!_intervalCache.ContainsKey(request.Frequency) || request.Frequency > BarSize.OneDay)
                throw new Exception("Bar size is not supported");

            string symbol = request.Instrument.DatasourceSymbol;
            if (string.IsNullOrEmpty(symbol))
                symbol = request.Instrument.Symbol;

            var availableSymbols = GetAvailableSymbols();
            if (!availableSymbols.Contains(symbol))
                throw new Exception("Symbol is not available");

            _requestIDs++;

            var requestList = _connectedRequests.GetOrAdd(request.Frequency, new ConcurrentDictionary<int, RealTimeDataRequest>());
            bool success = requestList.TryAdd(_requestIDs, request);

            FeedTimer timer;
            bool result = _timers.TryGetValue(request.Frequency, out timer);
            if (!result)
                throw new NotSupportedException($"Timer not found for freq. {request.Frequency}");

            // start timer if not is already started:
            if (!timer.Active)
                timer.Start();

            return _requestIDs;
        }

        public void CancelRealTimeData(int requestID)
        {
            RealTimeDataRequest request = null;

            foreach (var key1Request in _connectedRequests)
            {
                if (key1Request.Value.TryGetValue(requestID, out request))
                    break;
            }

            if (request == null)
                // kein Request gefunden ...
                return;

            RealTimeDataRequest dummy;
            FeedTimer timer;
            ConcurrentDictionary<int, RealTimeDataRequest> dummy2;
            if (_connectedRequests[request.Frequency].TryRemove(requestID, out dummy))
                if (_connectedRequests[request.Frequency].Count == 0)
                    if (_connectedRequests.TryRemove(request.Frequency, out dummy2))
                        if (_connectedRequests.Count == 0)
                            if (_timers.TryGetValue(request.Frequency, out timer))
                                if (timer.Active)
                                    timer.Stop();
        }
        
        public event EventHandler<RealTimeDataEventArgs> DataReceived;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
        public event EventHandler<ErrorArgs> Error;
        public event PropertyChangedEventHandler PropertyChanged;

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private void TimerElapsed(object sender, BarSize e)
        {
            if (!Connected)
                return;

            foreach (KeyValuePair<BarSize, ConcurrentDictionary<int, RealTimeDataRequest>> pair in _connectedRequests)
            {
                if (_connectedRequests.Count == 0)
                    continue;
                
                List<string> symbols = new List<string>();

                List<RealTimeDataRequest> requestList = pair.Value.Select(p => p.Value).ToList();
                {
                    List<RealTimeDataRequest> requestsToIgnore = new List<RealTimeDataRequest>();
                    foreach (var request in requestList)
                    {
                        //if needed, we filter out the data outside of regular trading hours
                        if (request.RTHOnly &&
                            request.Frequency < BarSize.OneDay &&
                            request.Instrument.Sessions != null)
                        {
                            if (!DateTime.UtcNow.InSession(request.Instrument.Sessions))
                                requestsToIgnore.Add(request);
                        }
                    }
                    requestList.RemoveAll(x => requestsToIgnore.Contains(x));
                }

                // @ToDo: Feacture: Diese Abfrage könnte gecachet werden, um Performance zu spaaren
                foreach (RealTimeDataRequest request in requestList)
                {
                    string symbol = request.Instrument.DatasourceSymbol;
                    if (string.IsNullOrEmpty(symbol))
                        symbol = request.Instrument.Symbol;

                    if (!symbols.Contains(symbol))
                        symbols.Add(symbol);
                }

                if (symbols.Count > 0)
                {
                    Dictionary<string, Hashtable> data;
                    if (RequestNewData(pair.Key, symbols, out data))
                    {
                        foreach (var request in requestList)
                        {
                            string symbol = request.Instrument.DatasourceSymbol;
                            if (string.IsNullOrEmpty(symbol))
                                symbol = request.Instrument.Symbol;

                            RaiseEvent(DataReceived, this, new RealTimeDataEventArgs(
                                request.Instrument.ID.Value,
                                long.Parse((string)data[symbol]["time"], CultureInfo.InvariantCulture),
                                decimal.Parse((string)data[symbol]["open"], CultureInfo.InvariantCulture),
                                decimal.Parse((string)data[symbol]["high"], CultureInfo.InvariantCulture),
                                decimal.Parse((string)data[symbol]["low"], CultureInfo.InvariantCulture),
                                decimal.Parse((string)data[symbol]["close"], CultureInfo.InvariantCulture),
                                0,
                                0,
                                0,
                                request.AssignedID
                                ));
                        }
                    }
                }
            }
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
        
        #region API Helper Functions

        public void Initialize(string accessKey, PriceType price)
        {
            _api = new feedapi(accessKey);

            switch (price)
            {
                case PriceType.Bid:
                    _api.setPrice("bid");
                    break;
                case PriceType.Mid:
                    _api.setPrice("mid");
                    break;
                case PriceType.Ask:
                    _api.setPrice("ask");
                    break;
            }
        }

        private void ReloadAvailableIntervalCache()
        {
            lock (_intervalCache)
            {
                Hashtable intervals = _api.getAvailableIntervals(false);
                if (_api.getStatus().Equals("OK"))
                {
                    _intervalCache.Clear();

                    ICollection c = intervals.Values;
                    foreach (var current in c)
                    {
                        Hashtable value = (Hashtable)current;

                        int interval = int.Parse((string)value["interval"], CultureInfo.InvariantCulture);
                        BarSize? barSize = ForexFeedIntervalToBarSize(interval);
                        if (barSize.HasValue)
                        {
                            _intervalCache.Add(barSize.Value, interval);
                        }
                    }
                }
                else
                    LogError("getAvailableIntervals");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interval">
        /// Interval in secounds
        /// </param>
        /// <returns></returns>
        private BarSize? ForexFeedIntervalToBarSize(int interval)
        {
            switch (interval)
            {
                case 1:
                    return BarSize.OneSecond;
                case 5:
                    return BarSize.FiveSeconds;
                case 15:
                    return BarSize.FifteenSeconds;
                case 30:
                    return BarSize.ThirtySeconds;
                case 1 * 60:
                    return BarSize.OneMinute;
                case 5 * 60:
                    return BarSize.FiveMinutes;
                case 15 * 60:
                    return BarSize.FifteenMinutes;
                case 30 * 60:
                    return BarSize.ThirtyMinutes;
                case 1 * 60 * 60:
                    return BarSize.OneHour;
                case 24 * 60 * 60:
                    return BarSize.OneDay;
                /*case "weekly":
                    return BarSize.OneWeek;
                case "monthly":
                    return BarSize.OneMonth;
                case "yearly":
                    return BarSize.OneYear;
                case "10s":
                case "10m":
                case "45s":
                case "20m":
                case "45m":
                case "2h":
                case "3h":
                case "4h":
                case "6h":
                case "12h":*/
                default:
                    return null;
            }
        }

        private List<string> GetAvailableSymbols()
        {
            List<string> returnList = new List<string>();

            Hashtable symbols = _api.getAvailableSymbols(false);
            if (_api.getStatus().Equals("OK"))
            {
                ICollection c = symbols.Values;
                foreach (var current in c)
                {
                    Hashtable value = (Hashtable)current;

                    returnList.Add((string)value["symbol"]);
                }
            }
            else
                LogError("getAvailableSymbols");

            return returnList;
        }

        private bool RequestNewData(BarSize barSize, List<string> symbols, out Dictionary<string, Hashtable> data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var symbol in symbols)
            {
                if (sb.Length > 0)
                    sb.Append(",");

                sb.Append(symbol);
            }

            _api.setTimeout(30);
            _api.setSymbol(sb.ToString());
            _api.setInterval(_intervalCache[barSize]);
            _api.setPeriods(1);
            ArrayList quotes = _api.getData();

            if (_api.getStatus().Equals("OK"))
            {
                Dictionary<string, Hashtable> locData = new Dictionary<string, Hashtable>();
                
                foreach (var quoteTmp in quotes)
                {
                    Hashtable quote = (Hashtable)quoteTmp;
                    locData.Add((string)quote["symbol"], quote);
                }

                data = locData;
                return true;
            }

            LogError("getData");
            data = null;
            return false;
        }

        #endregion
    }
}
