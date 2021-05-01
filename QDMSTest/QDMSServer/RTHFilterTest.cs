// -----------------------------------------------------------------------
// <copyright file="RTHFilterTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using QDMS;
using QDMSApp;

namespace QDMSTest
{
    [TestFixture]
    public class RTHFilterTest
    {
        [Test]
        public void FiltersDataOutsideRTHWithSingleDaySession()
        {
            var sessions = new List<InstrumentSession>
            {
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Monday ,
                    ClosingDay = DayOfTheWeek.Monday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true
                }
            };

            var data = GetDataBetween(new DateTime(2014, 1, 13, 0, 0, 0), new DateTime(2014, 1, 13, 23, 0, 0));

            RTHFilter.Filter(data, sessions);

            var startCutoff = new DateTime(2014, 1, 13, 8, 0, 0);
            var endingCutoff = new DateTime(2014, 1, 13, 16, 0, 0);

            Assert.AreEqual(0, data.Count(x => x.DT <= startCutoff));
            Assert.AreEqual(0, data.Count(x => x.DT > endingCutoff));
        }

        [Test]
        public void FiltersDataOutsideRTHWhenSessionSpansMultipleDays()
        {
            var sessions = new List<InstrumentSession>
            {
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Monday ,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true
                }
            };

            var data = GetDataBetween(new DateTime(2014, 1, 13, 0, 0, 0), new DateTime(2014, 1, 14, 23, 0, 0));
            var unfilteredData = new List<OHLCBar>(data);

            RTHFilter.Filter(data, sessions);

            var startCutoff = new DateTime(2014, 1, 13, 8, 0, 0);
            var endingCutoff = new DateTime(2014, 1, 14, 16, 0, 0);

