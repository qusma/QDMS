// -----------------------------------------------------------------------
// <copyright file="ITickDataStorage.cs" company="">
// Copyright 2019 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    /// <summary>
    ///     Interface for local storage of ticks
    /// </summary>
    public interface ITickDataStorage : IDisposable
    {
        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="instrumentId"></param>
        void AddDataAsync(TickEventArgs data, int instrumentId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        void RequestHistoricalData(HistoricalDataRequest request);

        /// <summary>
        /// Call before starting to send real time data to be stored
        /// </summary>
        /// <param name="instrument"></param>
        void InitializeRealtimeDataStream(Instrument instrument);

        /// <summary>
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        void DeleteData(Instrument instrument, DateTime startDate, DateTime endDate);

        /// <summary>
        ///     Get info on what data is stored on a particular instrument
        /// </summary>
        /// <param name="instrumentID"></param>
        /// <returns></returns>
        List<StoredDataInfo> GetStorageInfo(int instrumentID);

        /// <summary>
        /// Fires when data arrives
        /// </summary>
        event EventHandler<HistoricalTickDataEventArgs> HistoricalDataArrived;

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