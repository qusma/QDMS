// -----------------------------------------------------------------------
// <copyright file="HistoricalDataServerTest.cs" company="">
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
    public class HistoricalDataServerTest
    {
        private HistoricalDataServer _hdServer;
        private QDMSClient.QDMSClient _client;
        private Mock<IHistoricalDataBroker> _brokerMock;

        [SetUp]
        public void SetUp()
        {
            _brokerMock = new Mock<IHistoricalDataBroker>();
            
            _hdServer = new HistoricalDataServer(5557, _brokerMock.Object);
            _hdServer.StartServer();

            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5554, 5555, 5556, 5557);
            _client.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _hdServer.Dispose();
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

        [Test]
        public void StartServerStartsTheServer()
        {
            Assert.IsTrue(_hdServer.ServerRunning);
        }

        [Test]
        public void StopServerStopsTheServer()
        {
            _hdServer.StopServer();
            Assert.IsFalse(_hdServer.ServerRunning);
        }

        [Test]
        public void DataPushRequestIsForwardedToLocalStorage()
        {
            var instrument = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource { ID = 1, Name = "MockSource" }
            };

            var data = new List<OHLCBar>
            {
                new OHLCBar {Open = 1, High = 2, Low = 3, Close = 4, DT = new DateTime(2013, 1, 1) }
            };

            var req = new DataAdditionRequest(BarSize.OneDay, instrument, data, true);

            _client.PushData(req);

            Thread.Sleep(50);

            _brokerMock.Verify(x => x.AddData(
                It.Is<DataAdditionRequest>(y =>
                    y.Frequency == BarSize.OneDay &&
                    y.Instrument.ID == 1 &&
                    y.Data.Count == 1)
                ), Times.Once);
        }

        [Test]
        public void AcceptAvailableDataRequestsAreForwardedToTheHistoricalDataBroker()
        {
            var instrument = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource { ID = 1, Name = "MockSource" }
            };

            _client.GetLocallyAvailableDataInfo(instrument);

            Thread.Sleep(50);

            _brokerMock.Verify(x => x.GetAvailableDataInfo(
                It.Is<Instrument>(y =>
                    y.ID == 1 &&
                    y.Symbol == "SPY")
                    ), Times.Once);
        }

        [Test]
        public void SendsErrorMessageWhenExceptionIsRaisedByBrokerOnHistoricalDataRequest()
        {
            bool errorRaised = false;
            _client.Error += (sender, e) => errorRaised = true;

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

            _brokerMock.Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>())).Throws(new Exception("error message"));   

            _client.RequestHistoricalData(request);

            Thread.Sleep(500);

            Assert.IsTrue(errorRaised);
        }
    }
}