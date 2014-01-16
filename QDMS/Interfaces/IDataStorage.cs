// -----------------------------------------------------------------------
// <copyright file="IDataStorage.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    public interface IDataStorage : IHistoricalDataSource
    {
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
        void AddDataAsync(OHLCBar data, Instrument instrument, BarSize frequency, bool overwrite = false);


        void UpdateData(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool adjust = false);
        List<OHLCBar> GetData(Instrument instrument, DateTime startDate, DateTime endDate, BarSize barSize = BarSize.OneDay);
        void DeleteAllInstrumentData(Instrument instrument);
        void DeleteData(Instrument instrument, BarSize frequency);
        void DeleteData(Instrument instrument, BarSize frequency, List<OHLCBar> bars);
        List<StoredDataInfo> GetStorageInfo(int instrumentID);
        StoredDataInfo GetStorageInfo(int instrumentID, BarSize barSize);
    }
}
