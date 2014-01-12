// -----------------------------------------------------------------------
// <copyright file="HistoricalDataBrokerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using QDMS;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class HistoricalDataBrokerTest
    {
        private HistoricalDataBroker _broker;
        private Mock<IHistoricalDataSource> _dataSourceMock;
        private Mock<IDataStorage> _localStorageMock;

        [SetUp]
        public void SetUp()
        {
            _dataSourceMock = new Mock<IHistoricalDataSource>();
            _dataSourceMock.SetupGet(x => x.Name).Returns("MockSource");
            _dataSourceMock.SetupGet(x => x.Connected).Returns(false);

            _localStorageMock = new Mock<IDataStorage>();

            _broker = new HistoricalDataBroker(_localStorageMock.Object, new List<IHistoricalDataSource> { _dataSourceMock.Object });

            _dataSourceMock.SetupGet(x => x.Connected).Returns(true);
        }

        [TearDown]
        public void TearDown()
        {
            _broker.Dispose();
        }

        [Test]
        public void DataAdditionRequestsAreForwardedToLocalStorage()
        {
            var data = new List<OHLCBar>
            {
                new OHLCBar {Open = 1, High = 2, Low = 3, Close = 4, DT = new DateTime(2000, 1, 1) }
            };
            
            var instrument = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource {ID = 1, Name = "TESTDS"}
            };

            var request = new DataAdditionRequest(BarSize.OneDay, instrument, data, true);


            _broker.AddData(request);
            _localStorageMock.Verify(x => x.AddData(
                It.Is<List<OHLCBar>>(b => b.Count == 1 && b[0].Close == 4),
                It.Is<Instrument>(i => i.ID == 1 && i.Symbol == "SPY" && i.Datasource.Name == "TESTDS"),
                It.Is<BarSize>(z => z == BarSize.OneDay),
                It.Is<bool>(k => k == true),
                It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public void HistoricalDataRequestsAreForwardedToTheCorrectDataSource()
        {
            var instrument = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource { ID = 1, Name = "TESTDS" }
            };

            var request = new HistoricalDataRequest(instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                forceFreshData: true,
                localStorageOnly: false,
                saveToLocalStorage: false,
                rthOnly: true);
        }

        [Test]
        public void RequestsAreCorrectlySplitIntoSubrequestsWhenOnlyPartOfTheDataIsAvailable()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void ForceFreshDataFlagIsObeyed()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void LocalStorageOnlyFlagIsObeyed()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void SavesToLocalStorageWhenSaveToLocalStorageFlagIsSet()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void DoesNotSaveToLocalStorageWhenSaveToLocalStorageFlagIsNotSet()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void RegularTradingHoursAreFilteredWhenRTHFlagIsSet()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void DataArrivedEventIsRaisedWhenDataSourceReturnsData()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void RequesterIdentityIsPreservedWhenDataIsReturned()
        {
            
        }

        [Test]
        public void BrokerConnectsToDataSources()
        {
            _dataSourceMock.Verify(x => x.Connect(), Times.Once);
        }
        
    }
}
