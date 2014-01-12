// -----------------------------------------------------------------------
// <copyright file="HistoricalDataServerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Moq;
using NUnit.Framework;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class HistoricalDataServerTest
    {
        private HistoricalDataServer _server;
        private QDMSClient.QDMSClient _client;
        private Mock<IHistoricalDataBroker> _brokerMock;

        [SetUp]
        public void SetUp()
        {
            _brokerMock = new Mock<IHistoricalDataBroker>();
            _server = new HistoricalDataServer(5557, _brokerMock.Object);
            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5554, 5555, 5556, 5557);
        }

        [TearDown]
        public void TearDown()
        {
            _server.Dispose();
            _client.Dispose();
        }

        [Test]
        public void HistoricalDataRequestsAreForwardedToTheBroker()
        {
            Assert.IsTrue(false);
        }
    }
}
