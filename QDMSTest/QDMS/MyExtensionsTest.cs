// -----------------------------------------------------------------------
// <copyright file="MyExtensionsTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QDMS;

namespace QDMSTest
{
    [TestFixture]
    public class MyExtensionsTest
    {
        private List<OHLCBar> _ohlcData;

        [SetUp]
        public void SetUp()
        {
            _ohlcData = new List<OHLCBar>
            {
                new OHLCBar {DT = new DateTime(2000, 1, 1), Open = 100, High = 105, Low = 95, Close = 100},
                new OHLCBar {DT = new DateTime(2000, 1, 2), Open = 99, High = 107, Low = 90, Close = 99},
                new OHLCBar {DT = new DateTime(2000, 1, 3), Open = 57, High = 95, Low = 50, Close = 50},
                new OHLCBar {DT = new DateTime(2000, 1, 4), Open = 100, High = 105, Low = 95, Close = 100},
                new OHLCBar {DT = new DateTime(2000, 1, 5), Open = 100, High = 105, Low = 95, Close = 100},
                new OHLCBar {DT = new DateTime(2000, 1, 6), Open = 100, High = 105, Low = 95, Close = 100},
                new OHLCBar {DT = new DateTime(2000, 1, 7), Open = 100, High = 105, Low = 95, Close = 100},
                new OHLCBar {DT = new DateTime(2000, 1, 8), Open = 200, High = 300, Low = 50, Close = 100}
            };
        }

        [Test]
        public void IndexOfCorrectlyThrowsNullException()
        {
            var list = new List<double>();
            Assert.Throws<ArgumentNullException>(() => list.IndexOf(null));
        }

        [Test]
        public void RIndexOfeturnsMinusOneIfNotFound()
        {
            int index = _ohlcData.IndexOf(x => x.DT > new DateTime(2001, 1, 1));
            Assert.AreEqual(-1, index);
        }

        [Test]
        public void IndexOfReturnsCorrectIndex()
        {
            int index = _ohlcData.IndexOf(x => x.Open == 100);
            Assert.AreEqual(0, index);

            index = _ohlcData.IndexOf(x => x.Open >= 200);
            Assert.AreEqual(7, index);
        }

        [Test]
        public void MostFrequentThrowsExceptionWhenCalledOnEmptyList()
        {
            var emptyList = new List<int>();
            Assert.Throws<Exception>(() => emptyList.MostFrequent());
        }

        [Test]
        public void MostFrequentReturnsTheFirstEncounteredMostFrequentElement()
        {
            var data = new List<int> { 5, 6, 4, 1, 2, 5, 6, 5, 6, 9, 0, 0, 5, 6, 0, 3, 4, 7, 8 };
            Assert.AreEqual(5, data.MostFrequent());
        }

        [Test]
        public void DistinctReturnsDistinctValues()
        {
            var data = new List<int> { 5, 6, 6, 5, 4, 1, 2 };
            data = data.Distinct((x, y) => x == y).ToList();

            var targetData = new List<int> { 5, 6, 4, 1, 2 };

            Assert.AreEqual(targetData.Count, data.Count);
            for (int i = 0; i < targetData.Count; i++)
            {
                Assert.AreEqual(targetData[i], data[i]);
            }
        }

        [Test]
        public void DistinctReturnsDistinctValuesWhenUsedWithObjectsUsingComparisonExpression()
        {
            var data = new List<OHLCBar>
            {
                new OHLCBar {DT = new DateTime(2000, 1, 1), Open = 100, High = 105, Low = 95, Close = 100},
                new OHLCBar {DT = new DateTime(2000, 1, 2), Open = 99, High = 107, Low = 90, Close = 99},
                new OHLCBar {DT = new DateTime(2000, 1, 1), Open = 100, High = 105, Low = 95, Close = 100},
                new OHLCBar {DT = new DateTime(2000, 1, 2), Open = 99, High = 107, Low = 90, Close = 99},
            };

            data = data.Distinct((x, y) => x.DT == y.DT).ToList();
            Assert.AreEqual(2, data.Count);
        }

        [Test]
        public void DistinctReturnsDistinctValuesWhenUsedWithObjectsUsingHashExpression()
        {
            var data = new List<OHLCBar>
            {
                new OHLCBar {DT = new DateTime(2000, 1, 1), Open = 100, High = 105, Low = 95, Close = 100},
                new OHLCBar {DT = new DateTime(2000, 1, 2), Open = 99, High = 107, Low = 90, Close = 99},
                new OHLCBar {DT = new DateTime(2000, 1, 1), Open = 100, High = 105, Low = 95, Close = 100},
                new OHLCBar {DT = new DateTime(2000, 1, 2), Open = 99, High = 107, Low = 90, Close = 99},
            };

            data = data.Distinct(x => x.DT.GetHashCode()).ToList();
            Assert.AreEqual(2, data.Count);
        }

