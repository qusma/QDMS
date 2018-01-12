// -----------------------------------------------------------------------
// <copyright file="IRealTimeDataSource.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    /// <summary>
    /// Provides real time OHLC bars
    /// </summary>
    public interface IRealTimeDataSource : INotifyPropertyChanged
    {
        /// <summary>
        /// Connect to the data source.
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnect from the data source.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Request real time data.
        /// </summary>
        /// <param name="request"></param>
        void RequestRealTimeData(RealTimeDataRequest request);

        /// <summary>
        /// Cancel a real time data stream.
        /// </summary>
        /// <param name="requestID">The ID of the real time data stream.</param>
        void CancelRealTimeData(int requestID);

        /// <summary>
        /// The name of the data source.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Fires when new real bar time data is received.
        /// </summary>
        event EventHandler<RealTimeDataEventArgs> DataReceived;

        /// <summary>
        /// Fires when new real tick time data is received.
        /// </summary>
        event EventHandler<RealTimeTickEventArgs> TickReceived;

        /// <summary>
        /// Fires on any error.
        /// </summary>
        event EventHandler<ErrorArgs> Error;
        
        /// <summary>
        /// Fires on disconnection from the data source.
        /// </summary>
        event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
    }
}
