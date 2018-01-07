// -----------------------------------------------------------------------
// <copyright file="Class1.cs" company="">
// Copyright 2018 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;
using WebSocket4Net;

namespace QDMS.Server.DataSources.Binance
{
    public class Binance : IHistoricalDataSource, IRealTimeDataSource
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        
        public int RequestRealTimeData(RealTimeDataRequest request) => throw new NotImplementedException();

        public void CancelRealTimeData(int requestID)
        {
            throw new NotImplementedException();
        }

        public string Name => "Binance";

        public bool Connected { get; }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<RealTimeDataEventArgs> DataReceived;
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
    }
}