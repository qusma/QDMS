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
        private Mock<IContinuousFuturesBroker> _cfBrokerMock;
        private Mock<IRealTimeDataSource> _dataSourceMock;
        private Mock<IDataStorage> _localStorageMock;

        [SetUp]
        public void SetUp()
        {
            _dataSourceMock = new Mock<IRealTimeDataSource>();
            _dataSourceMock.SetupGet(x => x.Name).Returns("MockSource");
            _dataSourceMock.SetupGet(x => x.Connected).Returns(false);

            _cfBrokerMock = new Mock<IContinuousFuturesBroker>();
            _cfBrokerMock.SetupGet(x => x.Connected).Returns(true);

            _localStorageMock = new Mock<IDataStorage>();
            _localStorageMock.Setup(x => x.Connected).Returns(true);

            _broker = new RealTimeDataBroker(_cfBrokerMock.Object, _localStorageMock.Object, new List <IRealTimeDataSource> { _dataSourceMock.Object });

            _dataSourceMock.SetupGet(x => x.Connected).Returns(true);
        }

        [TearDown]
        public void TearDown()
        {
            _broker.Dispose();
        }

        [Test]
        public void BrokerConnectsToDataSources()
        {
            _dataSourceMock.Verify(x => x.Connect(), Times.Once);
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

            _dataSourceMock.Verify(x => x.RequestRealTimeData(
                It.Is<RealTimeDataRequest>(
                    r => r.Instrument.ID == 1 &&
                    r.Instrument.Symbol == "SPY" &&
                    r.Frequency == BarSize.FiveSeconds)),
                Times.Once);
        }

        [Test]
        public void DoesNotRepeatRequestToSourceWhenARealTimeStreamAlreadyExists()
        {
            var inst = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds);

            _broker.RequestRealTimeData(req);
            Thread.Sleep(100);
            _broker.RequestRealTimeData(req);

            _dataSourceMock.Verify(x => x.RequestRealTimeData(
                It.IsAny<RealTimeDataRequest>()),
                Times.Once);
        }

        [Test]
        public void DoesNotRepeatRequestToSourceWhenARealTimeContinuousFuturesStreamAlreadyExists()
        {
            var inst = new Instrument
            {
                ID = 1,
                Symbol = "ContinuousVIXFutures",
                IsContinuousFuture = true,
                Type = InstrumentType.Future,
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            var frontContract = new Instrument
            {
                ID = 2,
                Symbol = "VXF3",
                Type = InstrumentType.Future,
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds);

            _cfBrokerMock.Setup(x => x.RequestFrontContract(It.IsAny<Instrument>(), It.IsAny<DateTime?>())).Returns(1);
            _broker.RequestRealTimeData(req);

            _cfBrokerMock.Raise(x => x.FoundFrontContract += null, new FoundFrontContractEventArgs(1, frontContract, DateTime.Now));

            _broker.RequestRealTimeData(req);

            _dataSourceMock.Verify(x => x.RequestRealTimeData(
                It.IsAny<RealTimeDataRequest>()),
                Times.Once);
            _cfBrokerMock.Verify(x => x.RequestFrontContract(
                It.IsAny<Instrument>(),
                It.IsAny<DateTime?>()),
                Times.Once);
        }

        [Test]
        public void RequestsFrontContractFromCFBrokerForContinuousFuturesRequests()
        {
            var cf = new ContinuousFuture()
            {
                ID = 1,
                InstrumentID = 1,
                Month = 1,
                UnderlyingSymbol = new UnderlyingSymbol
                {
                    ID = 1,
                   Symbol = "VIX",
                   Rule = new ExpirationRule()
                }
            };

            var inst = new Instrument
            {
                ID = 1,
                Symbol = "VIXCONTFUT",
                IsContinuousFuture = true,
                ContinuousFuture = cf,
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds);

            _cfBrokerMock.Setup(x => x.RequestFrontContract(It.IsAny<Instrument>(), It.IsAny<DateTime?>())).Returns(0);

            _broker.RequestRealTimeData(req);

            _cfBrokerMock.Verify(x => x.RequestFrontContract(It.IsAny<Instrument>(), It.IsAny<DateTime?>()));
        }

        [Test]
        public void RequestsCorrectFuturesContractForContinuousFutures()
        {
            var cf = new ContinuousFuture()
            {
                ID = 1,
                InstrumentID = 1,
                Month = 1,
                UnderlyingSymbol = new UnderlyingSymbol
                {
                    ID = 1,
                    Symbol = "VIX",
                    Rule = new ExpirationRule()
                }
            };

            var inst = new Instrument
            {
                ID = 1,
                Symbol = "VIXCONTFUT",
                IsContinuousFuture = true,
                ContinuousFuture = cf,
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds);

            _cfBrokerMock.Setup(x => x.RequestFrontContract(It.IsAny<Instrument>(), It.IsAny<DateTime?>())).Returns(0);

            _broker.RequestRealTimeData(req);

            var frontFutureInstrument = new Instrument
            {
                Symbol = "VXF4",
                ID = 2,
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            _cfBrokerMock.Raise(x => x.FoundFrontContract += null, new FoundFrontContractEventArgs(0, frontFutureInstrument, DateTime.Now));

            _dataSourceMock.Verify(x => x.RequestRealTimeData(
                It.Is<RealTimeDataRequest>(y =>
                    y.Instrument.ID == 2)), Times.Once);
        }

        [Test]
        public void RaisesDataEventWithTheContinuousFuturesAlias()
        {
            var cf = new ContinuousFuture()
            {
                ID = 1,
                InstrumentID = 1,
                Month = 1,
                UnderlyingSymbol = new UnderlyingSymbol
                {
                    ID = 1,
                    Symbol = "VIX",
                    Rule = new ExpirationRule()
                }
            };

            var inst = new Instrument
            {
                ID = 1,
                Symbol = "VIXCONTFUT",
                IsContinuousFuture = true,
                ContinuousFuture = cf,
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds);

            _cfBrokerMock.Setup(x => x.RequestFrontContract(It.IsAny<Instrument>(), It.IsAny<DateTime?>())).Returns(0);
            int assignedID = 0;
            _dataSourceMock.Setup(x => x.RequestRealTimeData(It.IsAny<RealTimeDataRequest>())).Callback<RealTimeDataRequest>(r => assignedID = r.AssignedID);

            bool raisedCorrectSymbol = false;
            _broker.RealTimeDataArrived += (sender, e) =>
                raisedCorrectSymbol = raisedCorrectSymbol ? raisedCorrectSymbol : e.InstrumentID == 1;
            _broker.RequestRealTimeData(req);

            var frontFutureInstrument = new Instrument
            {
                Symbol = "VXF4",
                ID = 2,
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            _cfBrokerMock.Raise(x => x.FoundFrontContract += null, new FoundFrontContractEventArgs(0, frontFutureInstrument, DateTime.Now));

            _dataSourceMock.Raise(x => x.DataReceived += null,
                new RealTimeDataEventArgs(2, MyUtils.ConvertToTimestamp(DateTime.Now), 100, 100, 100, 100, 50, 100, 2, assignedID));

            Thread.Sleep(50);

            Assert.IsTrue(raisedCorrectSymbol);
        }

        [Test]
        public void RealTimeDataIsSavedToLocalStorageIfFlagIsSet()
        {
            var inst = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource { ID = 999, Name = "MockSource" }
            };

            var req = new RealTimeDataRequest(inst, BarSize.FiveSeconds, savetoLocalStorage: true);

            int assignedID = 0;
            _dataSourceMock.Setup(x => x.RequestRealTimeData(It.IsAny<RealTimeDataRequest>())).Callback<RealTimeDataRequest>(r => assignedID = r.AssignedID);

            _broker.RequestRealTimeData(req);
            Thread.Sleep(100);


            _dataSourceMock.Raise(x => x.DataReceived += null, new RealTimeDataEventArgs(1, 1389906576, 100, 100, 100, 100, 1000, 100, 5, assignedID));

            _localStorageMock.Verify(x => x.AddDataAsync(
                It.Is<OHLCBar>(y => y.Open == 100 && y.Volume == 1000),
                It.Is<Instrument>(y => y.ID == 1 && y.Symbol == "SPY"),
                It.Is<BarSize>(y => y == BarSize.FiveSeconds),
                It.Is<bool>(y => y == false)));
        }
    }
}