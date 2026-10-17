// -----------------------------------------------------------------------
// <copyright file="PostgresStorage.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NLog;
using Npgsql;
using NpgsqlTypes;
using QDMS.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;

#pragma warning disable 67

namespace QDMS.Server.DataStorage
{
    /// <summary>
    /// Local bar storage in PostgreSQL. The schema (tables "bars" and "instrument_info")
    /// is created by the DataDBContext migrations in QDMS.Dal.
    /// </summary>
    public class PostgresStorage : IDataStorage
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly NpgsqlDataSource _dataSource;
        private Timer _connectionStatusUpdateTimer;
        private bool _connected;

        public PostgresStorage(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            _dataSource = NpgsqlDataSource.Create(connectionString);
            _connectionStatusUpdateTimer = new Timer(5000);
            _connectionStatusUpdateTimer.Elapsed += (s, e) => UpdateConnectionStatus();
            _connectionStatusUpdateTimer.Start();
        }

        public string Name => "Local Storage";

        public bool Connected
        {
            get => _connected;
            private set
            {
                if (_connected == value) return;
                _connected = value;
                OnPropertyChanged();
            }
        }

        public void Connect()
        {
        }

        public void Disconnect()
        {
        }

        private void UpdateConnectionStatus()
        {
            try
            {
                using var cmd = _dataSource.CreateCommand("SELECT 1");
                cmd.ExecuteScalar();
                Connected = true;
            }
            catch (Exception)
            {
                Connected = false;
            }
        }

