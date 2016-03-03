using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using NLog;

namespace QDMS.Server.DataSources
{
    /// <summary>
    /// A real time source that simulates data from a
    /// historical source.
    /// </summary>
    public class RealTimeFromHistoricalDataSource : IRealTimeDataSource, IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private class ShortTimerController
        {
            public SimulatedTimerAsync Caller { get; set; }
            public IAsyncResult AsyncResult { get; set; }
            public bool Active { get; set; }
            public int Interval { get; set; }
        }

        private readonly ConcurrentDictionary<BarSize, Timer> _timers = new ConcurrentDictionary<BarSize, Timer>();
        private readonly ConcurrentDictionary<BarSize, ShortTimerController> _shortTimers = new ConcurrentDictionary<BarSize, ShortTimerController>(); 
        private readonly ConcurrentDictionary<RequestGrouping, ConcurrentDictionary<int, RealTimeDataRequest>> _connectedRequests = new ConcurrentDictionary<RequestGrouping, ConcurrentDictionary<int, RealTimeDataRequest>>();
        private readonly ConcurrentDictionary<RequestGrouping, int> _historicalRequests = new ConcurrentDictionary<RequestGrouping, int>();  
        private readonly ConcurrentDictionary<int, List<OHLCBar>> _historicalData = new ConcurrentDictionary<int, List<OHLCBar>>();

        /// <summary>
        /// Represents the position for each request in the historical data.
        /// 
        /// For each request, we start at historical bar 1.
        /// </summary>
        private readonly ConcurrentDictionary<int, int> _requestPosition = new ConcurrentDictionary<int, int>();

        private int _requestIDs;
        private readonly IHistoricalDataSource _historicalDataSource;
        private readonly DataLocation _dataLocation;
        private readonly bool _rthOnly;
        private readonly bool _saveDataToStorage;
        private readonly DateTime _startingDate;
        private readonly DateTime _endingDate;
        private int _historicalRequestIDs;

        private readonly object _historicalDataRequestLock = new object();

        public string Name { get; }
        public bool Connected { get; private set; }

        void StartAllPossibleTimers()
        {
            Timer timer;
            ShortTimerController controller;
            foreach (var requrestDict in _connectedRequests)
            {
                if (requrestDict.Value.Count > 0)
                {
                    if (_timers.TryGetValue(requrestDict.Key.Frequency, out timer))
                        if (!timer.Enabled)
                            timer.Start();

                    if (_shortTimers.TryGetValue(requrestDict.Key.Frequency, out controller))
                        if (!controller.AsyncResult.IsCompleted)
                        {
                            controller.Active = false;
                            controller.AsyncResult.AsyncWaitHandle.WaitOne();
                        }
                }
            }
        }
        void StopAllTimers()
        {
            Timer timer;
            ShortTimerController controller;
            var barSizes = Enum.GetValues(typeof(BarSize)).Cast<BarSize>();
            foreach (var barSize in barSizes)
            {
                if (_timers.TryGetValue(barSize, out timer))
                    if (timer.Enabled)
                        timer.Stop();

                if (_shortTimers.TryGetValue(barSize, out controller))
                    if (controller.AsyncResult != null && !controller.AsyncResult.IsCompleted)
                    {
                        controller.Active = false;
                        controller.AsyncResult.AsyncWaitHandle.WaitOne();
                    }
            }
        }

        public void Dispose()
        {
            lock (_timers)
            {
                StopAllTimers();
                _timers.Clear();
            }
        }

