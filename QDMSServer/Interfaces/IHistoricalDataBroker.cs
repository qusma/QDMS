// -----------------------------------------------------------------------
// <copyright file="IHistoricalDataBroker.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using QDMS;

namespace QDMSServer
{
    public interface IHistoricalDataBroker
    {
        /// <summary>
        /// Holds the real time data sources.
        /// </summary>
        Dictionary<string, IHistoricalDataSource> DataSources { get; }

        void RequestHistoricalData(HistoricalDataRequest request);
        void AddData(DataAdditionRequest request);
        List<StoredDataInfo> GetAvailableDataInfo(Instrument instrument);

        event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        void Dispose();
    }
}
