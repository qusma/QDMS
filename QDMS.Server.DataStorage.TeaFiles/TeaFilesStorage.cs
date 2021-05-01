// -----------------------------------------------------------------------
// <copyright file="TeaFilesStorage.cs" company="">
// Copyright 2019 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using QDMS;
using TeaTime;

namespace QDMSApp.DataSources
{
    public class TeaFilesStorage : ITickDataStorage
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _storageDir;
        private readonly Thread _saverThread;
        private readonly Thread _readerThread;
        private readonly ConcurrentDictionary<int, ConcurrentQueue<TickEventArgs>> _queuedTicks;
        private readonly ConcurrentQueue<HistoricalDataRequest> _queuedRequests;
        private bool _runThreads = true;
        private int _timeBetweenFileWrites = 250; //in ms

        public TeaFilesStorage(IFileSystem fileSystem, ISettings settings)
        {
            _fileSystem = fileSystem;
            _storageDir = settings.teaFilesDirectory;
            _saverThread = new Thread(TickSaverLoop);
            _readerThread = new Thread(DataLoaderLoop);
        }

        private async void TickSaverLoop()
        {
            while (_runThreads)
            {
                foreach (var kvp in _queuedTicks)
                {
                    var ticks = kvp.Value;
                    var id = kvp.Key;
                    using (var stream = _fileSystem.File.OpenWrite(GetFileName(id, DateTime.Now.Date)))
                    using (var file = TeaFile<Tick>.OpenWrite(stream)) //TODO this breaks if adding historical
                    {
                        while (ticks.TryDequeue(out TickEventArgs req))
                        {
                            file.Write(req.Tick);
                        }
                    }
                }

                Thread.Sleep(_timeBetweenFileWrites);
            }
        }

        private async void DataLoaderLoop()
        {
            while (_runThreads)
            {
                while (_queuedRequests.TryDequeue(out HistoricalDataRequest req))
                {
                    var data = GetData(req.Instrument.ID.Value, req.StartingDate, req.EndingDate);
                    RaiseEvent(HistoricalDataArrived, this, new HistoricalTickDataEventArgs(req.Instrument, data));
                }
                Thread.Sleep(50);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<HistoricalTickDataEventArgs> HistoricalDataArrived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

        public bool Connected { get; private set; }
        public string Name { get; } = "TeaFiles";

        public void Connect()
        {
            Connected = true;
            _runThreads = true;
            _saverThread.Start();
            _readerThread.Start();
        }

        public void Disconnect()
        {
            _runThreads = false;
            _saverThread.Join();
            _readerThread.Join();
            Connected = false;
        }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            _queuedRequests.Enqueue(request);
        }

        public void InitializeRealtimeDataStream(Instrument instrument)
        {
            if (!_queuedTicks.ContainsKey(instrument.ID.Value))
            {
                _queuedTicks.TryAdd(instrument.ID.Value, new ConcurrentQueue<TickEventArgs>());
            }
        }


        public void Dispose()
        {
            Disconnect();
        }

        public void AddDataAsync(TickEventArgs data, int instrumentId)
        {
            _queuedTicks[instrumentId].Enqueue(data);
        }

        private List<Tick> GetData(int instrumentId, DateTime startDate, DateTime endDate)
        {
            var data = new List<Tick>();

            //loop through the relevant files
            var files = GetFilesBetweenDates(instrumentId, startDate, endDate);
            foreach (var fname in files)
            {
                using (var stream = _fileSystem.File.OpenRead(fname))
                using (var tf = TeaFile<Tick>.OpenRead(fname))
                {
                    //todo seek start point
                }
            }




            //do we need to stream historical data? Something like btc/usd runs at 1.5MB per hours, grabbing a couple of months at once might be a problem...Nah, let the end user split up their requests if they need to.

            return data;
        }

        private string GetFileName(int instrumentId, DateTime date)
        {
            return instrumentId + date.ToString("-yyyy-MM-dd");
        }

        public void DeleteData(Instrument instrument, DateTime startDate, DateTime endDate)
        {
            
            throw new NotImplementedException();
        }

        public List<StoredDataInfo> GetStorageInfo(int instrumentID)
        {
            throw new NotImplementedException();

        }

        private List<string> GetAllFilesForInstrument(int instrumentId)
        {
            return _fileSystem.Directory.GetFiles(_fileSystem.Path.Combine(_storageDir, instrumentId.ToString())).ToList();
        }

        private List<string> GetFilesBetweenDates(int instrumentId, DateTime startDate, DateTime endDate)
        {
            var fileNames = new List<string>();
            //TODO directory per file!
            var allFiles = GetAllFilesForInstrument(instrumentId);
            var tmpDate = new DateTime(startDate.Year, startDate.Month, startDate.Day);
            while (tmpDate < endDate)
            {
                var fname = GetFileName(instrumentId, tmpDate);
                if (allFiles.Contains(fname))
                {
                    fileNames.Add(fname);
                }
                tmpDate = tmpDate.AddDays(1);
            }

            return fileNames;
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
    }
}