            Assert.AreEqual(0, data.Count(x => x.DT < startCutoff));
            Assert.AreEqual(0, data.Count(x => x.DT > endingCutoff));
            Assert.AreEqual(
                unfilteredData.Count 
                - unfilteredData.Count(x => x.DT <= startCutoff) 
                - unfilteredData.Count(x => x.DT > endingCutoff), 
                data.Count);
        }
        
        [Test]
        public void FiltersDataOutsideRTHWithMultipleSessionsInOneDay()
        {
            var sessions = new List<InstrumentSession>
            {
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Monday ,
                    ClosingDay = DayOfTheWeek.Monday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(11, 0, 0)
                },
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Monday ,
                    ClosingDay = DayOfTheWeek.Monday,
                    OpeningTime = new TimeSpan(14, 0, 0),
                    ClosingTime = new TimeSpan(18, 0, 0)
                }
            };

            var data = GetDataBetween(new DateTime(2014, 1, 13, 0, 0, 0), new DateTime(2014, 1, 14, 23, 0, 0));
            var unfilteredData = new List<OHLCBar>(data);

            RTHFilter.Filter(data, sessions);

            var startCutoff1 = new DateTime(2014, 1, 13, 8, 0, 0);
            var endingCutoff1 = new DateTime(2014, 1, 13, 11, 0, 0);
            var startCutoff2 = new DateTime(2014, 1, 13, 14, 0, 0);
            var endingCutoff2 = new DateTime(2014, 1, 13, 18, 0, 0);

            Assert.AreEqual(0, data.Count(x => x.DT <= startCutoff1));
            Assert.AreEqual(0, data.Count(x => x.DT > endingCutoff1 && x.DT <= startCutoff2));
            Assert.AreEqual(0, data.Count(x => x.DT > endingCutoff2));

            Assert.AreEqual(
                unfilteredData.Count
                - unfilteredData.Count(x => x.DT <= startCutoff1)
                - unfilteredData.Count(x => x.DT > endingCutoff1 && x.DT <= startCutoff2)
                - unfilteredData.Count(x => x.DT > endingCutoff2)
                , data.Count);
        }

        [Test]
        public void FiltersDataOutsideRTHWhenSessionSpansMultipleWeeks()
        {
            var sessions = new List<InstrumentSession>
            {
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Sunday ,
                    ClosingDay = DayOfTheWeek.Monday,
                    OpeningTime = new TimeSpan(16, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true
                }
            };

            var data = GetDataBetween(new DateTime(2014, 3, 30, 0, 0, 0), new DateTime(2014, 3, 31, 23, 0, 0));
            var unfilteredData = new List<OHLCBar>(data);
            RTHFilter.Filter(data, sessions);

            var startCutoff = new DateTime(2014, 3, 30, 16, 0, 0);
            var endingCutoff = new DateTime(2014, 3, 31, 16, 0, 0);

            Assert.AreEqual(0, data.Count(x => x.DT < startCutoff));
            Assert.AreEqual(0, data.Count(x => x.DT > endingCutoff));
            Assert.AreEqual(
                unfilteredData.Count
                - unfilteredData.Count(x => x.DT <= startCutoff)
                - unfilteredData.Count(x => x.DT > endingCutoff),
                data.Count);
        }

        [Test]
        public void FiltersDataCorrectlyWhenThereAreNoBarsBetweenSessions()
        {
            var sessions = new List<InstrumentSession>
            {
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Monday ,
                    ClosingDay = DayOfTheWeek.Monday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(11, 0, 0)
                },
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Monday ,
                    ClosingDay = DayOfTheWeek.Monday,
                    OpeningTime = new TimeSpan(11, 30, 0),
                    ClosingTime = new TimeSpan(18, 0, 0)
                }
            };

            var data = GetDataBetween(new DateTime(2014, 1, 13, 0, 0, 0), new DateTime(2014, 1, 13, 23, 0, 0));
            var unfilteredData = new List<OHLCBar>(data);
            RTHFilter.Filter(data, sessions);

            var startCutoff1 = new DateTime(2014, 1, 13, 8, 0, 0);
            var endingCutoff1 = new DateTime(2014, 1, 13, 11, 0, 0);
            var startCutoff2 = new DateTime(2014, 1, 13, 11, 30, 0);
            var endingCutoff2 = new DateTime(2014, 1, 13, 18, 0, 0);

            Assert.AreEqual(0, data.Count(x => x.DT < startCutoff1));
            Assert.AreEqual(0, data.Count(x => x.DT > endingCutoff1 && x.DT < startCutoff2));
            Assert.AreEqual(0, data.Count(x => x.DT > endingCutoff2));

            Assert.AreEqual(
                unfilteredData.Count
                - unfilteredData.Count(x => x.DT <= startCutoff1)
                - unfilteredData.Count(x => x.DT > endingCutoff1 && x.DT <= startCutoff2)
                - unfilteredData.Count(x => x.DT > endingCutoff2)
                , data.Count);
        }

        [Test]
        public void FiltersCorrectlyWhenDataGapsOverMultipleSessionsIntoASession()
        {
            var sessions = new List<InstrumentSession>
            {
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Monday ,
                    ClosingDay = DayOfTheWeek.Monday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(12, 0, 0)
                },
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Tuesday ,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(12, 0, 0)
                },
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Wednesday ,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(12, 0, 0)
                },
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Thursday ,
                    ClosingDay = DayOfTheWeek.Thursday,
                    OpeningTime = new TimeSpan(12, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0)
                }
            };

            // the data starts right before monday, but then jumps right into the wednesday session
            // 31/3: Monday
            var data = GetDataBetween(new DateTime(2014, 3, 31, 0, 0, 0), new DateTime(2014, 3, 31, 11, 0, 0));
            data.AddRange(GetDataBetween(new DateTime(2014, 4, 2, 9, 0, 0), new DateTime(2014, 4, 3, 20, 0, 0)));

            var unfilteredData = new List<OHLCBar>(data);

            RTHFilter.Filter(data, sessions);
            var startCutoff1 = new DateTime(2014, 3, 31, 8, 0, 0);
            var endingCutoff1 = new DateTime(2014, 4, 2, 12, 0, 0);
            var startCutoff2 = new DateTime(2014, 4, 3, 12, 0, 0);
            var endingCutoff2 = new DateTime(2014, 4, 3, 16, 0, 0);

            Assert.AreEqual(unfilteredData.Count
                - unfilteredData.Count(x => x.DT <= startCutoff1)
                - unfilteredData.Count(x => x.DT > endingCutoff1 && x.DT <= startCutoff2)
                - unfilteredData.Count(x => x.DT > endingCutoff2)
                , data.Count);
        }

        [Test]
        public void FiltersCorrectlyWhenDataGapsOverMultipleSessionsOutsideASession()
        {
            var sessions = new List<InstrumentSession>
            {
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Monday ,
                    ClosingDay = DayOfTheWeek.Monday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(12, 0, 0)
                },
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Tuesday ,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(12, 0, 0)
                },
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Wednesday ,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(12, 0, 0)
                },
                new InstrumentSession
                { 
                    OpeningDay = DayOfTheWeek.Thursday ,
                    ClosingDay = DayOfTheWeek.Thursday,
                    OpeningTime = new TimeSpan(12, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0)
                }
            };

            // the data starts right before monday, but then jumps to before the wednesday session
            // 31/3: Monday
            var data = GetDataBetween(new DateTime(2014, 3, 31, 0, 0, 0), new DateTime(2014, 3, 31, 11, 0, 0));
            data.AddRange(GetDataBetween(new DateTime(2014, 4, 2, 4, 0, 0), new DateTime(2014, 4, 3, 20, 0, 0)));

            var unfilteredData = new List<OHLCBar>(data);

            RTHFilter.Filter(data, sessions);
            var startCutoff1 = new DateTime(2014, 3, 31, 8, 0, 0);
            var endingCutoff1 = new DateTime(2014, 3, 31, 12, 0, 0);
            var startCutoff2 = new DateTime(2014, 4, 2, 8, 0, 0);
            var endingCutoff2 = new DateTime(2014, 4, 2, 12, 0, 0);
            var startCutoff3 = new DateTime(2014, 4, 3, 12, 0, 0);
            var endingCutoff3 = new DateTime(2014, 4, 3, 16, 0, 0);

            Assert.AreEqual(unfilteredData.Count
                - unfilteredData.Count(x => x.DT <= startCutoff1)
                - unfilteredData.Count(x => x.DT > endingCutoff1 && x.DT <= startCutoff2)
                - unfilteredData.Count(x => x.DT > endingCutoff2 && x.DT <= startCutoff3)
                - unfilteredData.Count(x => x.DT > endingCutoff3)
                , data.Count);
        }

        [Test]
        public void InSessionReturnsTrueWhenInsideOneDaySession()
        {
            var session = new InstrumentSession
                {
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(15, 0, 0)
                };

            //Monday, March 31
            var dt = new DateTime(2014, 3, 31, 12, 30, 5);

            Assert.IsTrue(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsFalseWhenAfterOneDaySessionOnSameDay()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                ClosingDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Monday, March 31
            var dt = new DateTime(2014, 3, 31, 16, 30, 5);

            Assert.IsFalse(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsFalseWhenBeforeOneDaySessionOnSameDay()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                ClosingDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Monday, March 31
            var dt = new DateTime(2014, 3, 31, 5, 30, 5);

            Assert.IsFalse(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsFalseWhenAfterOneDaySessionOnNextDay()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                ClosingDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Tuesday, April 1
            var dt = new DateTime(2014, 4, 1, 12, 30, 5);

            Assert.IsFalse(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsFalseWhenBeforeOneDaySessionOnPreviousDay()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                ClosingDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Sunday, March 30
            var dt = new DateTime(2014, 3, 30, 12, 30, 5);

            Assert.IsFalse(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsTrueWhenInsideMultiDaySession()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                ClosingDay = DayOfTheWeek.Tuesday,
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Tuesday, April 1
            var dt = new DateTime(2014, 4, 1, 7, 0, 0);

            Assert.IsTrue(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsFalseWhenAfterMultiDaySession()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                ClosingDay = DayOfTheWeek.Tuesday,
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Wednesday, April 2
            var dt = new DateTime(2014, 4, 2, 7, 0, 0);

            Assert.IsFalse(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsFalseWhenBeforeMultiDaySession()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                ClosingDay = DayOfTheWeek.Tuesday,
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Monday, March 31
            var dt = new DateTime(2014, 3, 31, 7, 0, 0);

            Assert.IsFalse(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsTrueWhenInsideWeekSpanningSessionInFirstWeek()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Sunday,
                ClosingDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(15, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Sunday, March 30
            var dt = new DateTime(2014, 3, 30, 16, 0, 0);

            Assert.IsTrue(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsTrueWhenInsideWeekSpanningSessionInSecondWeek()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Sunday,
                ClosingDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(15, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Monday, March 31
            var dt = new DateTime(2014, 3, 31, 13, 0, 0);

            Assert.IsTrue(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsFalseWhenAfterWeekSpanningDaySession()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Sunday,
                ClosingDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(15, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Tuesday, April 2
            var dt = new DateTime(2014, 4, 2, 13, 0, 0);

            Assert.IsFalse(dt.InSession(session));
        }

        [Test]
        public void InSessionReturnsFalseWhenBeforeWeekSpanningDaySession()
        {
            var session = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Sunday,
                ClosingDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(15, 0, 0),
                ClosingTime = new TimeSpan(15, 0, 0)
            };

            //Sunday, March 30
            var dt = new DateTime(2014, 3, 30, 13, 0, 0);

            Assert.IsFalse(dt.InSession(session));
        }

        private List<OHLCBar> GetDataBetween(DateTime startDate, DateTime endDate)
        {
            var currentDate = startDate;
            var bars = new List<OHLCBar>();

            while (currentDate < endDate)
            {
                bars.Add(new OHLCBar { Open = 100, High = 100, Low = 100, Close = 100, DT = currentDate });
                currentDate = currentDate.AddHours(1);
            }

            return bars;
        }
    }
}