// -----------------------------------------------------------------------
// <copyright file="FRED.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;
using QDMS;

namespace QDMSServer.DataSources
{
    public class FRED : IHistoricalDataSource
    {
        private string _apiKey = "f8d71bdcf1d7153e157e0baef35f67db";

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Connect to the data source.
        /// </summary>
        public void Connect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disconnect from the data source.
        /// </summary>
        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name { get; private set; }
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
    }
}
