// -----------------------------------------------------------------------
// <copyright file="TimeSeriesTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NUnit.Framework;
using QDMS;

namespace QDMSTest
{
    [TestFixture]
    public class TimeSeriesTest
    {
        private TimeSeries _ts;

        [SetUp]
        public void SetUp()
        {
            var data = new List<OHLCBar>
            {
                new OHLCBar {Open = 29.06m, High =29.06m, Low = 28.65m, Close = 28.65m, Volume = 125, OpenInterest = 885, DT = new DateTime(2012,3,8) },
                new OHLCBar {Open = 28.5m, High =28.86m, Low = 28.5m, Close = 28.6m, Volume = 320, OpenInterest = 1143, DT = new DateTime(2012,3,9) },
                new OHLCBar {Open = 28.7m, High =28.7m, Low = 28.02m, Close = 28.12m, Volume = 40, OpenInterest = 1153, DT = new DateTime(2012,3,12) },
                new OHLCBar {Open = 28.01m, High =28.05m, Low = 27.55m, Close = 27.55m, Volume = 151, OpenInterest = 1231, DT = new DateTime(2012,3,13) },
                new OHLCBar {Open = 27.44m, High =27.85m, Low = 27.12m, Close = 27.85m, Volume = 228, OpenInterest = 1382, DT = new DateTime(2012,3,14) },
                new OHLCBar {Open = 27.94m, High =28m, Low = 27.4m, Close = 27.55m, Volume = 238, OpenInterest = 1448, DT = new DateTime(2012,3,15) },
                new OHLCBar {Open = 27.65m, High =27.85m, Low = 27.35m, Close = 27.55m, Volume = 222, OpenInterest = 1606, DT = new DateTime(2012,3,16) },
                new OHLCBar {Open = 27.65m, High =27.65m, Low = 27m, Close = 27.05m, Volume = 25, OpenInterest = 1621, DT = new DateTime(2012,3,19) },
                new OHLCBar {Open = 27.45m, High =27.45m, Low = 26.85m, Close = 27m, Volume = 87, OpenInterest = 1683, DT = new DateTime(2012,3,20) },
                new OHLCBar {Open = 26.94m, High =27.15m, Low = 26.45m, Close = 26.45m, Volume = 302, OpenInterest = 1814, DT = new DateTime(2012,3,21) },
                new OHLCBar {Open = 26.77m, High =27.1m, Low = 26.75m, Close = 27.1m, Volume = 765, OpenInterest = 2196, DT = new DateTime(2012,3,22) },
                new OHLCBar {Open = 27.18m, High =27.3m, Low = 26.5m, Close = 26.84m, Volume = 499, OpenInterest = 2397, DT = new DateTime(2012,3,23) },
                new OHLCBar {Open = 26.58m, High =26.58m, Low = 25.5m, Close = 25.65m, Volume = 644, OpenInterest = 2782, DT = new DateTime(2012,3,26) },
            };

            _ts = new TimeSeries(data);
        }

        [Test]
        public void CurrentBarProgressedToTheRightPoint()
        {
            var progressed = _ts.AdvanceTo(new DateTime(2012, 3, 12));
            Assert.IsTrue(progressed);
            Assert.AreEqual(2, _ts.CurrentBar);
        }

        [Test]
        public void AdvanceToReturnsFalseWhenNotProgressed()
        {
            _ts.AdvanceTo(new DateTime(2012, 3, 9));
            var progressed = _ts.AdvanceTo(new DateTime(2012, 3, 11));
            Assert.IsFalse(progressed);
        }

        [Test]
        public void AdvanceToReturnsFalseWhenAtEndOfSeries()
        {
            _ts.AdvanceTo(new DateTime(2012, 3, 26));
            Assert.IsFalse(_ts.AdvanceTo(new DateTime(2012, 3, 27)));
        }
    }
}