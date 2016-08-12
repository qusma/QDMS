// -----------------------------------------------------------------------
// <copyright file="RealTimeDataServerTest.cs" company="">
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
    public class RealTimeDataServerTest
    {
        private const int DefaultDelayInMilliseconds = 100;

        private RealTimeDataServer _rtServer;
        private QDMSClient.QDMSClient _client;
        private Mock<IRealTimeDataBroker> _brokerMock;

        [SetUp]
        public void SetUp()
        {
            _brokerMock = new Mock<IRealTimeDataBroker>();
            // Also need the real time server to keep the "heartbeat" going
            _rtServer = new RealTimeDataServer(5555, 5554, _brokerMock.Object);
            _rtServer.StartServer();

            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5554, 5555, 5556, 5557);
            _client.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _rtServer.Dispose();
        }

        [Test]
        public void ServerCorrectlyForwardsRealTimeDataRequestsToBroker()
        {
            var ds = new Datasource { ID = 1, Name = "TestDS" };
            var inst = new Instrument { ID = 15, Datasource = ds, DatasourceID = 1, Symbol = "SPY", Type = InstrumentType.Stock };
            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds, false);

            _client.RequestRealTimeData(req);

            Thread.Sleep(DefaultDelayInMilliseconds);

            _brokerMock.Verify(
                x => x.RequestRealTimeData(
                    It.Is<RealTimeDataRequest>(
                        y => y.Frequency == BarSize.FiveSeconds &&
                             y.RTHOnly == false &&
                             y.SaveToLocalStorage == false &&
                             y.Instrument.ID == 15 &&
                             y.Instrument.Symbol == "SPY" &&
                             y.Instrument.Datasource.Name == "TestDS")));
        }

        [Test]
        public void ServerCorrectlyForwardsCancellationRequestsToBroker()
        {
            var ds = new Datasource { ID = 1, Name = "TestDS" };
            var inst = new Instrument { ID = 15, Datasource = ds, DatasourceID = 1, Symbol = "SPY", Type = InstrumentType.Stock };

            _client.CancelRealTimeData(inst);

            Thread.Sleep(DefaultDelayInMilliseconds);

            _brokerMock.Verify(x => x.CancelRTDStream(It.Is<int>(y => y == 15)));
        }

        [Test]
        public void ServerReturnsErrorToClientIfNoInstrumentIdIsSet()
        {
            var ds = new Datasource { ID = 1, Name = "TestDS" };
            var inst = new Instrument { ID = null, Datasource = ds, DatasourceID = 1, Symbol = "SPY", Type = InstrumentType.Stock };
            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds, false);

            string error = null;
            int? requestId = null;

            _client.Error += (s, e) =>
            {
                error = e.ErrorMessage;
                requestId = e.RequestID;
            };

            _client.RequestRealTimeData(req);

            Thread.Sleep(DefaultDelayInMilliseconds);

            Assert.IsTrue(!string.IsNullOrEmpty(error));
            Assert.IsTrue(requestId.HasValue);
        }

        [Test]
        public void ServerReturnsErrorToClientIfNoInstrumentDatasourceIsSet()
        {
            var inst = new Instrument { ID = 1, Datasource = null, Symbol = "SPY", Type = InstrumentType.Stock };
            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds, false);

            string error = null;
            int? requestId = null;

            _client.Error += (s, e) =>
            {
                error = e.ErrorMessage;
                requestId = e.RequestID;
            };

            _client.RequestRealTimeData(req);

            Thread.Sleep(DefaultDelayInMilliseconds);

            Assert.IsTrue(!string.IsNullOrEmpty(error));
            Assert.IsTrue(requestId.HasValue);
        }

        [Test]
        public void ServerForwardsErrorToClientIfExceptionIsThrownInRequestRealTimeData()
        {
            var ds = new Datasource { ID = 1, Name = "TestDS" };
            var inst = new Instrument { ID = 1, Datasource = ds, DatasourceID = 1, Symbol = "SPY", Type = InstrumentType.Stock };
            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds, false);

            string error = null;
            int? requestId = null;

            _client.Error += (s, e) =>
            {
                error = e.ErrorMessage;
                requestId = e.RequestID;
            };

            _brokerMock.Setup(x => x.RequestRealTimeData(It.IsAny<RealTimeDataRequest>())).Throws(new Exception("testerror"));

            _client.RequestRealTimeData(req);

            Thread.Sleep(DefaultDelayInMilliseconds);

            Assert.IsTrue(!string.IsNullOrEmpty(error));
            Assert.IsTrue(requestId.HasValue);
        }

        [Test]
        public void ServerCorrectlyForwardsRealTimeData()
        {
            var ds = new Datasource { ID = 1, Name = "TestDS" };
            var inst = new Instrument { ID = 15, Datasource = ds, DatasourceID = 1, Symbol = "SPY", Type = InstrumentType.Stock };
            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds, false);

            _brokerMock.Setup(x => x.RequestRealTimeData(It.IsAny<RealTimeDataRequest>())).Returns(true);

            _client.RequestRealTimeData(req);

            Thread.Sleep(DefaultDelayInMilliseconds);

            RealTimeDataEventArgs receivedData = null;

            _client.RealTimeDataReceived += (s, e) => receivedData = e;

            var dt = DateTime.Now.ToBinary();

            _brokerMock.Raise(x => x.RealTimeDataArrived += null, new RealTimeDataEventArgs(15, dt, 100m, 105m, 95m, 99m, 10000000, 101, 500, 1));

            Thread.Sleep(DefaultDelayInMilliseconds);

            Assert.IsNotNull(receivedData);
            Assert.AreEqual(15, receivedData.InstrumentID);
            Assert.AreEqual(dt, receivedData.Time);
            Assert.AreEqual(100m, receivedData.Open);
            Assert.AreEqual(105m, receivedData.High);
            Assert.AreEqual(95m, receivedData.Low);
            Assert.AreEqual(99m, receivedData.Close);
            Assert.AreEqual(10000000, receivedData.Volume);
            Assert.AreEqual(500, receivedData.Count);
            Assert.AreEqual(101, receivedData.Wap);
        }

        [Test]
        public void StartServerStartsTheServer()
        {
            Assert.IsTrue(_rtServer.ServerRunning);
        }

        [Test]
        public void StopServerStopsTheServer()
        {
            _rtServer.StopServer();
            Assert.IsFalse(_rtServer.ServerRunning);
        }
    }
}