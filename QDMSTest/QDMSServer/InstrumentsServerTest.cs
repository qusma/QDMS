// -----------------------------------------------------------------------
// <copyright file="InstrumentsServerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Moq;
using NUnit.Framework;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class InstrumentsServerTest
    {
        private InstrumentsServer _server;
        private QDMSClient.QDMSClient _client;
        private Mock<IInstrumentSource> _instrumentSourceMock;

        [SetUp]
        public void SetUp()
        {
            _instrumentSourceMock = new Mock<IInstrumentSource>();
            _server = new InstrumentsServer(5555, _instrumentSourceMock.Object);

            _server.StartServer();

            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5553, 5554, 5555, 5556);
        }

        [TearDown]
        public void TearDown()
        {
            _server.StopServer();
            _server.Dispose();

            _client.Dispose();
        }

        [Test]
        public void SearchesForInstrumentsWithTheCorrectParameters()
        {
            Assert.IsTrue(false);
        }
    }
}
