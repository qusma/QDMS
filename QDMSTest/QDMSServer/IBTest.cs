// -----------------------------------------------------------------------
// <copyright file="IBTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using Krs.Ats.IBNet;
using Moq;
using NUnit.Framework;
using QDMS;
using QDMSServer.DataSources;

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
            _ibDatasource = new IB(client: _ibClientMock.Object);
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
            Assert.IsTrue(false);
        }

        [Test]
        public void WhenDataSourceSymbolIsSetThatIsTheValueSentInTheHistoricalRequest()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void WhenDataSourceSymbolIsSetThatIsTheValueSentInTheRealTimeRequest()
        {
            Assert.IsTrue(false);
        }
    }
}