using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;
using System.Windows;
using NLog;
using QDMS;
using QDMS.Annotations;
using System.Globalization;

#pragma warning disable 67

namespace QDMSServer.DataSources
{
    public class SqlServerStorage : IDataStorage
    {
        /// <summary>
        /// Periodically updates the Connected property.
        /// </summary>
        private Timer _connectionStatusUpdateTimer;

        private Logger _logger = LogManager.GetCurrentClassLogger();
        
        private string _connectionString;

        public SqlServerStorage(string connectionString)
        {
            Name = "Local Storage";
            _connectionString = connectionString;

            _connectionStatusUpdateTimer = new Timer(1000);
            _connectionStatusUpdateTimer.Elapsed += _connectionStatusUpdateTimer_Elapsed;
            _connectionStatusUpdateTimer.Start();
        }

        private void _connectionStatusUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    Connected = connection.State == System.Data.ConnectionState.Open;
                }
                catch
                {
                    Connected = false;
                }
            }
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

        private bool _connected;

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        public bool Connected
        {
            get
            {
                return _connected;
            }

            set
            {
                _connected = value;
                OnPropertyChanged();
            }
        }

        private bool TryConnect(out SqlConnection connection)
        {
            connection = new SqlConnection(_connectionString);
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Local storage: DB connection failed with error: " + ex.Message);
                return false;
            }

            return connection.State == System.Data.ConnectionState.Open;
        }

        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name { get; private set; }

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
            SqlConnection connection;
            if (!TryConnect(out connection))
                throw new Exception("Could not connect to database");

            using (var cmd = new SqlCommand("", connection))
            {
                cmd.CommandText = "SELECT * FROM data WHERE " +
                                                "InstrumentID = @ID AND Frequency = @Freq AND DT >= @Start AND DT <= @End ORDER BY DT ASC";
                cmd.Parameters.AddWithValue("ID", instrument.ID);
                cmd.Parameters.AddWithValue("Freq", (int)frequency);
                cmd.Parameters.AddWithValue("Start", frequency >= BarSize.OneDay ? startDate.Date : startDate);
                cmd.Parameters.AddWithValue("End", frequency >= BarSize.OneDay ? endDate.Date : endDate);

                var data = new List<OHLCBar>();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var bar = new OHLCBar
                        {
                            DT = reader.GetDateTime(0),
                            DTOpen = reader.IsDBNull(15) ? null : (DateTime?)reader.GetDateTime(15),
                            Open = reader.GetDecimal(3),
                            High = reader.GetDecimal(4),
                            Low = reader.GetDecimal(5),
                            Close = reader.GetDecimal(6),
                            AdjOpen = reader.IsDBNull(7) ? null : (decimal?)reader.GetDecimal(7),
                            AdjHigh = reader.IsDBNull(8) ? null : (decimal?)reader.GetDecimal(8),
                            AdjLow = reader.IsDBNull(9) ? null : (decimal?)reader.GetDecimal(9),
                            AdjClose = reader.IsDBNull(10) ? null : (decimal?)reader.GetDecimal(10),
                            Volume = reader.IsDBNull(11) ? null : (long?)reader.GetInt64(11),
                            OpenInterest = reader.IsDBNull(12) ? null : (int?)reader.GetInt32(12),
                            Dividend = reader.IsDBNull(13) ? null : (decimal?)reader.GetDecimal(13),
                            Split = reader.IsDBNull(14) ? null : (decimal?)reader.GetDecimal(14)
                        };

                        data.Add(bar);
                    }
                }

                connection.Close();

                return data;
            }
        }

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

        public void AddData(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool overwrite = false, bool adjust = true)
        {
            if (!instrument.ID.HasValue)
                throw new Exception("Instrument must have an ID assigned to it.");

            if (data.Count == 0)
            {
                Log(LogLevel.Error, "Local storage: asked to add data of 0 length");
                return;
            }

            SqlConnection connection;
            if (!TryConnect(out connection))
                throw new Exception("Could not connect to database");

            bool needsAdjustment = false;
            using (var cmd = new SqlCommand("", connection))
            {
                cmd.CommandTimeout = 0;

                var sb = new StringBuilder();
                sb.Append("BEGIN TRAN T1;");

                //We create a temporary table which will then be used to merge the data into the data table
                var r = new Random();
                string tableName = "tmpdata" + r.Next();
                sb.AppendFormat("SELECT * INTO {0} from data where 1=2;", tableName);

                //start the insert
                for (int i = 0; i < data.Count; i++)
                {
                    var bar = data[i];
                    if (frequency >= BarSize.OneDay)
                    {
                        //we don't save the time when saving this stuff to allow flexibility with changing sessions
                        bar.DT = bar.DT.Date;
                        bar.DTOpen = null;
                    }

                    if(i == 0 || (i-1) % 500 == 0)
                    sb.AppendFormat("INSERT INTO {0} " +
                                "(DT, InstrumentID, Frequency, [Open], High, Low, [Close], AdjOpen, AdjHigh, AdjLow, AdjClose, " +
                                "Volume, OpenInterest, Dividend, Split, DTOpen) VALUES ", tableName);

                    sb.AppendFormat("('{0}', {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15})",
                                       bar.DT.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                                       instrument.ID,
                                       (int)frequency,
                                       bar.Open.ToString(CultureInfo.InvariantCulture),
                                       bar.High.ToString(CultureInfo.InvariantCulture),
                                       bar.Low.ToString(CultureInfo.InvariantCulture),
                                       bar.Close.ToString(CultureInfo.InvariantCulture),
                                       bar.AdjOpen.HasValue ? bar.AdjOpen.Value.ToString(CultureInfo.InvariantCulture) : "NULL",
                                       bar.AdjHigh.HasValue ? bar.AdjHigh.Value.ToString(CultureInfo.InvariantCulture) : "NULL",
                                       bar.AdjLow.HasValue ? bar.AdjLow.Value.ToString(CultureInfo.InvariantCulture) : "NULL",
                                       bar.AdjClose.HasValue ? bar.AdjClose.Value.ToString(CultureInfo.InvariantCulture) : "NULL",
                                       bar.Volume.HasValue ? bar.Volume.Value.ToString(CultureInfo.InvariantCulture) : "NULL",
                                       bar.OpenInterest.HasValue ? bar.OpenInterest.Value.ToString(CultureInfo.InvariantCulture) : "NULL",
                                       bar.Dividend.HasValue ? bar.Dividend.Value.ToString(CultureInfo.InvariantCulture) : "NULL",
                                       bar.Split.HasValue ? bar.Split.Value.ToString(CultureInfo.InvariantCulture) : "NULL",
                                       bar.DTOpen.HasValue ? String.Format("'{0:yyyy-MM-ddTHH:mm:ss.fff}'", bar.DTOpen.Value) : "NULL"
                                       );

                    sb.Append((i % 500 != 0 && i < data.Count - 1) ? ", " : ";");
                    
                    if (!needsAdjustment && (data[i].Dividend.HasValue || data[i].Split.HasValue))
                        needsAdjustment = true;
                }

                //Merge the temporary table with the data table
                sb.AppendFormat(@"MERGE INTO
                                    dbo.data T
                                USING
                                    (SELECT * FROM {0}) AS S (DT, InstrumentID, Frequency, [Open], High, Low, [Close], AdjOpen, AdjHigh, AdjLow, AdjClose,
                                        Volume, OpenInterest, Dividend, Split, DTOpen)
                                ON
                                    T.InstrumentID = S.InstrumentID AND T.Frequency = S.Frequency AND T.DT = S.DT
                                WHEN NOT MATCHED THEN
                                    INSERT (DT, InstrumentID, Frequency, [Open], High, Low, [Close], AdjOpen, AdjHigh, AdjLow, AdjClose,
                                        Volume, OpenInterest, Dividend, Split, DTOpen)
                                    VALUES (DT, InstrumentID, Frequency, [Open], High, Low, [Close], AdjOpen, AdjHigh, AdjLow, AdjClose,
                                        Volume, OpenInterest, Dividend, Split, DTOpen)",
                              tableName);

                if (overwrite)
                {
                    sb.Append(@" WHEN MATCHED THEN
                                    UPDATE SET
                                        T.DT = S.DT,
                                        T.InstrumentID = S.InstrumentID,
                                        T.Frequency = S.Frequency,
                                        T.[Open] = S.[Open],
                                        T.High = S.High,
                                        T.Low = S.Low,
                                        T.[Close] = S.[Close],
                                        T.AdjOpen = S.AdjOpen,
                                        T.AdjHigh = S.AdjHigh,
                                        T.AdjLow = S.AdjLow,
                                        T.AdjClose = S.AdjClose,
                                        T.Volume = S.Volume,
                                        T.OpenInterest = S.OpenInterest,
                                        T.Dividend = S.Dividend,
                                        T.Split = S.Split,
                                        T.DTOpen = S.DTOpen;");
                }
                else
                {
                    sb.Append(";");
                }

                sb.AppendFormat("DROP TABLE {0};", tableName);
                sb.Append("COMMIT TRAN T1;");

                cmd.CommandText = sb.ToString();

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, "SQL Server query error: " + ex.Message);
                }

                //finally update the instrument info
                cmd.CommandText = string.Format(@"
                                                MERGE INTO
                                                    instrumentinfo T
                                                USING
                                                    (SELECT * FROM (VALUES ({0}, {1}, '{2}', '{3}'))
														Dummy(InstrumentID, Frequency, EarliestDate, LatestDate)) S
                                                ON
                                                    T.InstrumentID = S.InstrumentID AND T.Frequency = S.Frequency
                                                WHEN NOT MATCHED THEN
                                                    INSERT (InstrumentID, Frequency, EarliestDate, LatestDate)
                                                    VALUES (InstrumentID, Frequency, EarliestDate, LatestDate)
                                                WHEN MATCHED THEN
                                                    UPDATE SET
                                                        T.EarliestDate = (SELECT MIN(mydate) FROM (VALUES (T.EarliestDate), (S.EarliestDate)) AS AllDates(mydate)),
                                                        T.LatestDate = (SELECT MAX(mydate) FROM (VALUES (T.LatestDate), (S.LatestDate)) AS AllDates(mydate));",
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
                    Log(LogLevel.Error, "SQL Server query error: " + ex.Message);
                }

                Log(LogLevel.Info, string.Format(
                    "Saved {0} data points of {1} @ {2} to local storage. {3} {4}",
                    data.Count,
                    instrument.Symbol,
                    Enum.GetName(typeof(BarSize), frequency),
                    overwrite ? "Overwrite" : "NoOverwrite",
                    adjust ? "Adjust" : "NoAdjust"));
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
        /// <param name="data"></param>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        /// <param name="overwrite"></param>
        public void AddDataAsync(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool overwrite = false)
        {
            AddData(data, instrument, frequency, overwrite);
        }

        public void AddDataAsync(OHLCBar data, Instrument instrument, BarSize frequency, bool overwrite = false)
        {
            AddData(new List<OHLCBar> { data }, instrument, frequency, overwrite);
        }

        public void UpdateData(List<OHLCBar> data, Instrument instrument, BarSize frequency, bool adjust = false)
        {
            AddData(data, instrument, frequency, true, adjust);
        }

        public void DeleteAllInstrumentData(Instrument instrument)
        {
            SqlConnection connection;
            if (!TryConnect(out connection))
                throw new Exception("Could not connect to database");

            using (var cmd = new SqlCommand("", connection))
            {
                cmd.CommandText = string.Format("DELETE FROM data WHERE InstrumentID = {0}", instrument.ID);
                cmd.ExecuteNonQuery();
                cmd.CommandText = string.Format("DELETE FROM instrumentinfo WHERE InstrumentID = {0}", instrument.ID);
                cmd.ExecuteNonQuery();
            }

            Log(LogLevel.Info, string.Format("Deleted all data for instrument {0}", instrument));

            connection.Close();
        }

        public void DeleteData(Instrument instrument, BarSize frequency)
        {
            SqlConnection connection;
            if (!TryConnect(out connection))
                throw new Exception("Could not connect to database");

            using (var cmd = new SqlCommand("", connection))
            {
                cmd.CommandText = string.Format("DELETE FROM data WHERE InstrumentID = {0} AND Frequency = {1}", instrument.ID, (int)frequency);
                cmd.ExecuteNonQuery();
                cmd.CommandText = string.Format("DELETE FROM instrumentinfo WHERE InstrumentID = {0} AND Frequency = {1}", instrument.ID, (int)frequency);
                cmd.ExecuteNonQuery();
            }

            Log(LogLevel.Info, string.Format("Deleted all {0} data for instrument {1}", frequency, instrument));

            connection.Close();
        }

        public void DeleteData(Instrument instrument, BarSize frequency, List<OHLCBar> bars)
        {
            SqlConnection connection;
            if (!TryConnect(out connection))
                throw new Exception("Could not connect to database");

            using (var cmd = new SqlCommand("", connection))
            {
                var sb = new StringBuilder();
                sb.Append("BEGIN TRAN T1;");
                for (int i = 0; i < bars.Count; i++)
                {
                    sb.AppendFormat("DELETE FROM data WHERE InstrumentID = {0} AND Frequency = {1} AND DT = '{2}';",
                        instrument.ID,
                        (int)frequency,
                        frequency < BarSize.OneDay
                        ? bars[i].DT.ToString("yyyy-MM-dd HH:mm:ss.fff")
                        : bars[i].DT.ToString("yyyy-MM-dd")); //for frequencies greater than a day, we don't care about time
                }
                sb.Append("COMMIT TRAN T1;");

                cmd.CommandText = sb.ToString();
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
                        cmd.CommandText = string.Format(
                            @"UPDATE instrumentinfo
	                            SET
		                            EarliestDate = (SELECT MIN(DT) FROM data WHERE InstrumentID = {0} AND Frequency = {1}),
		                            LatestDate = (SELECT MAX(DT) FROM data WHERE InstrumentID = {0} AND Frequency = {1})
	                            WHERE
		                            InstrumentID = {0} AND Frequency = {1}", instrument.ID, (int)frequency);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            Log(LogLevel.Info, string.Format("Deleted {0} {1} bars for instrument {2}", bars.Count, frequency, instrument));

            connection.Close();
        }

        public List<StoredDataInfo> GetStorageInfo(int instrumentID)
        {
            SqlConnection connection;
            if (!TryConnect(out connection))
                throw new Exception("Could not connect to database");

            var instrumentInfos = new List<StoredDataInfo>();

            using (var cmd = new SqlCommand("", connection))
            {
                cmd.CommandText = "SELECT * FROM instrumentinfo WHERE InstrumentID = " + instrumentID;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var info = new StoredDataInfo
                        {
                            InstrumentID = instrumentID,
                            Frequency = (BarSize)reader.GetInt32(1),
                            EarliestDate = reader.GetDateTime(2),
                            LatestDate = reader.GetDateTime(3)
                        };
                        instrumentInfos.Add(info);
                    }
                }
            }
            connection.Close();

            return instrumentInfos;
        }

        public StoredDataInfo GetStorageInfo(int instrumentID, BarSize frequency)
        {
            SqlConnection connection;
            if (!TryConnect(out connection))
                throw new Exception("Could not connect to database");

            var instrumentInfo = new StoredDataInfo();

            using (var cmd = new SqlCommand("", connection))
            {
                cmd.CommandText = string.Format("SELECT * FROM instrumentinfo WHERE InstrumentID = {0} AND Frequency = {1}",
                    instrumentID,
                    (int)frequency);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        instrumentInfo.InstrumentID = instrumentID;
                        instrumentInfo.Frequency = (BarSize)reader.GetInt32(1);
                        instrumentInfo.EarliestDate = reader.GetDateTime(2);
                        instrumentInfo.LatestDate = reader.GetDateTime(3);
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

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;

        public event EventHandler<ErrorArgs> Error;

        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

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

        /// <summary>
        /// Add a message to the log.
        ///</summary>
        private void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_connectionStatusUpdateTimer != null)
            {
                _connectionStatusUpdateTimer.Dispose();
                _connectionStatusUpdateTimer = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

#pragma warning restore 67