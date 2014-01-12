// -----------------------------------------------------------------------
// <copyright file="HistoricalDataBrokerTest.cs" company="">
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
            _dataSourceMock.SetupGet(x => x.Connected).Returns(true);

            _localStorageMock = new Mock<IDataStorage>();

            _broker = new HistoricalDataBroker(_localStorageMock.Object, new List<IHistoricalDataSource> { _dataSourceMock.Object });
        }

        [TearDown]
        public void TearDown()
        {
            _broker.Dispose();
        }

        [Test]
        public void DataAdditionRequestsAreForwardedToLocalStorage()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void HistoricalDataRequestsAreForwardedToTheCorrectDataSource()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void BrokerConnectsToDataSources()
        {
            Assert.IsTrue(false);
        }
        
    }
}
