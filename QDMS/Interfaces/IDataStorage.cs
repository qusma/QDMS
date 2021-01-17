// -----------------------------------------------------------------------
// <copyright file="IDataStorage.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    /// <summary>
    /// Interface for storage of historical OHLC data
    /// </summary>
    public interface IDataStorage : IHistoricalDataSource, IDisposable
    {
        /// <summary>
        /// Add data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        /// <param name="overwrite"></param>
        /// <param name="adjust"></param>
        void AddData(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool overwrite = false, bool adjust = true);

        /// <summary>
        /// This method allows adding data, but allowing the actual saving of the data to be delayed.
        /// Useful when you want to allow the data source the ability to make batch inserts/save to file/whatever on its own discretion.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        /// <param name="overwrite"></param>
        void AddDataAsync(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool overwrite = false);

        /// <summary>
        /// This method allows adding data, but allowing the actual saving of the data to be delayed.
        /// Useful when you want to allow the data source the ability to make batch inserts/save to file/whatever on its own discretion.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        /// <param name="overwrite"></param>
        void AddDataAsync(OHLCBar data, Instrument instrument, BarSize frequency, bool overwrite = false);


        /// <summary>
        /// Update existing data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        /// <param name="adjust"></param>
        void UpdateData(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool adjust = false);

        /// <summary>
        /// Request data
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="barSize"></param>
        /// <returns></returns>
        List<OHLCBar> GetData(Instrument instrument, DateTime startDate, DateTime endDate, BarSize barSize = BarSize.OneDay);

        /// <summary>
        /// Remove all data on an instrument
        /// </summary>
        /// <param name="instrument"></param>
        void DeleteAllInstrumentData(Instrument instrument);

        /// <summary>
        /// Remove all data on an instrument for a particular frequency
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        void DeleteData(Instrument instrument, BarSize frequency);

        /// <summary>
        /// Delete specific bars
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        /// <param name="bars"></param>
        void DeleteData(Instrument instrument, BarSize frequency, List<OHLCBar> bars);

        /// <summary>
        /// Get info on what data is stored on a particular instrument
        /// </summary>
        /// <param name="instrumentID"></param>
        /// <returns></returns>
        List<StoredDataInfo> GetStorageInfo(int instrumentID);

        /// <summary>
        /// Get info on what data is stored on a particular instrument, for a particular frequency
        /// </summary>
        /// <param name="instrumentID"></param>
        /// <param name="barSize"></param>
        /// <returns></returns>
        StoredDataInfo GetStorageInfo(int instrumentID, BarSize barSize);
    }
}
