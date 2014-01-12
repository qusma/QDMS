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
        private Mock<IHistoricalDataSource> _ds;
        private QDMSClient.QDMSClient _client;

        [SetUp]
        public void SetUp()
        {
            _ds = new Mock<IHistoricalDataSource>();
            _ds.SetupGet(x => x.Name).Returns("MockSource");
            _ds.SetupGet(x => x.Connected).Returns(true);

            _broker = new HistoricalDataBroker(5553, additionalSources: new List<IHistoricalDataSource> { _ds.Object });
            _broker.StartServer();

            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5556, 5555, 5554, 5553);
            _client.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Disconnect();
            _client.Dispose();

            _broker.StopServer();
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
