// -----------------------------------------------------------------------
// <copyright file="HistoricalDataServerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using Moq;
using NUnit.Framework;
using QDMS;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class HistoricalDataServerTest
    {
        private HistoricalDataServer _hdServer;
        private RealTimeDataServer _rtServer;
        private QDMSClient.QDMSClient _client;
        private Mock<IHistoricalDataBroker> _brokerMock;

        [SetUp]
        public void SetUp()
        {
            _brokerMock = new Mock<IHistoricalDataBroker>();

            //also need the real time server to keep the "heartbeat" going
            _rtServer = new RealTimeDataServer(5555, 5554);
            _rtServer.StartServer();

            _hdServer = new HistoricalDataServer(5557, _brokerMock.Object);
            _hdServer.StartServer();

            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5554, 5555, 5556, 5557);
            _client.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _hdServer.Dispose();
            _rtServer.Dispose();
            _client.Dispose();
        }

        [Test]
        public void HistoricalDataRequestsAreForwardedToTheBroker()
        {
            var instrument = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource { ID = 1, Name = "MockSource" }
            };

            instrument.Exchange = new Exchange()
            {
                ID = 1,
                Name = "Exchange",
                Timezone = "Eastern Standard Time"
            };

            var request = new HistoricalDataRequest(instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1));

            _client.RequestHistoricalData(request);

            Thread.Sleep(500);

            _brokerMock.Verify(x => x.RequestHistoricalData(
                It.Is<HistoricalDataRequest>(r => 
                    r.Instrument.ID == 1 && 
                    r.Frequency == BarSize.OneDay &&
                    r.StartingDate == new DateTime(2012, 1, 1) &&
                    r.EndingDate == new DateTime(2013, 1, 1))), Times.Once);
        }
    }
}
