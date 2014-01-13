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
        private RealTimeDataBroker _rtBrokerMock;
        private Mock<IRealTimeDataSource> _ds;
        private Mock<IContinuousFuturesBroker> _cfBrokerMock;
        private QDMSClient.QDMSClient _client;

        [SetUp]
        public void SetUp()
        {
            _ds = new Mock<IRealTimeDataSource>();
            _ds.SetupGet(x => x.Name).Returns("MockSource");
            _ds.SetupGet(x => x.Connected).Returns(false);

            _cfBrokerMock = new Mock<IContinuousFuturesBroker>();

            _rtBrokerMock = new RealTimeDataBroker(5555, 5556, new List<IRealTimeDataSource> { _ds.Object }, _cfBrokerMock.Object);
            _rtBrokerMock.StartServer();

            _ds.SetupGet(x => x.Connected).Returns(true);

            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5556, 5555, 5554, 5553);
            _client.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Disconnect();
            _client.Dispose();

            _rtBrokerMock.StopServer();
            _rtBrokerMock.Dispose();
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
        public void SendsFrontContractRequestToCFBrokerWhenRealTimeContinuousFuturesDataIsRequested()
        {
            var inst = new Instrument
            {
                ID = 1,
                Symbol = "VIXCONT",
                UnderlyingSymbol = "VIX",
                IsContinuousFuture = true,
                ContinuousFuture = new ContinuousFuture()
                {
                    UnderlyingSymbol = new UnderlyingSymbol()
                    {
                        ID = 1,
                        Symbol = "VIX",
                        Rule = new ExpirationRule()
                    },
                    ID = 1,
                    Month = 1,
                    UnderlyingSymbolID = 1
                },
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            _cfBrokerMock
                .Setup(x => x.RequestFrontContract(It.IsAny<Instrument>(), It.IsAny<DateTime>()))
                .Returns(0);

            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds);

            _client.RequestRealTimeData(req);

            Thread.Sleep(500);

            _cfBrokerMock.Verify(x => x.RequestFrontContract(
                It.Is<Instrument>(i =>
                    i.IsContinuousFuture == true &&
                    i.ID.HasValue && 
                    i.ID == 1 &&
                    i.Symbol == "VIXCONT"
                    ),
                It.IsAny<DateTime>()
                ), Times.Once);
        }

        [Test]
        public void RequestsCorrectFuturesContractForContinuousFutures()
        {
            Assert.IsTrue(false);

        }
    }
}
