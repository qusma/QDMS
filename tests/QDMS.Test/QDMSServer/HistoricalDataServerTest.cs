﻿// -----------------------------------------------------------------------
// <copyright file="HistoricalDataServerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using NetMQ;
using NUnit.Framework;
using QDMS;
using QDMSApp;

namespace QDMSTest
{
    [TestFixture]
    public class HistoricalDataServerTest
    {
        private RealTimeDataServer _rtServer;
        private HistoricalDataServer _hdServer;
        private QDMSClient.QDMSClient _client;
        private Mock<IRealTimeDataBroker> _realTimeDataBrokerMock;
        private Mock<IHistoricalDataBroker> _historicalDataBrokerMock;

        [SetUp]
        public void SetUp()
        {
            _realTimeDataBrokerMock = new Mock<IRealTimeDataBroker>();
            _historicalDataBrokerMock = new Mock<IHistoricalDataBroker>();

            var settings = new Mock<ISettings>();
            settings.SetupGet(x => x.rtDBPubPort).Returns(5555);
            settings.SetupGet(x => x.rtDBReqPort).Returns(5554);
            settings.SetupGet(x => x.hDBPort).Returns(5557);

            // Also need the real time server to keep the "heartbeat" going
            _rtServer = new RealTimeDataServer(settings.Object, _realTimeDataBrokerMock.Object);
            _rtServer.StartServer();

            _hdServer = new HistoricalDataServer(settings.Object, _historicalDataBrokerMock.Object);
            _hdServer.StartServer();

            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5554, 5555, 5557, 5559, "");
            _client.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
            _hdServer?.Dispose();
            _rtServer?.Dispose();
            NetMQConfig.Cleanup();
        }

        [Test]
        public void HistoricalDataRequestsAreForwardedToTheBroker()
        {
            var instrument = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource { ID = 1, Name = "MockSource" },
                Exchange = new Exchange
                {
                    ID = 1,
                    Name = "Exchange",
                    Timezone = "Eastern Standard Time"
                }
            };
            var request = new HistoricalDataRequest(instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1));

            _client.RequestHistoricalData(request);
            // TODO: Think about delay amount
            Thread.Sleep(1500);

            _historicalDataBrokerMock.Verify(
                x => x.RequestHistoricalData(
                    It.Is<HistoricalDataRequest>(
                        r =>
                            r.Instrument.ID == 1 &&
                            r.Frequency == BarSize.OneDay &&
                            r.StartingDate == new DateTime(2012, 1, 1) &&
                            r.EndingDate == new DateTime(2013, 1, 1))),
                Times.Once);
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
                new OHLCBar {Open = 1, High = 2, Low = 3, Close = 4, DT = new DateTime(2013, 1, 1)}
            };
            var req = new DataAdditionRequest(BarSize.OneDay, instrument, data);

            _client.PushData(req);
            // TODO: Think about delay amount
            Thread.Sleep(50);

            _historicalDataBrokerMock.Verify(
                x => x.AddData(
                    It.Is<DataAdditionRequest>(
                        y =>
                            y.Frequency == BarSize.OneDay &&
                            y.Instrument.ID == 1 &&
                            y.Data.Count == 1)
                    ),
                Times.Once);
        }

        [Test]
        public void SendsErrorMessageWhenExceptionIsRaisedByBrokerOnHistoricalDataRequest()
        {
            var errorRaised = false;

            _client.Error += (sender, e) => errorRaised = true;

            var instrument = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource { ID = 1, Name = "MockSource" },
                Exchange = new Exchange
                {
                    ID = 1,
                    Name = "Exchange",
                    Timezone = "Eastern Standard Time"
                }
            };
            var request = new HistoricalDataRequest(instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1));

            _historicalDataBrokerMock.Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>())).Throws(new Exception("error message"));

            _client.RequestHistoricalData(request);
            // TODO: Think about delay amount
            Thread.Sleep(1500);

            Assert.IsTrue(errorRaised);
        }
    }
}