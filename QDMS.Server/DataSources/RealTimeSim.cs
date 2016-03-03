// -----------------------------------------------------------------------
// <copyright file="RealTimeSim.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using QDMS;
using QDMS.Annotations;

#pragma warning disable 67
namespace QDMSServer.DataSources
{
    public class RealTimeSim : IRealTimeDataSource, IDisposable
    {
        private Timer _timer;
        private ConcurrentDictionary<int, int> _requestedInstrumentIDs;
        private ConcurrentDictionary<int, int> _loopsPassed;
        private ConcurrentDictionary<int, int> _loopLimit;
        private ConcurrentDictionary<int, int> _idMap;
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
            _requestedInstrumentIDs = new ConcurrentDictionary<int, int>();
            _loopsPassed = new ConcurrentDictionary<int, int>();
            _loopLimit = new ConcurrentDictionary<int, int>();
            _idMap = new ConcurrentDictionary<int, int>();

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
            if (!request.Instrument.ID.HasValue) throw new Exception("ID doesn't have value.");


            bool success = _requestedInstrumentIDs.TryAdd(_requestIDs, request.Instrument.ID.Value);

            int number;
            if (request.Frequency == BarSize.Tick)
                number = 1;
            else
                number = (int)(request.Frequency.ToTimeSpan().TotalMilliseconds);

            _loopsPassed.TryAdd(request.Instrument.ID.Value, number);
            _loopLimit.TryAdd(request.Instrument.ID.Value, number);
            _idMap.TryAdd(request.Instrument.ID.Value, request.AssignedID);
            
            return ++_requestIDs;
        }

        public void CancelRealTimeData(int requestID)
        {
            int instrumentID;
            _requestedInstrumentIDs.TryRemove(requestID, out instrumentID);
        }

        private void SimulateData(object sender, ElapsedEventArgs e)
        {
            foreach (int instrumentID in _requestedInstrumentIDs.Values)
            {
                _loopsPassed[instrumentID]++;
                if (_loopsPassed[instrumentID] < _loopLimit[instrumentID]) continue;
                _loopsPassed[instrumentID] = 0;

                double open = _rand.NextDouble() * 50 + 20;
                double high = open + _rand.NextDouble();
                double low = open - _rand.NextDouble();
                double close = low + (high - low) * _rand.NextDouble();
                int id;
                bool success = _idMap.TryGetValue(instrumentID, out id);

                if(success)
                    RaiseEvent(DataReceived, this, new RealTimeDataEventArgs(
                        instrumentID, 
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
#pragma warning restore 67