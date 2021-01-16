using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace QDMS.Server.DataSources.Tiingo
{
    public class Tiingo : IHistoricalDataSource //IRealTimeDataSource
    {
        private string _apiKey = "";
        private HttpClient _client = new HttpClient();
        private bool _runDownloader;
        private ConcurrentQueue<HistoricalDataRequest> _queuedRequests;
        private Thread _downloaderThread;

        public Tiingo(ISettings settings)
        {
            _queuedRequests = new ConcurrentQueue<HistoricalDataRequest>();
            _downloaderThread = new Thread(DownloaderLoop);
        }



        public bool Connected => throw new NotImplementedException();

        public string Name => "Tiingo";

        public event EventHandler<RealTimeDataEventArgs> DataReceived;
        public event EventHandler<TickEventArgs> TickReceived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;

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

        private async Task<List<OHLCBar>> GetData(HistoricalDataRequest req)
        {
            var url = "https://api.tiingo.com/tiingo/daily/<ticker>/prices?startDate=2012-1-1&endDate=2016-1-1&format=csv";

            //crypto: https://api.tiingo.com/tiingo/crypto/prices?tickers=<ticker>&startDate=2019-01-02&resampleFreq=5min

            //fx: 


            //TODO: can also support higher freq data from their IEX endpoint
            
            return new List<OHLCBar>();
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            _queuedRequests.Enqueue(request);
        }

        public void RequestRealTimeData(RealTimeDataRequest request)
        {
            //TODO: implement
        }

        public void CancelRealTimeData(int requestID)
        {
            //TODO: implement
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
