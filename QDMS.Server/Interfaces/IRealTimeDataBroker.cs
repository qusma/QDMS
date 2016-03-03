// -----------------------------------------------------------------------
// <copyright file="IRealTimeDataBroker.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using QDMS;

namespace QDMSServer
{
    public interface IRealTimeDataBroker
    {
        /// <summary>
        /// Holds the real time data sources.
        /// </summary>
        ObservableDictionary<string, IRealTimeDataSource> DataSources { get; }

        event EventHandler<RealTimeDataEventArgs> RealTimeDataArrived;

        bool RequestRealTimeData(RealTimeDataRequest request);
        bool CancelRTDStream(int instrumentID);

        ConcurrentNotifierBlockingList<RealTimeStreamInfo> ActiveStreams { get; }
    }
}
