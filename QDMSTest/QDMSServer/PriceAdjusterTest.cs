// -----------------------------------------------------------------------
// <copyright file="PriceAdjusterTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using QDMS;
using QDMS.Utils;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class PriceAdjusterTest
    {
        private List<OHLCBar> _data;

        [SetUp]
        public void SetUp()
        {
            _data = new List<OHLCBar>
            {
                new OHLCBar {Open = 100, High = 100, Low = 100, Close = 100},
                new OHLCBar {Open = 100, High = 100, Low = 100, Close = 100},
                new OHLCBar {Open = 100, High = 100, Low = 100, Close = 100},
                new OHLCBar {Open = 100, High = 100, Low = 100, Close = 100}
            };
        }

        [Test]
        public void AdjustsForSplitsCorrectly()
        {
            _data[1].Split = 10;
            PriceAdjuster.AdjustData(ref _data);
            Assert.AreEqual(10, _data[0].AdjClose);
            Assert.AreEqual(100, _data[1].AdjClose);
        }

        [Test]
        public void AdjustsForDividendsCorrectly()
        {
            _data[1].Dividend = 1;
            _data[0].Close = 101;

            PriceAdjuster.AdjustData(ref _data);
            Assert.AreEqual(100, _data[0].AdjClose);
            Assert.AreEqual(100, _data[1].AdjClose);
        }

        [Test]
        public void AdjustsForDividendsAndSplitsSimultaneouslyCorrectly()
        {
            _data[1].Dividend = 1;
            _data[0].Close = 101;
            _data[1].Split = 10;

            PriceAdjuster.AdjustData(ref _data);

            Assert.AreEqual(10, _data[0].AdjClose);
            Assert.AreEqual(100, _data[1].AdjClose);
        }

        [Test]
        public void LeavesDataUntouchedIfNoDividendsOrSplits()
        {
            PriceAdjuster.AdjustData(ref _data);

            Assert.AreEqual(_data[0].AdjClose, _data[0].Close);
            Assert.AreEqual(_data[1].AdjClose, _data[1].Close);
            Assert.AreEqual(_data[2].AdjClose, _data[2].Close);
            Assert.AreEqual(_data[3].AdjClose, _data[3].Close);

            Assert.AreEqual(100, _data[0].Close);
            Assert.AreEqual(100, _data[1].Close);
            Assert.AreEqual(100, _data[2].Close);
            Assert.AreEqual(100, _data[3].Close);
        }

        [Test]
        public void DoesNotCrashOnNullInput()
        {
            _data = null;
            PriceAdjuster.AdjustData(ref _data);
        }
    }
}