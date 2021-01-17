﻿// -----------------------------------------------------------------------
// <copyright file="IHistoricalDataBroker.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using QDMS;

namespace QDMSServer
{
    public interface IHistoricalDataBroker : IDisposable
    {
        /// <summary>
        /// Holds the historical data sources.
        /// </summary>
        ObservableDictionary<string, IHistoricalDataSource> DataSources { get; }

        void RequestHistoricalData(HistoricalDataRequest request);
        void AddData(DataAdditionRequest request);

        event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        event EventHandler<ErrorArgs> Error;
    }
}
