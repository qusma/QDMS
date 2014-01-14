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
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class RTHFilterTest
    {
        [Test]
        public void FiltersDataOutsideRTH()
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

            var filteredData = RTHFilter.Filter(data, sessions);

            var startCutoff = new DateTime(2014, 1, 13, 8, 0, 0);
            var endingCutoff = new DateTime(2014, 1, 13, 16, 0, 0);

            Assert.AreEqual(0, filteredData.Count(x => x.DT < startCutoff));
            Assert.AreEqual(0, filteredData.Count(x => x.DT > endingCutoff));
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

            var filteredData = RTHFilter.Filter(data, sessions);

            var startCutoff = new DateTime(2014, 1, 13, 8, 0, 0);
            var endingCutoff = new DateTime(2014, 1, 14, 16, 0, 0);

            Assert.AreEqual(0, filteredData.Count(x => x.DT < startCutoff));
            Assert.AreEqual(0, filteredData.Count(x => x.DT > endingCutoff));
            Assert.AreEqual(data.Count - data.Count(x => x.DT < startCutoff) - data.Count(x => x.DT > endingCutoff), filteredData.Count);
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

            var filteredData = RTHFilter.Filter(data, sessions);

            var startCutoff1 = new DateTime(2014, 1, 13, 8, 0, 0);
            var endingCutoff1 = new DateTime(2014, 1, 14, 11, 0, 0);
            var startCutoff2 = new DateTime(2014, 1, 13, 14, 0, 0);
            var endingCutoff2 = new DateTime(2014, 1, 14, 18, 0, 0);

            Assert.AreEqual(0, filteredData.Count(x => x.DT < startCutoff1));
            Assert.AreEqual(0, filteredData.Count(x => x.DT > endingCutoff1 && x.DT < startCutoff2));
            Assert.AreEqual(0, filteredData.Count(x => x.DT > endingCutoff2));

            Assert.AreEqual(data.Count 
                - data.Count(x => x.DT < startCutoff1) 
                - data.Count(x => x.DT > endingCutoff1 && x.DT < startCutoff2)
                - data.Count(x => x.DT > endingCutoff2)
                , filteredData.Count);
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