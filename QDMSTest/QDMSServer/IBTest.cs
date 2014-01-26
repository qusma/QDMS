// -----------------------------------------------------------------------
// <copyright file="IBTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using Krs.Ats.IBNet;
using Moq;
using NUnit.Framework;
using QDMS;
using QDMSServer.DataSources;
using BarSize = Krs.Ats.IBNet.BarSize;
using HistoricalDataEventArgs = Krs.Ats.IBNet.HistoricalDataEventArgs;

namespace QDMSTest
{
    [TestFixture]
    public class IBTest
    {
        private Mock<IIBClient> _ibClientMock;
        private IB _ibDatasource;

        [SetUp]
        public void SetUp()
        {
            _ibClientMock = new Mock<IIBClient>();
            _ibClientMock.Setup(x => x.Connected).Returns(true);
            _ibDatasource = new IB(client: _ibClientMock.Object);
            _ibDatasource.Connect();
        }

        [Test]
        public void CallsConnectOnIBClient()
        {
            _ibClientMock.Verify(x => x.Connect(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()));
        }

        [Test]
        public void HistoricalRequestsAreSplitToRespectRequestLimits()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void RealTimeRequestsAreReSentAfterARealTimeDataPacingViolation()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void HistoricalRequestsAreReSentAfterARealTimeDataPacingViolation()
        {
            var exchange = new Exchange { ID = 1, Name = "Ex", Timezone = "Pacific Standard Time" };
            var req = new HistoricalDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY", Exchange = exchange },
                Frequency = QDMS.BarSize.OneDay,
                StartingDate = new DateTime(2014, 1, 14),
                EndingDate = new DateTime(2014, 1, 15),
                RTHOnly = true
            };

            int requestID = 0;

            _ibClientMock
                .Setup(x => x.RequestHistoricalData(
                    It.IsAny<int>(),
                    It.IsAny<Contract>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<Krs.Ats.IBNet.BarSize>(),
                    It.IsAny<HistoricalDataType>(),
                    It.IsAny<int>()))
                .Callback<Int32, Contract, DateTime, String, BarSize, HistoricalDataType, Int32>((y, a, b, c, d, e, f) => requestID = y);


            _ibDatasource.RequestHistoricalData(req);

            _ibClientMock.Raise(x => x.Error += null, new ErrorEventArgs(requestID, (ErrorMessage) 162, ""));

            Thread.Sleep(20000);

            _ibClientMock.Verify(x => x.RequestHistoricalData(
                    It.IsAny<int>(),
                    It.IsAny<Contract>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<Krs.Ats.IBNet.BarSize>(),
                    It.IsAny<HistoricalDataType>(),
                    It.IsAny<int>()), 
                    Times.Exactly(2));
        }

        [Test]
        public void HistoricalRequestsAreCorrectlyForwardedToTheIBClient()
        {
            var exchange = new Exchange { ID = 1, Name = "Ex", Timezone = "Pacific Standard Time" };
            var req = new HistoricalDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY", Exchange = exchange },
                Frequency = QDMS.BarSize.OneDay,
                StartingDate = new DateTime(2014, 1, 14),
                EndingDate = new DateTime(2014, 1, 15),
                RTHOnly = true
            };

            _ibDatasource.RequestHistoricalData(req);

