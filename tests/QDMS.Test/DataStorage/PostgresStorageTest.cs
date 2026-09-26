// -----------------------------------------------------------------------
// <copyright file="PostgresStorageTest.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NUnit.Framework;
using QDMS;
using QDMS.Server.DataStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QDMSTest.DataStorage
{
    [TestFixture]
    public class PostgresStorageTest
    {
        private PostgresStorage _storage;
        private readonly Instrument _instrument = new Instrument { ID = 1, Symbol = "SPY" };

        [SetUp]
        public async Task SetUp()
        {
            _storage = new PostgresStorage(await PostgresFixture.CreateDatabase());
        }

        [TearDown]
        public void TearDown()
        {
            _storage.Dispose();
        }

        private static List<OHLCBar> DailyBars(DateTime start, int count, decimal close = 100m) =>
            Enumerable.Range(0, count)
                .Select(i => new OHLCBar
                {
                    DT = start.AddDays(i).AddHours(16), //the time should get dropped for daily bars
                    Open = close, High = close + 1, Low = close - 1, Close = close,
                    Volume = 1000 + i
                })
                .ToList();

        [Test]
        public void AddedDataCanBeReadBack()
        {
            _storage.AddData(DailyBars(new DateTime(2020, 1, 1), 10), _instrument, BarSize.OneDay);

            var data = _storage.GetData(_instrument, new DateTime(2020, 1, 3), new DateTime(2020, 1, 5));

            Assert.That(data.Select(x => x.DT), Is.EqualTo(new[] { new DateTime(2020, 1, 3), new DateTime(2020, 1, 4), new DateTime(2020, 1, 5) }));
            Assert.That(data[0].Close, Is.EqualTo(100m));
            Assert.That(data[0].Volume, Is.EqualTo(1002));
        }

        [Test]
        public void ExistingBarsAreKeptWithoutOverwriteAndReplacedWithIt()
        {
            _storage.AddData(DailyBars(new DateTime(2020, 1, 1), 3, close: 100m), _instrument, BarSize.OneDay);

            _storage.AddData(DailyBars(new DateTime(2020, 1, 1), 3, close: 200m), _instrument, BarSize.OneDay, overwrite: false);
            var afterNoOverwrite = _storage.GetData(_instrument, new DateTime(2020, 1, 1), new DateTime(2020, 1, 3));

            _storage.AddData(DailyBars(new DateTime(2020, 1, 1), 3, close: 300m), _instrument, BarSize.OneDay, overwrite: true);
            var afterOverwrite = _storage.GetData(_instrument, new DateTime(2020, 1, 1), new DateTime(2020, 1, 3));

            Assert.That(afterNoOverwrite.Select(x => x.Close), Is.All.EqualTo(100m));
            Assert.That(afterOverwrite.Select(x => x.Close), Is.All.EqualTo(300m));
            Assert.That(afterOverwrite, Has.Count.EqualTo(3));
        }

        [Test]
        public void DuplicateBarsInOneBatchDontFail()
        {
            var bars = DailyBars(new DateTime(2020, 1, 1), 2);
            bars.Add(DailyBars(new DateTime(2020, 1, 1), 1, close: 150m)[0]);

            _storage.AddData(bars, _instrument, BarSize.OneDay);

            var data = _storage.GetData(_instrument, new DateTime(2020, 1, 1), new DateTime(2020, 1, 2));
            Assert.That(data, Has.Count.EqualTo(2));
            Assert.That(data[0].Close, Is.EqualTo(150m), "last duplicate should win");
        }

        [Test]
        public void StorageInfoTracksTheStoredRange()
        {
            _storage.AddData(DailyBars(new DateTime(2020, 1, 10), 5), _instrument, BarSize.OneDay);
            _storage.AddData(DailyBars(new DateTime(2020, 1, 1), 3), _instrument, BarSize.OneDay);
            _storage.AddData(DailyBars(new DateTime(2020, 1, 1), 3), _instrument, BarSize.OneHour);

            var info = _storage.GetStorageInfo(_instrument.ID.Value, BarSize.OneDay);

            Assert.That(info.EarliestDate, Is.EqualTo(new DateTime(2020, 1, 1)));
            Assert.That(info.LatestDate, Is.EqualTo(new DateTime(2020, 1, 14)));
            Assert.That(_storage.GetStorageInfo(_instrument.ID.Value), Has.Count.EqualTo(2));
        }

        [Test]
        public void DividendsProduceAdjustedPrices()
        {
            var bars = DailyBars(new DateTime(2020, 1, 1), 5);
            bars[3].Dividend = 1m;

            _storage.AddData(bars, _instrument, BarSize.OneDay);

            var data = _storage.GetData(_instrument, new DateTime(2020, 1, 1), new DateTime(2020, 1, 5));
            Assert.That(data.Take(3).Select(x => x.AdjClose), Is.All.LessThan(100m), "bars before the ex-date are adjusted down");
            Assert.That(data.Skip(3).Select(x => x.AdjClose), Is.All.EqualTo(100m));
        }

        [Test]
        public void DeletingSomeBarsShrinksTheStoredRange()
        {
            var bars = DailyBars(new DateTime(2020, 1, 1), 5);
            _storage.AddData(bars, _instrument, BarSize.OneDay);

            _storage.DeleteData(_instrument, BarSize.OneDay, bars.Take(2).ToList());

            var info = _storage.GetStorageInfo(_instrument.ID.Value, BarSize.OneDay);
            Assert.That(info.EarliestDate, Is.EqualTo(new DateTime(2020, 1, 3)));
            Assert.That(_storage.GetData(_instrument, new DateTime(2020, 1, 1), new DateTime(2020, 1, 5)), Has.Count.EqualTo(3));
        }

        [Test]
        public void DeletingAllBarsRemovesTheStorageInfo()
        {
            var bars = DailyBars(new DateTime(2020, 1, 1), 3);
            _storage.AddData(bars, _instrument, BarSize.OneDay);

            _storage.DeleteData(_instrument, BarSize.OneDay, bars);

            Assert.That(_storage.GetStorageInfo(_instrument.ID.Value, BarSize.OneDay), Is.Null);
        }

        [Test]
        public void DeleteAllInstrumentDataRemovesEverything()
        {
            _storage.AddData(DailyBars(new DateTime(2020, 1, 1), 3), _instrument, BarSize.OneDay);
            _storage.AddData(DailyBars(new DateTime(2020, 1, 1), 3), _instrument, BarSize.OneHour);

            _storage.DeleteAllInstrumentData(_instrument);

            Assert.That(_storage.GetStorageInfo(_instrument.ID.Value), Is.Empty);
            Assert.That(_storage.GetData(_instrument, new DateTime(2020, 1, 1), new DateTime(2020, 1, 5)), Is.Empty);
        }
    }
}
