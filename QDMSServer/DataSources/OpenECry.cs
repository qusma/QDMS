// -----------------------------------------------------------------------
// <copyright file="OEC.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

//Documentation: http://pro.openecry.com/api/api/index.html

//Currently, OEC has the next limits:
//DOMs - number of DOM subscriptions
//
//MaxBars - number of bar subscriptions
//
//MaxTicks - tick subscriptions
//
//Quotes - futures-related quote subscriptions
//
//QuotesEquity - equity-related quote subscriptions (if you have equity account)
//
//QuotesFX - FX quote subscriptions (if you have forex account)
//
//Since bar and tick requests are more expensive resource that quotes and DOMs, they have additional limitations. These limits are OEC-wide and can be changed any time:
//MaxBars = 8192: Maximum amount of bars which returns back to a client per one request
//
//MaxDayBarDays = 365: Maximum allowed days to load for day and day-based bars
//
//MaxIntraBarDays = 90: Maximum allowed days to load for intraday bars
//
//MaxPerGroup = 4096: Maximum items per group which returns back to a client
//
//MaxTickBasedBarDays = 10: Maximum allowed days to load for tick-based bars
//
//MaxTickDays = 3: Maximum allowed days to load for ticks
//
//MaxTicks = 65536: Maximum amount of ticks which returns back to a client per one request


using System;
using System.ComponentModel;
using OEC.API;
using QDMS;

namespace QDMSServer.DataSources
{
    public class OpenECry : IRealTimeDataSource, IHistoricalDataSource
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private OEC.API.OECClient _client;

        public OpenECry()
        {
            _client = new OEC.API.OECClient();
            _client.UUID = "9e61a8bc-0a31-4542-ad85-33ebab0e4e86";

            //Hook up connected/disconnected events
            _client.OnLoginComplete += () => { Connected = true; };
            _client.OnDisconnected += (e) => 
            { 
                Connected = false;
                RaiseEvent(Disconnected, this, new DataSourceDisconnectEventArgs("OpenECry", ""));
            };

            //Hook up error event and propagate it
            _client.OnError += (e) => RaiseEvent(Error, this, new ErrorArgs(0, e.Message));

            //Contract request arrives -- we use it to request data
            //todo

            //when tick data comes in, pass it on
            _client.OnPriceTick += _client_OnPriceTick;
        }

        private void _client_OnPriceTick(Contract contract, Price tick)
        {
            int instrumentID = 0; //todo fix, keep id -> id map
            int requestID = 0; //todo fix, keep id -> id map
            decimal price = (decimal)tick.LastPrice;

            var args = new RealTimeDataEventArgs(
                instrumentID,
                tick.LastDateTime.ToBinary(),
                price,
                price,
                price,
                price,
                tick.LastVol,
                tick.LastPrice,
                1,
                requestID);
            RaiseEvent(DataReceived, this, args);
        }

        /// <summary>
        /// Connect to the data source.
        /// </summary>
        public void Connect()
        {
            _client.Connect("api.openecry.com", 9200, "vic", "vic", true);
        }

        /// <summary>
        /// Disconnect from the data source.
        /// </summary>
        public void Disconnect()
        {
            _client.Disconnect();
        }

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// Request real time data.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The ID associated with this real time data request.</returns>
        public int RequestRealTimeData(RealTimeDataRequest request)
        {
            //var contract = new OEC.API.Contract();
            //_client.Subscribe()

            //So here's what's going on: it won't let us create a contract. 
            //We have to request it, then when it arrives we request the data stream.

            //Depending on the BarSize we have to request either ticks or bars

            return 0; //fix
            //TODO write
        }

        /// <summary>
        /// Cancel a real time data stream.
        /// </summary>
        /// <param name="requestID">The ID of the real time data stream.</param>
        public void CancelRealTimeData(int requestID)
        {
            //_client.CancelSubscription();
            //TODO write
        }

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name { get { return "OpenECry"; } }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event EventHandler<RealTimeDataEventArgs> DataReceived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

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
