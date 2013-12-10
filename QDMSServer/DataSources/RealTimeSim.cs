// -----------------------------------------------------------------------
// <copyright file="RealTimeSim.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Timers;
using QDMS;

#pragma warning disable 67
namespace QDMSServer.DataSources
{
    class RealTimeSim : IRealTimeDataSource, IDisposable
    {
        private Timer _timer;
        private BlockingCollection<string> _requestedSymbols;
        private ConcurrentDictionary<string, int> _loopsPassed;
        private ConcurrentDictionary<string, int> _loopLimit;
        private Random _rand;
        private int _requestIDs = 0;

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
            if (_requestedSymbols != null)
            {
                _requestedSymbols.Dispose();
                _requestedSymbols = null;
            }
        }
        
        public RealTimeSim()
        {
            Name = "SIM";
            _requestedSymbols = new BlockingCollection<string>();
            _loopsPassed = new ConcurrentDictionary<string, int>();
            _loopLimit = new ConcurrentDictionary<string, int>();

            _timer = new Timer(100);
            _timer.Elapsed += SimulateData;

            _rand = new Random();
        }

        public void Connect()
        {
            Connected = true;
            _timer.Start();
        }

        public void Disconnect()
        {
            Connected = false;
            _timer.Stop();
        }

        public int RequestRealTimeData(RealTimeDataRequest request)
        {
            _requestedSymbols.TryAdd(request.Instrument.Symbol, 1000);
            if(request.Frequency == BarSize.Tick)
                _loopLimit.TryAdd(request.Instrument.Symbol, 1);
            else
                _loopLimit.TryAdd(request.Instrument.Symbol, (int) (request.Frequency.ToTimeSpan().TotalMilliseconds / 100));

            _loopsPassed.TryAdd(request.Instrument.Symbol, _loopLimit[request.Instrument.Symbol]);
            return ++_requestIDs;
        }

        public void CancelRealTimeData(int requestID)
        {
            throw new NotImplementedException();
        }

        private void SimulateData(object sender, ElapsedEventArgs e)
        {
            foreach (string s in _requestedSymbols)
            {
                _loopsPassed[s]++;
                if (_loopsPassed[s] < _loopLimit[s]) continue;
                _loopsPassed[s] = 0;

                double open = _rand.NextDouble() * 50 + 20;
                double high = open + _rand.NextDouble();
                double low = open - _rand.NextDouble();
                double close = (high - low) * _rand.NextDouble();
                RaiseEvent(DataReceived, this, new RealTimeDataEventArgs(
                    s, 
                    MyUtils.ConvertToTimestamp(DateTime.Now), 
                    (decimal) open,
                    (decimal) high,
                    (decimal) low,
                    (decimal) close,
                    1000,
                    0,
                    0));
            }
        }


        public string Name { get; private set; }

        public event EventHandler<RealTimeDataEventArgs> DataReceived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
        public bool Connected { get; private set; }

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
    }
}
#pragma warning restore 67