        [Test]
        public void CountDecimalPlacesReturnsTheCorrectNumber()
        {
            decimal val = 5.123m;
            Assert.AreEqual(3, val.CountDecimalPlaces());
        }

        [Test]
        public void CountDecimalPlacesReturnsTheCorrectNumberForNegativeInputs()
        {
            decimal val = -5.123m;
            Assert.AreEqual(3, val.CountDecimalPlaces());
        }

        [Test]
        public void CountDecimalPlacesReturnsTheCorrectNumberForSmallNumbers()
        {
            decimal val = 0.0000001m;
            Assert.AreEqual(7, val.CountDecimalPlaces());
        }

        [Test]
        public void CountDecimalPlacesReturnsTheCorrectNumberWithTrailingZeros()
        {
            decimal val = 5.123000m;
            Assert.AreEqual(3, val.CountDecimalPlaces());
        }

        [Test]
        public void RemoveAllRemovesCorrectItemsFromCollection()
        {
            var data = new List<int> { 1, 3, 5, 4, 6, 8 };
            data.RemoveAll(x => x % 2 == 0);
            Assert.AreEqual(3, data.Count);
            Assert.Contains(1, data);
            Assert.Contains(3, data);
            Assert.Contains(5, data);
        }

        [Test]
        public void SessionEndTimesByDayReturnsCorrectSessionsForGlobexSessions()
        {
            var inst = new Instrument();
            inst.Sessions = GetGlobexSessions();
            Dictionary<int, TimeSpan> result = inst.SessionEndTimesByDay();
            Assert.AreEqual(new TimeSpan(16, 30, 0), result[(int)DayOfTheWeek.Monday]);
            Assert.AreEqual(new TimeSpan(16, 30, 0), result[(int)DayOfTheWeek.Tuesday]);
            Assert.AreEqual(new TimeSpan(16, 30, 0), result[(int)DayOfTheWeek.Wednesday]);
            Assert.AreEqual(new TimeSpan(16, 30, 0), result[(int)DayOfTheWeek.Thursday]);
            Assert.AreEqual(new TimeSpan(15, 15, 0), result[(int)DayOfTheWeek.Friday]);
            Assert.IsFalse(result.ContainsKey((int)DayOfTheWeek.Sunday));
        }

        [Test]
        public void SessionStartTimesByDayReturnsCorrectSessionsForGlobexSessions()
        {
            var inst = new Instrument();
            inst.Sessions = GetGlobexSessions();
            Dictionary<int, InstrumentSession> result = inst.SessionStartTimesByDay();
            Assert.AreEqual(DayOfTheWeek.Sunday, result[(int)DayOfTheWeek.Monday].OpeningDay);
            Assert.AreEqual(new TimeSpan(17, 00, 0), result[(int)DayOfTheWeek.Monday].OpeningTime);

            Assert.AreEqual(DayOfTheWeek.Monday, result[(int)DayOfTheWeek.Tuesday].OpeningDay);
            Assert.AreEqual(new TimeSpan(17, 00, 0), result[(int)DayOfTheWeek.Tuesday].OpeningTime);

            Assert.AreEqual(DayOfTheWeek.Tuesday, result[(int)DayOfTheWeek.Wednesday].OpeningDay);
            Assert.AreEqual(new TimeSpan(17, 00, 0), result[(int)DayOfTheWeek.Wednesday].OpeningTime);

            Assert.AreEqual(DayOfTheWeek.Wednesday, result[(int)DayOfTheWeek.Thursday].OpeningDay);
            Assert.AreEqual(new TimeSpan(17, 00, 0), result[(int)DayOfTheWeek.Thursday].OpeningTime);

            Assert.AreEqual(DayOfTheWeek.Thursday, result[(int)DayOfTheWeek.Friday].OpeningDay);
            Assert.AreEqual(new TimeSpan(17, 00, 0), result[(int)DayOfTheWeek.Friday].OpeningTime);

            Assert.IsFalse(result.ContainsKey((int)DayOfTheWeek.Sunday));
        }

        private List<InstrumentSession> GetGlobexSessions()
        {
            return new List<InstrumentSession>
            {
                new InstrumentSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                },
                new InstrumentSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                },
                new InstrumentSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                },
                new InstrumentSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                },
                new InstrumentSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                },
                new InstrumentSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Thursday,
                },
                new InstrumentSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                },
                new InstrumentSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Friday,
                },
                new InstrumentSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Sunday,
                    ClosingDay = DayOfTheWeek.Monday,
                }
            };
        }
    }
}