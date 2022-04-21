// -----------------------------------------------------------------------
// <copyright file="ITickDataSource.cs" company="">
// Copyright 2019 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    /// <summary>
    /// Data source interface for tick data
    /// </summary>
    public interface ITickDataSource : INotifyPropertyChanged
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
        /// The name of the data source.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Fires when data arrives
        /// </summary>
        event EventHandler<HistoricalTickDataEventArgs> HistoricalDataArrived;

        /// <summary>
        /// Fires when data arrives
        /// </summary>
        event EventHandler<TickEventArgs> RealTimeDataArrived;

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