        public List<OHLCBar> GetData(Instrument instrument, DateTime startDate, DateTime endDate, BarSize frequency = BarSize.OneDay)
        {
            using var cmd = _dataSource.CreateCommand(
                @"SELECT dt, open, high, low, close, adj_open, adj_high, adj_low, adj_close,
                         volume, open_interest, dividend, split, dt_open
                  FROM bars
                  WHERE instrument_id = $1 AND frequency = $2 AND dt >= $3 AND dt <= $4
                  ORDER BY dt ASC");
            cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = instrument.ID.Value });
            cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = (int)frequency });
            cmd.Parameters.Add(Timestamp(frequency >= BarSize.OneDay ? startDate.Date : startDate));
            cmd.Parameters.Add(Timestamp(frequency >= BarSize.OneDay ? endDate.Date : endDate));

            var data = new List<OHLCBar>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                data.Add(new OHLCBar
                {
                    DT = reader.GetDateTime(0),
                    Open = reader.GetDecimal(1),
                    High = reader.GetDecimal(2),
                    Low = reader.GetDecimal(3),
                    Close = reader.GetDecimal(4),
                    AdjOpen = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    AdjHigh = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                    AdjLow = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    AdjClose = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                    Volume = reader.IsDBNull(9) ? null : reader.GetInt64(9),
                    OpenInterest = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    Dividend = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
                    Split = reader.IsDBNull(12) ? null : reader.GetDecimal(12),
                    DTOpen = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
                    InstrumentID = instrument.ID.Value,
                    Frequency = frequency
                });
            }

            return data;
        }

        //for IHistoricalDataSource, we just grab data and send it back with the event
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            var data = GetData(request.Instrument, request.StartingDate, request.EndingDate, request.Frequency);
            HistoricalDataArrived?.Invoke(this, new HistoricalDataEventArgs(request, data));
        }

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
                _logger.Error("Local storage: asked to add data of 0 length");
                return;
            }

            bool needsAdjustment = false;
            foreach (var bar in data)
            {
                if (frequency >= BarSize.OneDay)
                {
                    //we don't save the time when saving this stuff to allow flexibility with changing sessions
                    bar.DT = bar.DT.Date;
                    bar.DTOpen = null;
                }

                needsAdjustment |= bar.Dividend.HasValue || bar.Split.HasValue;
            }

            // Duplicates within the batch would make ON CONFLICT fail; the last one wins, as with REPLACE.
            var distinctBars = data
                .GroupBy(x => x.DT)
                .Select(g => g.Last())
                .ToList();

            using (var connection = _dataSource.OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                using (var cmd = new NpgsqlCommand("CREATE TEMP TABLE bars_import (LIKE bars INCLUDING DEFAULTS) ON COMMIT DROP", connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var writer = connection.BeginBinaryImport(
                    @"COPY bars_import (dt, instrument_id, frequency, open, high, low, close, adj_open, adj_high, adj_low, adj_close,
                                        volume, open_interest, dividend, split, dt_open) FROM STDIN (FORMAT BINARY)"))
                {
                    foreach (var bar in distinctBars)
                    {
                        writer.StartRow();
                        writer.Write(Unspecified(bar.DT), NpgsqlDbType.Timestamp);
                        writer.Write(instrument.ID.Value, NpgsqlDbType.Integer);
                        writer.Write((int)frequency, NpgsqlDbType.Integer);
                        writer.Write(bar.Open, NpgsqlDbType.Numeric);
                        writer.Write(bar.High, NpgsqlDbType.Numeric);
                        writer.Write(bar.Low, NpgsqlDbType.Numeric);
                        writer.Write(bar.Close, NpgsqlDbType.Numeric);
                        WriteNullable(writer, bar.AdjOpen, NpgsqlDbType.Numeric);
                        WriteNullable(writer, bar.AdjHigh, NpgsqlDbType.Numeric);
                        WriteNullable(writer, bar.AdjLow, NpgsqlDbType.Numeric);
                        WriteNullable(writer, bar.AdjClose, NpgsqlDbType.Numeric);
                        WriteNullable(writer, bar.Volume, NpgsqlDbType.Bigint);
                        WriteNullable(writer, bar.OpenInterest, NpgsqlDbType.Integer);
                        WriteNullable(writer, bar.Dividend, NpgsqlDbType.Numeric);
                        WriteNullable(writer, bar.Split, NpgsqlDbType.Numeric);
                        WriteNullable(writer, bar.DTOpen.HasValue ? Unspecified(bar.DTOpen.Value) : (DateTime?)null, NpgsqlDbType.Timestamp);
                    }

                    writer.Complete();
                }

                string onConflict = overwrite
                    ? @"ON CONFLICT (instrument_id, frequency, dt) DO UPDATE SET
                            open = EXCLUDED.open, high = EXCLUDED.high, low = EXCLUDED.low, close = EXCLUDED.close,
                            adj_open = EXCLUDED.adj_open, adj_high = EXCLUDED.adj_high, adj_low = EXCLUDED.adj_low,
                            adj_close = EXCLUDED.adj_close, volume = EXCLUDED.volume, open_interest = EXCLUDED.open_interest,
                            dividend = EXCLUDED.dividend, split = EXCLUDED.split, dt_open = EXCLUDED.dt_open"
                    : "ON CONFLICT (instrument_id, frequency, dt) DO NOTHING";

                using (var cmd = new NpgsqlCommand("INSERT INTO bars SELECT * FROM bars_import " + onConflict, connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                //finally update the instrument info
                using (var cmd = new NpgsqlCommand(
                    @"INSERT INTO instrument_info (instrument_id, frequency, earliest_date, latest_date)
                      SELECT $1, $2, MIN(dt), MAX(dt) FROM bars_import
                      ON CONFLICT (instrument_id, frequency) DO UPDATE SET
                          earliest_date = LEAST(instrument_info.earliest_date, EXCLUDED.earliest_date),
                          latest_date = GREATEST(instrument_info.latest_date, EXCLUDED.latest_date)",
                    connection, transaction))
                {
                    cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = instrument.ID.Value });
                    cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = (int)frequency });
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            _logger.Info("Saved {0} data points of {1} @ {2} to local storage. {3} {4}",
                data.Count,
                instrument.Symbol,
                frequency,
                overwrite ? "Overwrite" : "NoOverwrite",
                adjust ? "Adjust" : "NoAdjust");

            //if there were dividends or splits in the data we added,
            //we need to generate adjusted prices
            if (adjust && needsAdjustment && frequency == BarSize.OneDay) //adjustments are nonsensical on any other frequency
                AdjustData(instrument, frequency);
        }

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
            using (var batch = _dataSource.CreateBatch())
            {
                batch.BatchCommands.Add(Command("DELETE FROM bars WHERE instrument_id = $1", instrument.ID.Value));
                batch.BatchCommands.Add(Command("DELETE FROM instrument_info WHERE instrument_id = $1", instrument.ID.Value));
                batch.ExecuteNonQuery();
            }

            _logger.Info("Deleted all data for instrument {0}", instrument);
        }

        public void DeleteData(Instrument instrument, BarSize frequency)
        {
            using (var batch = _dataSource.CreateBatch())
            {
                batch.BatchCommands.Add(Command("DELETE FROM bars WHERE instrument_id = $1 AND frequency = $2", instrument.ID.Value, (int)frequency));
                batch.BatchCommands.Add(Command("DELETE FROM instrument_info WHERE instrument_id = $1 AND frequency = $2", instrument.ID.Value, (int)frequency));
                batch.ExecuteNonQuery();
            }

            _logger.Info("Deleted all {0} data for instrument {1}", frequency, instrument);
        }

        public void DeleteData(Instrument instrument, BarSize frequency, List<OHLCBar> bars)
        {
            //for frequencies of a day or more, we don't care about time
            var dates = bars
                .Select(x => Unspecified(frequency < BarSize.OneDay ? x.DT : x.DT.Date))
                .ToArray();

            using (var connection = _dataSource.OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM bars WHERE instrument_id = $1 AND frequency = $2 AND dt = ANY($3)", connection, transaction))
                {
                    cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = instrument.ID.Value });
                    cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = (int)frequency });
                    cmd.Parameters.Add(new NpgsqlParameter { Value = dates, NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Timestamp });
                    cmd.ExecuteNonQuery();
                }

                //remove the instrument_info row if there's nothing left, otherwise recalculate its range
                using (var cmd = new NpgsqlCommand(
                    @"WITH range AS (SELECT MIN(dt) AS mindt, MAX(dt) AS maxdt FROM bars WHERE instrument_id = $1 AND frequency = $2),
                          upd AS (
                              UPDATE instrument_info SET earliest_date = range.mindt, latest_date = range.maxdt
                              FROM range
                              WHERE range.mindt IS NOT NULL AND instrument_id = $1 AND frequency = $2)
                      DELETE FROM instrument_info
                      WHERE instrument_id = $1 AND frequency = $2 AND (SELECT mindt FROM range) IS NULL",
                    connection, transaction))
                {
                    cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = instrument.ID.Value });
                    cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = (int)frequency });
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            _logger.Info("Deleted {0} {1} bars for instrument {2}", bars.Count, frequency, instrument);
        }

        public List<StoredDataInfo> GetStorageInfo(int instrumentID)
        {
            using var cmd = _dataSource.CreateCommand(
                "SELECT frequency, earliest_date, latest_date FROM instrument_info WHERE instrument_id = $1");
            cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = instrumentID });

            var instrumentInfos = new List<StoredDataInfo>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                instrumentInfos.Add(new StoredDataInfo
                {
                    InstrumentID = instrumentID,
                    Frequency = (BarSize)reader.GetInt32(0),
                    EarliestDate = reader.GetDateTime(1),
                    LatestDate = reader.GetDateTime(2)
                });
            }

            return instrumentInfos;
        }

        public StoredDataInfo GetStorageInfo(int instrumentID, BarSize frequency)
        {
            //return null if nothing is found that matches these criteria
            return GetStorageInfo(instrumentID).FirstOrDefault(x => x.Frequency == frequency);
        }

        private static NpgsqlBatchCommand Command(string sql, params int[] parameters)
        {
            var cmd = new NpgsqlBatchCommand(sql);
            foreach (var p in parameters)
            {
                cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = p });
            }

            return cmd;
        }

        private static NpgsqlParameter Timestamp(DateTime value) =>
            new NpgsqlParameter<DateTime> { TypedValue = Unspecified(value), NpgsqlDbType = NpgsqlDbType.Timestamp };

        private static DateTime Unspecified(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

        private static void WriteNullable<T>(NpgsqlBinaryImporter writer, T? value, NpgsqlDbType type) where T : struct
        {
            if (value.HasValue)
                writer.Write(value.Value, type);
            else
                writer.WriteNull();
        }

        public event EventHandler<ErrorArgs> Error;

        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _connectionStatusUpdateTimer?.Dispose();
            _connectionStatusUpdateTimer = null;
            _dataSource.Dispose();
        }
    }
}
