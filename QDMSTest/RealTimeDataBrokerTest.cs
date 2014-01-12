// -----------------------------------------------------------------------
// <copyright file="RealTimeDataBrokerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
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
        private QDMSClient.QDMSClient _client;

        [SetUp]
        public void SetUp()
        {
            _ds = new Mock<IRealTimeDataSource>();
            _ds.SetupGet(x => x.Name).Returns("MockSource");
            _ds.SetupGet(x => x.Connected).Returns(true);

            _broker = new RealTimeDataBroker(5555, 5556, new List<IRealTimeDataSource> { _ds.Object });
            _broker.StartServer();

            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5556, 5555, 5554, 5554);
            _client.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Disconnect();
            _client.Dispose();

            _broker.StopServer();
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

            _client.RequestRealTimeData(req);

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
