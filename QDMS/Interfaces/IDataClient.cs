// -----------------------------------------------------------------------
// <copyright file="IDataClient.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    public interface IDataClient : IDisposable
    {
        event EventHandler<RealTimeDataEventArgs> RealTimeDataReceived;
        event EventHandler<HistoricalDataEventArgs> HistoricalDataReceived;
        event EventHandler<LocallyAvailableDataInfoReceivedEventArgs> LocallyAvailableDataInfoReceived;
        event EventHandler<ErrorArgs> Error;
        bool Connected { get; }
        /// <summary>
        /// Pushes data to local storage.
        /// </summary>
        void PushData(DataAdditionRequest request);
        /// <summary>
        /// Requests information on what historical data is available in local storage for this instrument.
        /// </summary>
        /// <param name="instrument"></param>
        void GetLocallyAvailableDataInfo(Instrument instrument);
        /// <summary>
        /// Request historical data. Data will be delivered through the HistoricalDataReceived event.
        /// </summary>
        /// <returns>An ID uniquely identifying this historical data request.</returns>
        int RequestHistoricalData(HistoricalDataRequest request);
        /// <summary>
        /// Request a new real time data stream. Data will be delivered through the RealTimeDataReceived event.
        /// </summary>
        int RequestRealTimeData(RealTimeDataRequest request);
        /// <summary>
        /// Tries to connect to the QDMS server.
        /// </summary>
        void Connect();
        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        void Disconnect(bool cancelStreams);
        /// <summary>
        /// Query the server for contracts matching a particular set of features.
        /// </summary>
        /// <param name="instrument">An Instrument object; any features that are not null will be search parameters. If null, all instruments are returned.</param>
        /// <returns>A list of instruments matching these features.</returns>
        List<Instrument> FindInstruments(Instrument instrument = null);
        /// <summary>
        /// Cancel a live real time data stream.
        /// </summary>
        void CancelRealTimeData(Instrument instrument);
        /// <summary>
        /// Get a list of all available instruments
        /// </summary>
        /// <returns></returns>
        List<Instrument> GetAllInstruments();
    }
}
