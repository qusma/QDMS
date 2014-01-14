// -----------------------------------------------------------------------
// <copyright file="RealTimeDataServerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Moq;
using NUnit.Framework;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class RealTimeDataServerTest
    {
        private RealTimeDataServer _rtServer;
        private QDMSClient.QDMSClient _client;
        private Mock<IRealTimeDataBroker> _brokerMock;

        [SetUp]
        public void SetUp()
        {
            _brokerMock = new Mock<IRealTimeDataBroker>();

            //also need the real time server to keep the "heartbeat" going
            _rtServer = new RealTimeDataServer(5555, 5554, _brokerMock.Object);
            _rtServer.StartServer();

            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5554, 5555, 5556, 5557);
            _client.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _rtServer.Dispose();
            _client.Dispose();
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