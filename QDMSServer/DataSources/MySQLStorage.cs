// -----------------------------------------------------------------------
// <copyright file="LocalStorage.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;
using NLog;
using QDMS;

#pragma warning disable 67
namespace QDMSServer.DataSources
{
    public class MySQLStorage : IDataStorage
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public MySQLStorage()
        {
            Name = "Local Storage";
        }

        /// <summary>
        /// Connect to the data source.
        /// </summary>
        public void Connect()
        {
            
        }

        /// <summary>
        /// Disconnect from the data source.
        /// </summary>
        public void Disconnect()
        {
            
        }

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        public bool Connected
        {
            get
            {
                using (MySqlConnection connection = DBUtils.CreateMySqlConnection("qdmsdata"))
                {
                    bool result = connection.Ping();
                    return result;
                }
            }
        }

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Tries to connect to the database.
        /// </summary>
        /// <param name="succeeded"></param>
        /// <returns></returns>
        private MySqlConnection TryConnect(out bool succeeded)
        {
            MySqlConnection connection = DBUtils.CreateMySqlConnection("qdmsdata");
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    _logger.Log(LogLevel.Error, "Local storage: DB connection failed with error: " + ex.Message));
                succeeded = false;
                return connection;
            }

            succeeded = connection.Ping();
            return connection;
        }

        /// <summary>
        /// Returns data from local storage.
        /// </summary>
        /// <param name="instrument">The instrument whose data you want.</param>
        /// <param name="startDate">Starting datetime.</param>
        /// <param name="endDate">Ending datetime.</param>
        /// <param name="frequency">Frequency.</param>
        /// <returns></returns>
        public List<OHLCBar> GetData(Instrument instrument, DateTime startDate, DateTime endDate, BarSize frequency = BarSize.OneDay)
        {
            bool isConnected;
            MySqlConnection connection = TryConnect(out isConnected);
            if (!isConnected)
                throw new Exception("Could not connect to database");

            using (var cmd = new MySqlCommand("", connection))
            {
                cmd.CommandText = "SELECT * FROM data WHERE " +
                                                "InstrumentID = ?ID AND Frequency = ?Freq AND DT >= ?Start AND DT <= ?End ORDER BY DT ASC";
                cmd.Parameters.AddWithValue("ID", instrument.ID);
                cmd.Parameters.AddWithValue("Freq", (int)frequency);
                cmd.Parameters.AddWithValue("Start", startDate);
                cmd.Parameters.AddWithValue("End", endDate);

                cmd.ExecuteNonQuery();

                var data = new List<OHLCBar>();

                var sessionEndTimes = instrument.SessionEndTimesByDay();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var bar = new OHLCBar
                        {
                            Open = reader.GetDecimal("Open"),
                            High = reader.GetDecimal("High"),
                            Low = reader.GetDecimal("Low"),
                            Close = reader.GetDecimal("Close"),
                            AdjOpen = reader.IsDBNull(7) ? null : (decimal?) reader.GetDecimal("AdjOpen"),
                            AdjHigh = reader.IsDBNull(8) ? null : (decimal?)reader.GetDecimal("AdjHigh"),
                            AdjLow = reader.IsDBNull(9) ? null : (decimal?)reader.GetDecimal("AdjLow"),
                            AdjClose = reader.IsDBNull(10) ? null : (decimal?)reader.GetDecimal("AdjClose"),
                            Volume = reader.IsDBNull(11) ? null : (long?) reader.GetInt64("Volume"),
                            OpenInterest = reader.IsDBNull(12) ? null : (int?) reader.GetInt32("OpenInterest"),
                            Dividend = reader.IsDBNull(13) ? null : (decimal?) reader.GetDecimal("Dividend"),
                            Split = reader.IsDBNull(14) ? null : (decimal?) reader.GetDecimal("Split")
                        };

                        bar.DT = reader.GetDateTime("DT");
                        if (frequency >= BarSize.OneDay)
                        {
                            //in the case of low frequency data
                            //we adjust the closing time to use the proper session time
                            if(sessionEndTimes.ContainsKey(bar.DT.DayOfWeek.ToInt()))
                                bar.DT = bar.DT.Date + sessionEndTimes[bar.DT.DayOfWeek.ToInt()];  
                        }
                        data.Add(bar);
                    }
                }

                connection.Close();

                return data;
            }
        }

        //for IHistoricalDataSource, we just grab data and send it back with the event
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            var data = GetData(request.Instrument, request.StartingDate, request.EndingDate, request.Frequency);

            RaiseEvent(HistoricalDataArrived, this, new HistoricalDataEventArgs(request, data));
        }

        /// <summary>
        /// Adjusts OHLC data for dividends and splits.
        /// </summary>
        private void AdjustData(Instrument instrument, BarSize frequency)
        {
            var data = GetData(instrument, new DateTime(1900, 1, 1), DateTime.Now, frequency);

            PriceAdjuster.AdjustData(ref data);

            //data has been adjusted, save it to the db
            UpdateData(data, instrument, frequency);
        }

        //Add new data to local storage
        public void AddData(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool overwrite = false, bool adjust = true)
        {
            if (!instrument.ID.HasValue)
                throw new Exception("Instrument must have an ID assigned to it.");

            if (data.Count == 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    _logger.Log(LogLevel.Error, "Local storage: asked to add data of 0 length"));
                return;
            }

            bool isConnected;
            MySqlConnection connection = TryConnect(out isConnected);
            if (!isConnected)
                throw new Exception("Could not connect to database");

            bool needsAdjustment = false;
            using (var cmd = new MySqlCommand("", connection))
            {
                int tmpCounter = 0;
                cmd.CommandText = "START TRANSACTION;";
                for (int i = 0; i < data.Count; i++)
                {
                    var bar = data[i];
                    if (frequency >= BarSize.OneDay)
                    {
                        //we don't save the time when saving this stuff to allow flexibility with changing sessions
                        bar.DT = bar.DT.Date; 
                    }

                    cmd.CommandText += string.Format("{15} INTO data " +
                                       "(DT, InstrumentID, Frequency, Open, High, Low, Close, AdjOpen, AdjHigh, AdjLow, AdjClose, " +
                                       "Volume, OpenInterest, Dividend, Split) VALUES (" +
                                       "'{0}', {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14});",
                                       bar.DT.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                       instrument.ID,
                                       (int)frequency,
                                       bar.Open,
                                       bar.High,
                                       bar.Low,
                                       bar.Close,
                                       bar.AdjOpen.HasValue ? bar.AdjOpen.Value.ToString() : "NULL",
                                       bar.AdjHigh.HasValue ? bar.AdjHigh.Value.ToString() : "NULL",
                                       bar.AdjLow.HasValue ? bar.AdjLow.Value.ToString() : "NULL",
                                       bar.AdjClose.HasValue ? bar.AdjClose.Value.ToString() : "NULL",
                                       bar.Volume.HasValue ? bar.Volume.Value.ToString() : "NULL",
                                       bar.OpenInterest.HasValue ? bar.OpenInterest.Value.ToString() : "NULL",
                                       bar.Dividend.HasValue ? bar.Dividend.Value.ToString() : "NULL",
                                       bar.Split.HasValue ? bar.Split.Value.ToString() : "NULL",
                                       overwrite ? "REPLACE" : "INSERT IGNORE"
                                       );

                    if (!needsAdjustment && (data[i].Dividend.HasValue || data[i].Split.HasValue))
                        needsAdjustment = true;

                    tmpCounter++;

                    //periodically insert...not sure what the optimal number of rows is
                    if (tmpCounter > 1000)
                    {
                        cmd.CommandText += "COMMIT;";
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                                _logger.Log(LogLevel.Error, "MySql query error: " + ex.Message));
                        }
                        cmd.CommandText = "START TRANSACTION;";
                        tmpCounter = 0;
                    }
                }
                cmd.CommandText += "COMMIT;";
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    //no need to log duplicate key errors if we're not overwriting, it's by design.
                    if (!ex.Message.Contains("Duplicate")) 
                        Application.Current.Dispatcher.Invoke(() =>
                            _logger.Log(LogLevel.Error, "MySql query error: " + ex.Message));
                }
                
                //finally update the instrument info
                cmd.CommandText = string.Format(
                                    "INSERT INTO instrumentinfo (InstrumentID, Frequency, EarliestDate, LatestDate) VALUES " +
                                    "({0}, {1}, '{2}', '{3}') " +
                                    "ON DUPLICATE KEY UPDATE EarliestDate = LEAST(EarliestDate, '{2}'), " +
                                    "LatestDate = GREATEST(LatestDate, '{3}')",
                                    instrument.ID.Value,
                                    (int)frequency,
                                    data[0].DT.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                    data[data.Count - 1].DT.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        _logger.Log(LogLevel.Error, "MySql query error: " + ex.Message));
                }

                Application.Current.Dispatcher.Invoke(() =>
                _logger.Log(LogLevel.Info, string.Format(
                    "Saved {0} data points of {1} @ {2} to local storage. {3} {4}", 
                    data.Count, 
                    instrument.Symbol, 
                    Enum.GetName(typeof(BarSize), frequency),
                    overwrite ? "Overwrite" : "NoOverwrite",
                    adjust ? "Adjust" : "NoAdjust")));
            }

            connection.Close();

            //if there were dividends or splits in the data we added,
            //we need to generate adjusted prices
            if (adjust && needsAdjustment && frequency == BarSize.OneDay) //adjustments are nonsensical on any other frequency
                AdjustData(instrument, frequency);
        }

        /// <summary>
        /// This method allows adding data, but allowing the actual saving of the data to be delayed.
        /// Useful when you want to allow the data source the ability to make batch inserts/save to file/whatever on its own discretion.
        /// </summary>
        public void AddDataAsync(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool overwrite = false)
        {
            AddData(data, instrument, frequency, overwrite);
        }

        /// <summary>
        /// This method allows adding data, but allowing the actual saving of the data to be delayed.
        /// Useful when you want to allow the data source the ability to make batch inserts/save to file/whatever on its own discretion.
        /// </summary>
        public void AddDataAsync(OHLCBar data, Instrument instrument, BarSize frequency, bool overwrite = false)
        {
            AddData(new List<OHLCBar> {data}, instrument, frequency, overwrite);
        }

        /// <summary>
        /// Add new data to the db, overwriting any older data that is in place.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        /// <param name="adjust"></param>
        public void UpdateData(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool adjust = false)
        {
            AddData(data, instrument, frequency, true, adjust);
        }

        /// <summary>
        /// Deletes all data for a specific instrument.
        /// </summary>
        /// <param name="instrument"></param>
        public void DeleteAllInstrumentData(Instrument instrument)
        {
            bool isConnected;
            MySqlConnection connection = TryConnect(out isConnected);
            if (!isConnected)
                throw new Exception("Could not connect to database");

            using (var cmd = new MySqlCommand("", connection))
            {
                cmd.CommandText = string.Format("DELETE FROM data WHERE InstrumentID = {0}", instrument.ID);
                cmd.ExecuteNonQuery();
                cmd.CommandText = string.Format("DELETE FROM instrumentinfo WHERE InstrumentID = {0}", instrument.ID);
                cmd.ExecuteNonQuery();
            }

            connection.Close();
        }

        /// <summary>
        /// Delets data of a specific frequency for a specific instrument.
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        public void DeleteData(Instrument instrument, BarSize frequency)
        {
            bool isConnected;
            MySqlConnection connection = TryConnect(out isConnected);
            if (!isConnected)
                throw new Exception("Could not connect to database");

            using (var cmd = new MySqlCommand("", connection))
            {
                cmd.CommandText = string.Format("DELETE FROM data WHERE InstrumentID = {0} AND Frequency = {1}", instrument.ID, (int)frequency);
                cmd.ExecuteNonQuery();
                cmd.CommandText = string.Format("DELETE FROM instrumentinfo WHERE InstrumentID = {0} AND Frequency = {1}", instrument.ID, (int)frequency);
                cmd.ExecuteNonQuery();
            }

            connection.Close();
        }

        /// <summary>
        /// Deletes a specific set of bars, at a given frequency for a given instrument.
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        /// <param name="bars"></param>
        public void DeleteData(Instrument instrument, BarSize frequency, List<OHLCBar> bars)
        {
            bool isConnected;
            MySqlConnection connection = TryConnect(out isConnected);
            if (!isConnected)
                throw new Exception("Could not connect to database");

            using (var cmd = new MySqlCommand("", connection))
            {
                cmd.CommandText = "START TRANSACTION;";
                for (int i = 0; i < bars.Count; i++)
                {
                    cmd.CommandText += string.Format("DELETE FROM data WHERE InstrumentID = {0} AND Frequency = {1} AND DT = '{2}';", 
                        instrument.ID, 
                        (int)frequency,
                        frequency < BarSize.OneDay 
                        ? bars[i].DT.ToString("yyyy-MM-dd HH:mm:ss.fff")
                        : bars[i].DT.ToString("yyyy-MM-dd")); //for frequencies greater than a day, we don't care about time
                }
                cmd.CommandText += "COMMIT;";
                cmd.ExecuteNonQuery();
                
                //check if there's any data left
                cmd.CommandText = string.Format("SELECT COUNT(*) FROM data WHERE InstrumentID = {0} AND Frequency = {1}",
                    instrument.ID,
                    (int)frequency);
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    int count = reader.GetInt32(0);
                    reader.Close();

                    if (count == 0)
                    {
                        //remove from the instrumentinfo table
                        cmd.CommandText = string.Format("DELETE FROM instrumentinfo WHERE InstrumentID = {0} AND Frequency = {1}", 
                            instrument.ID, 
                            (int)frequency);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        //update the instrumentinfo table
                        cmd.CommandText = string.Format("UPDATE instrumentinfo AS t1, " +
                                                        "(SELECT MAX(DT) as maxdt, MIN(DT) as mindt FROM data WHERE instrumentID = {0} AND Frequency = {1}) AS t2 " +
                                                        "SET `EarliestDate` = t2.mindt, " +
                                                        "t1.`LatestDate` = t2.maxdt " +
                                                        "WHERE t1.instrumentID = {0} AND t1.Frequency = {1}", instrument.ID, (int)frequency);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            connection.Close();
        }

        /// <summary>
        /// Gets the range of dates and the associated frequencies at which data is available for the specified instrument.
        /// </summary>
        /// <param name="instrumentID"></param>
        /// <returns></returns>
        public List<StoredDataInfo> GetStorageInfo(int instrumentID)
        {
            bool isConnected;
            MySqlConnection connection = TryConnect(out isConnected);
            if (!isConnected)
                throw new Exception("Could not connect to database");

            var instrumentInfos = new List<StoredDataInfo>();

            using (var cmd = new MySqlCommand("", connection))
            {
                cmd.CommandText = "SELECT * FROM instrumentinfo WHERE InstrumentID = " + instrumentID;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var info = new StoredDataInfo
                        {
                            InstrumentID = instrumentID,
                            Frequency = (BarSize) reader.GetInt32("Frequency"),
                            EarliestDate = reader.GetDateTime("EarliestDate"),
                            LatestDate = reader.GetDateTime("LatestDate")
                        };
                        instrumentInfos.Add(info);
                    }
                }
            }
            connection.Close();

            return instrumentInfos;
        }

        /// <summary>
        /// Gets the range of dates for which data is available for the specified instrument, at the specified frequency.
        /// </summary>
        /// <returns>Null if no match was found. A SotredDataInfo otherwise.</returns>
        public StoredDataInfo GetStorageInfo(int instrumentID, BarSize frequency)
        {
            bool isConnected;
            MySqlConnection connection = TryConnect(out isConnected);
            if (!isConnected)
                throw new Exception("Could not connect to database");

            var instrumentInfo = new StoredDataInfo();

            using (var cmd = new MySqlCommand("", connection))
            {
                cmd.CommandText = string.Format("SELECT * FROM instrumentinfo WHERE InstrumentID = {0} AND Frequency = {1}",
                    instrumentID,
                    (int)frequency);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        instrumentInfo.InstrumentID = instrumentID;
                        instrumentInfo.Frequency = (BarSize)reader.GetInt32("Frequency");
                        instrumentInfo.EarliestDate = reader.GetDateTime("EarliestDate");
                        instrumentInfo.LatestDate = reader.GetDateTime("LatestDate");
                    }
                    else
                    {
                        return null; //return null if nothing is found that matches these criteria
                    }
                }
            }
            connection.Close();

            return instrumentInfo;
        }


        ///<summary>
        /// Raise the event in a threadsafe manner
        ///</summary>
        ///<param name="event"></param>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        ///<typeparam name="T"></typeparam>
        static private void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
        where T : EventArgs
        {
            EventHandler<T> handler = @event;
            if (handler == null) return;
            handler(sender, e);
        }

        public event EventHandler<ErrorArgs> Error;
        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            
        }
    }
}
#pragma warning restore 67