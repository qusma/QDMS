// -----------------------------------------------------------------------
// <copyright file="RealTimeDataBrokerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Moq;
using NUnit.Framework;
using QDMS;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class RealTimeDataBrokerTest
    {
        private RealTimeDataBroker _broker;
        private Mock<IRealTimeDataSource> _ds;

        [SetUp]
        public void SetUp()
        {
            _ds = new Mock<IRealTimeDataSource>();
            _ds.SetupGet(x => x.Name).Returns("MockSource");
            _ds.SetupGet(x => x.Connected).Returns(false);

            _broker = new RealTimeDataBroker(new List<IRealTimeDataSource> { _ds.Object });

            _ds.SetupGet(x => x.Connected).Returns(true);
        }

        [TearDown]
        public void TearDown()
        {
            _broker.Dispose();
        }

        [Test]
        public void BrokerConnectsToDataSources()
        {
            _ds.Verify(x => x.Connect(), Times.Once);
        }

        [Test]
        public void RealTimeDataRequestsForwardedToDataSource()
        {
            var inst = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds);

            _broker.RequestRealTimeData(req);

            _ds.Verify(x => x.RequestRealTimeData(
                It.Is<RealTimeDataRequest>(
                    r => r.Instrument.ID == 1 && 
                    r.Instrument.Symbol == "SPY" &&
                    r.Frequency == BarSize.FiveSeconds)), 
                Times.Once);
        }

        [Test]
        public void RequestsCorrectFuturesContractForContinuousFutures()
        {
            


            Assert.IsTrue(false);

        }
    }
}