        public RealTimeFromHistoricalDataSource(IHistoricalDataSource historicalDataSource,
            DataLocation dataLocation, bool rthOnly, bool saveDataToStorage,
            DateTime startingDate, DateTime endingDate, double? overrideTimerInterval = null)
        {
            Name = "Simulate real time from " + historicalDataSource.Name;
            _dataLocation = dataLocation;
            _rthOnly = rthOnly;
            _saveDataToStorage = saveDataToStorage;
            _startingDate = startingDate;
            _endingDate = endingDate;
            _historicalDataSource = historicalDataSource;
            _historicalDataSource.Error += (sender, args) =>
            {
                _logger.Log(LogLevel.Error,
                    $"Error while requesting historical data for real time simulation: {args.ErrorCode} {args.ErrorMessage}");
            };
            _historicalDataSource.HistoricalDataArrived += (sender, args) =>
            {
                _historicalData.TryAdd(args.Request.RequestID, args.Data);
            };

            var barSizes = Enum.GetValues(typeof(BarSize)).Cast<BarSize>();
            SimulatedTimerAsync caller = new SimulatedTimerAsync(SimulatedTimer);
            foreach (var barSize in barSizes)
            {
                if (barSize > BarSize.OneDay)
                    break;
                
                double interval = overrideTimerInterval ?? barSize.ToTimeSpan().TotalMilliseconds;

                // The system is not able to simulate a timer with less than ca. 900 ms (dependes on the system).
                // It will be moustly higher than the expected waiting time. So we simulate the timer as a loop here.
                if (interval < 1000)
                {
                    ShortTimerController controller = new ShortTimerController { Active = false, Interval = (int)interval, Caller = caller };
                    _shortTimers.TryAdd(barSize, controller);
                }
                else
                {
                    var timer = new Timer(interval);
                    timer.Elapsed += TimerElapsed;

                    _timers.TryAdd(barSize, timer);
                }
            }
        }

        ~RealTimeFromHistoricalDataSource()
        {
            if (_timers.Count != 0 && Connected)
            {
                Disconnect();
                Dispose();
            }
        }
        
        public void Connect()
        {
            StartAllPossibleTimers();

            Connected = true;
        }

        public void Disconnect()
        {
            Connected = false;

            StopAllTimers();
        }

        public int RequestRealTimeData(RealTimeDataRequest request)
        {
            if (!request.Instrument.ID.HasValue) throw new Exception("ID doesn't have value.");

            _requestIDs++;

            var requestGrouping = new RequestGrouping
            {
                Instrument = request.Instrument,
                Frequency = request.Frequency
            };

            var requestList = _connectedRequests.GetOrAdd(requestGrouping,
                new ConcurrentDictionary<int, RealTimeDataRequest>());
            bool success = requestList.TryAdd(_requestIDs, request);

            lock (_historicalDataRequestLock)
            {
                int historicalRequestId;
                if (!_historicalRequests.TryGetValue(requestGrouping, out historicalRequestId))
                {
                    var asyncCall = new RequestHistoricalDataCaller(RequestHistoricalData);
                    asyncCall.BeginInvoke(requestGrouping, null, null);
                }
            }
            
            Timer timer;
            bool result = _timers.TryGetValue(request.Frequency, out timer);
            if (result)
            {
                // start timer if not is already started:
                if (!timer.Enabled)
                    timer.Start();
            }

            ShortTimerController controller;
            result = _shortTimers.TryGetValue(request.Frequency, out controller);
            if(result)
            {
                if(!controller.Active)
                {
                    controller.Active = true;
                    // start simulatetimer
                    controller.AsyncResult = controller.Caller.BeginInvoke(controller, request.Frequency, null, null);
                }
            }

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

            var requestGrouping = new RequestGrouping
            {
                Instrument = request.Instrument,
                Frequency = request.Frequency
            };

            RealTimeDataRequest dummy;
            Timer timer;
            ConcurrentDictionary<int, RealTimeDataRequest> dummy2;
            if (_connectedRequests[requestGrouping].TryRemove(requestID, out dummy))
                if (_connectedRequests[requestGrouping].Count == 0)
                    if (_connectedRequests.TryRemove(requestGrouping, out dummy2))
                        if (_connectedRequests.Count == 0)
                            if (_timers.TryGetValue(request.Frequency, out timer))
                                if (timer.Enabled)
                                    timer.Stop();
        }

