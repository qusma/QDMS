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
    public class RealTimeSim : IRealTimeDataSource, IDisposable
    {
        private Timer _timer;
        private ConcurrentDictionary<int, string> _requestedSymbols;
        private ConcurrentDictionary<string, int> _loopsPassed;
        private ConcurrentDictionary<string, int> _loopLimit;
        private ConcurrentDictionary<string, int> _idMap;
        private Random _rand;
        private int _requestIDs;

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
        
        public RealTimeSim()
        {
            Name = "SIM";
            _requestedSymbols = new ConcurrentDictionary<int, string>();
            _loopsPassed = new ConcurrentDictionary<string, int>();
            _loopLimit = new ConcurrentDictionary<string, int>();
            _idMap = new ConcurrentDictionary<string, int>();

            _timer = new Timer(1);
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
            bool success = _requestedSymbols.TryAdd(_requestIDs, request.Instrument.Symbol);

            int number;
            if (request.Frequency == BarSize.Tick)
                number = 1;
            else
                number = (int)(request.Frequency.ToTimeSpan().TotalMilliseconds);

            _loopsPassed.TryAdd(request.Instrument.Symbol, number);
            _loopLimit.TryAdd(request.Instrument.Symbol, number);
            _idMap.TryAdd(request.Instrument.Symbol, request.AssignedID);
            
            return ++_requestIDs;
        }

        public void CancelRealTimeData(int requestID)
        {
            string symbol;
            _requestedSymbols.TryRemove(requestID, out symbol);
        }

        private void SimulateData(object sender, ElapsedEventArgs e)
        {
            foreach (string s in _requestedSymbols.Values)
            {
                _loopsPassed[s]++;
                if (_loopsPassed[s] < _loopLimit[s]) continue;
                _loopsPassed[s] = 0;

                double open = _rand.NextDouble() * 50 + 20;
                double high = open + _rand.NextDouble();
                double low = open - _rand.NextDouble();
                double close = low + (high - low) * _rand.NextDouble();
                int id;
                bool success =_idMap.TryGetValue(s, out id);

                if(success)
                    RaiseEvent(DataReceived, this, new RealTimeDataEventArgs(
                        s, 
                        MyUtils.ConvertToTimestamp(DateTime.Now), 
                        (decimal) open,
                        (decimal) high,
                        (decimal) low,
                        (decimal) close,
                        1000,
                        0,
                        0,
                        id));
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