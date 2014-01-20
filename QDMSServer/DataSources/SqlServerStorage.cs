using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QDMS;

namespace QDMSServer.DataSources
{
    public class SqlServerStorage : IDataStorage
    {
        /// <summary>
        /// Connect to the data source.
        /// </summary>
        public void Connect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disconnect from the data source.
        /// </summary>
        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name { get; private set; }
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            throw new NotImplementedException();
        }

        public void AddData(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool overwrite = false, bool adjust = true)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method allows adding data, but allowing the actual saving of the data to be delayed.
        /// Useful when you want to allow the data source the ability to make batch inserts/save to file/whatever on its own discretion.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        /// <param name="overwrite"></param>
        public void AddDataAsync(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public void AddDataAsync(OHLCBar data, Instrument instrument, BarSize frequency, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public void UpdateData(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool adjust = false)
        {
            throw new NotImplementedException();
        }

        public List<OHLCBar> GetData(Instrument instrument, DateTime startDate, DateTime endDate, BarSize barSize = BarSize.OneDay)
        {
            throw new NotImplementedException();
        }

        public void DeleteAllInstrumentData(Instrument instrument)
        {
            throw new NotImplementedException();
        }

        public void DeleteData(Instrument instrument, BarSize frequency)
        {
            throw new NotImplementedException();
        }

        public void DeleteData(Instrument instrument, BarSize frequency, List<OHLCBar> bars)
        {
            throw new NotImplementedException();
        }

        public List<StoredDataInfo> GetStorageInfo(int instrumentID)
        {
            throw new NotImplementedException();
        }

        public StoredDataInfo GetStorageInfo(int instrumentID, BarSize barSize)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;
        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
    }
}