        public event EventHandler<RealTimeDataEventArgs> DataReceived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
        public event PropertyChangedEventHandler PropertyChanged;
        
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            var entry = _timers.First(p => p.Value == sender);
            
            SendData(entry.Key);
        }

        private void SimulatedTimer(ShortTimerController controller, BarSize barSize)
        {
            while (controller.Active)
            {
                SendData(barSize);
                System.Threading.Thread.Sleep(controller.Interval);
            }
        }
        private delegate void SimulatedTimerAsync(ShortTimerController controller, BarSize barSize);

        void SendData(BarSize barSize)
        {
            foreach (KeyValuePair<RequestGrouping, ConcurrentDictionary<int, RealTimeDataRequest>> pair in _connectedRequests)
            {
                // @ToDo: Performance: if many entries exists in this list, this could be an performance issue.
                if (pair.Key.Frequency != barSize)
                    continue;
                if (_connectedRequests.Count == 0)
                    continue;

                int historicalRequestId;
                if (!_historicalRequests.TryGetValue(pair.Key, out historicalRequestId))
                    continue; // the historical request is not made yet.

                List<OHLCBar> historicalData;
                if (!_historicalData.TryGetValue(historicalRequestId, out historicalData))
                    continue; // the historical data did not arrived yet.

                List<int> requestsToDelete = new List<int>();

                foreach (var pair2 in pair.Value)
                {
                    // get the current position
                    int indexPosition = 0;
                    if (_requestPosition.TryGetValue(pair2.Key, out indexPosition))
                        indexPosition++;

                    if (historicalData.Count <= indexPosition)
                    {
                        // end of historical data
                        _logger.Log(LogLevel.Info, $"End of historical data for real time simulation - request ID: {pair2.Key}");

                        requestsToDelete.Add(pair2.Key);
                    }
                    else
                    {
                        var currentBar = historicalData[indexPosition];

                        RaiseEvent(DataReceived, this, new RealTimeDataEventArgs(
                            pair2.Value.Instrument.ID.Value,
                            MyUtils.ConvertToTimestamp(currentBar.DT),
                            currentBar.Open,
                            currentBar.High,
                            currentBar.Low,
                            currentBar.Close,
                            currentBar.Volume ?? 0,
                            0,
                            0,
                            pair2.Value.AssignedID
                        ));

                        if (indexPosition == 0)
                            _requestPosition.TryAdd(pair2.Key, indexPosition);
                        else
                            _requestPosition[pair2.Key] = indexPosition;
                    }
                }

                RealTimeDataRequest dummy;
                foreach (var i in requestsToDelete)
                {
                    pair.Value.TryRemove(i, out dummy);
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

        #region Historical Data Request Management

        private void RequestHistoricalData(RequestGrouping request)
        {
            lock (_historicalDataRequestLock)
            {
                _historicalRequestIDs++;

                if (_historicalRequests.TryAdd(request, _historicalRequestIDs))
                {
                    _historicalDataSource.RequestHistoricalData(new HistoricalDataRequest
                    {
                        Instrument = request.Instrument,
                        Frequency = request.Frequency,

                        DataLocation = _dataLocation,
                        StartingDate = _startingDate,
                        EndingDate = _endingDate,
                        RTHOnly = _rthOnly,
                        SaveDataToStorage = _saveDataToStorage,

                        RequestID = _historicalRequestIDs
                    });
                }
            }
        }

        private delegate void RequestHistoricalDataCaller(RequestGrouping request);

        #endregion

        private struct RequestGrouping : IEquatable<RequestGrouping>
        {
            public Instrument Instrument;
            public BarSize Frequency;
            
            public bool Equals(RequestGrouping other)
            {
                return Instrument == other.Instrument &&
                       Frequency == other.Frequency;
            }
        }
    }
}
