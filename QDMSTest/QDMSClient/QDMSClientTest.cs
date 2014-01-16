// -----------------------------------------------------------------------
// <copyright file="QDMSClientTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using NUnit.Framework;
using QDMS;

namespace QDMSTest
{
    [TestFixture]
    public class QDMSClientTest
    {
        private QDMSClient.QDMSClient _client;

        [SetUp]
        public void SetUp()
        {
            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5555, 5556, 5557, 5558);
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
        }

        [Test]
        public void RequestHistoricalDataRaisesErrorEventAndReturnsMinusOneWhenDatesAreWrong()
        {
            var req = new HistoricalDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY" },
                StartingDate = new DateTime(2012, 1, 1),
                EndingDate = new DateTime(2011, 1, 1),
                Frequency = BarSize.OneDay
            };

            bool errorTriggered = false;
            _client.Error += (sender, e) => errorTriggered = true;

            Assert.AreEqual(-1, _client.RequestHistoricalData(req));

            Assert.IsTrue(errorTriggered);
        }

        [Test]
        public void RequestHistoricalDataRaisesErrorEventAndReturnsMinusOneWhenNotConnected()
        {
            var req = new HistoricalDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY" },
                StartingDate = new DateTime(2012, 1, 1),
                EndingDate = new DateTime(2013, 1, 1),
                Frequency = BarSize.OneDay
            };

            bool errorTriggered = false;
            _client.Error += (sender, e) => errorTriggered = true;

            Assert.AreEqual(-1, _client.RequestHistoricalData(req));

            Assert.IsTrue(errorTriggered);
        }

        [Test]
        public void RequestHistoricalDataRaisesErrorEventAndReturnsMinusOneWhenInstrumentIsNull()
        {
            var req = new HistoricalDataRequest
            {
                Instrument = null,
                StartingDate = new DateTime(2012, 1, 1),
                EndingDate = new DateTime(2013, 1, 1),
                Frequency = BarSize.OneDay
            };

            bool errorTriggered = false;
            _client.Error += (sender, e) => errorTriggered = true;

            Assert.AreEqual(-1, _client.RequestHistoricalData(req));

            Assert.IsTrue(errorTriggered);
        }

        [Test]
        public void RequestRealTimelDataRaisesErrorEventAndReturnsMinusOneWhenInstrumentIsNull()
        {
            var req = new RealTimeDataRequest
            {
                Instrument = null
            };

            bool errorTriggered = false;
            _client.Error += (sender, e) => errorTriggered = true;

            Assert.AreEqual(-1, _client.RequestRealTimeData(req));

            Assert.IsTrue(errorTriggered);
        }

        [Test]
        public void RequestRealTimelDataRaisesErrorEventAndReturnsMinusOneWhenNotConnected()
        {
            var req = new RealTimeDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY" }
            };

            bool errorTriggered = false;
            _client.Error += (sender, e) => errorTriggered = true;

            Assert.AreEqual(-1, _client.RequestRealTimeData(req));

            Assert.IsTrue(errorTriggered);
        }
    }
}
