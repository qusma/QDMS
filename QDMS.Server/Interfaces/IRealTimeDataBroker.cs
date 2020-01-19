// -----------------------------------------------------------------------
// <copyright file="IRealTimeDataBroker.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using QDMS;

namespace QDMSServer
{
    public interface IRealTimeDataBroker : IDisposable
    {
        /// <summary>
        /// Holds the real time data sources.
        /// </summary>
        ObservableDictionary<string, IRealTimeDataSource> DataSources { get; }

        event EventHandler<RealTimeDataEventArgs> RealTimeDataArrived;
        event EventHandler<TickEventArgs> RealTimeTickArrived;

        bool RequestRealTimeData(RealTimeDataRequest request);
        bool CancelRTDStream(int instrumentID, BarSize frequency);

        ConcurrentNotifierBlockingList<RealTimeStreamInfo> ActiveStreams { get; }
        
    }
}