            _ibClientMock.Verify(
                x => x.RequestHistoricalData(
                    It.IsAny<int>(),
                    It.IsAny<Contract>(),
                    It.IsAny<DateTime>(),
                    It.Is<string>(y => y == "1 D"),
                    It.Is<Krs.Ats.IBNet.BarSize>(y => y == Krs.Ats.IBNet.BarSize.OneDay),
                    It.Is<HistoricalDataType>(y => y == HistoricalDataType.Trades),
                    It.Is<int>(y => y == 1))
                , Times.Once);
        }

        [Test]
        public void RealTimeRequestsAreCorrectlyForwardedToTheIBClient()
        {
            var exchange = new Exchange { ID = 1, Name = "Ex", Timezone = "Pacific Standard Time" };
            var req = new RealTimeDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY", UnderlyingSymbol = "SPY", Exchange = exchange, Currency = "USD", Type = InstrumentType.Stock },
                Frequency = QDMS.BarSize.FiveSeconds,
                RTHOnly = true
            };

            _ibDatasource.RequestRealTimeData(req);

            _ibClientMock.Verify(x => x.RequestRealTimeBars(
                It.IsAny<int>(),
                It.Is<Contract>(y => y.Symbol == "SPY" && y.Exchange == "Ex" && y.SecurityType == SecurityType.Stock),
                It.Is<int>(y => y == (int)Krs.Ats.IBNet.BarSize.FiveSeconds),
                It.Is<RealTimeBarType>(y => y == RealTimeBarType.Trades),
                It.Is<bool>(y => y == true)));
        }

        [Test]
        public void WhenDataSourceSymbolIsSetThatIsTheValueSentInTheHistoricalRequest()
        {
            var exchange = new Exchange { ID = 1, Name = "Ex", Timezone = "Pacific Standard Time" };
            var req = new HistoricalDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY", UnderlyingSymbol = "SPY", DatasourceSymbol = "TestMe!", Exchange = exchange, Currency = "USD", Type = InstrumentType.Stock },
                Frequency = QDMS.BarSize.OneDay,
                StartingDate = new DateTime(2014, 1, 14),
                EndingDate = new DateTime(2014, 1, 15),
                RTHOnly = true
            };

            _ibDatasource.RequestHistoricalData(req);

            _ibClientMock.Verify(x => x.RequestHistoricalData(
                It.IsAny<int>(), 
                It.Is<Contract>(y => y.Symbol == "TestMe!"),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<BarSize>(),
                It.IsAny<HistoricalDataType>(),
                It.IsAny<int>()));
        }

        [Test]
        public void WhenDataSourceSymbolIsSetThatIsTheValueSentInTheRealTimeRequest()
        {
            var exchange = new Exchange { ID = 1, Name = "Ex", Timezone = "Pacific Standard Time" };
            var req = new RealTimeDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY", DatasourceSymbol = "TestMe!", UnderlyingSymbol = "SPY", Exchange = exchange, Currency = "USD", Type = InstrumentType.Stock },
                Frequency = QDMS.BarSize.FiveSeconds,
                RTHOnly = true
            };

            _ibDatasource.RequestRealTimeData(req);

            _ibClientMock.Verify(x => x.RequestRealTimeBars(
                It.IsAny<int>(),
                It.Is<Contract>(y => y.Symbol == "TestMe!"),
                It.IsAny<int>(),
                It.IsAny<RealTimeBarType>(),
                It.IsAny<bool>()));
        }

        [Test]
        public void ArrivedRealTimeDataCorrentlyRaisesEvent()
        {
            var exchange = new Exchange { ID = 1, Name = "Ex", Timezone = "Pacific Standard Time" };
            var req = new RealTimeDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY", UnderlyingSymbol = "SPY", Exchange = exchange, Currency = "USD", Type = InstrumentType.Stock },
                Frequency = QDMS.BarSize.FiveSeconds,
                RTHOnly = true
            };

            int requestID = -1;
            _ibClientMock
                .Setup(x => x.RequestRealTimeBars(It.IsAny<int>(), It.IsAny<Contract>(), It.IsAny<int>(), It.IsAny<RealTimeBarType>(), It.IsAny<bool>()))
                .Callback<int, Contract, Int32, RealTimeBarType, Boolean>((y, a, b, c, d) => requestID = y);

            _ibDatasource.RequestRealTimeData(req);

            bool received = false;
            _ibDatasource.DataReceived += (sender, e) => received = true;

            _ibClientMock.Raise(x => x.RealTimeBar += null, new RealTimeBarEventArgs(requestID, 10000000, 1, 2, 3, 4, 5, 3, 5));

            Assert.IsTrue(received);
        }

        [Test]
        public void ArrivedHistoricalDataCorrectlyRaisesEvent()
        {
            var exchange = new Exchange { ID = 1, Name = "Ex", Timezone = "Pacific Standard Time" };
            var req = new HistoricalDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY", UnderlyingSymbol = "SPY", Exchange = exchange, Currency = "USD", Type = InstrumentType.Stock },
                Frequency = QDMS.BarSize.OneDay,
                StartingDate = new DateTime(2014, 1, 14),
                EndingDate = new DateTime(2014, 1, 15),
                RTHOnly = true
            };

            int requestID = -1;
            _ibClientMock
                .Setup(x => x.RequestHistoricalData(It.IsAny<int>(), It.IsAny<Contract>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<BarSize>(), It.IsAny<HistoricalDataType>(), It.IsAny<int>()))
                .Callback<Int32, Contract, DateTime, String, BarSize, HistoricalDataType, Int32>((y, a, b, c, d, e, f) => requestID = y);

            _ibDatasource.RequestHistoricalData(req);

            bool received = false;
            _ibDatasource.HistoricalDataArrived += (sender, e) => received = true;

            _ibClientMock.Raise(x => x.HistoricalData += null, 
                new HistoricalDataEventArgs(
                    requestID, 
                    new DateTime(2014, 1, 15),
                    1,
                    2,
                    3,
                    4,
                    5,
                    5,
                    3,
                    false,
                    1,
                    1));

            Assert.IsTrue(received);
        }
    }
}