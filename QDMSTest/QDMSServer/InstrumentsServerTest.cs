// -----------------------------------------------------------------------
// <copyright file="InstrumentsServerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using EntityData;
using Moq;
using NUnit.Framework;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class InstrumentsServerTest
    {
        private InstrumentsServer _instrumentsServer;
        
        // Needed for the heartbeat the checks if the connection is alive.
        private RealTimeDataServer _rtdServer;
        private QDMSClient.QDMSClient _client;
        private Mock<IInstrumentSource> _instrumentSourceMock;
        private Mock<IRealTimeDataBroker> _rtdBrokerMock;

        [SetUp]
        public void SetUp()
        {
            _instrumentSourceMock = new Mock<IInstrumentSource>();
            _instrumentsServer = new InstrumentsServer(5555, _instrumentSourceMock.Object);

            _rtdBrokerMock = new Mock<IRealTimeDataBroker>();
            _rtdServer = new RealTimeDataServer(5554, 5553, _rtdBrokerMock.Object);

            _instrumentsServer.StartServer();
            _rtdServer.StartServer();

            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5553, 5554, 5555, 5556);
            _client.Connect();
        }

        [TearDown]
        public void TearDown()
        {
            _instrumentsServer.StopServer();
            _instrumentsServer.Dispose();

            _rtdServer.StopServer();
            _rtdServer.Dispose();

            _client.Dispose();
        }

        [Test]
        public void SearchesForInstrumentsWithTheCorrectParameters()
        {
            _client.FindInstruments(new QDMS.Instrument { Symbol = "SPY", Datasource = new QDMS.Datasource { Name = "Interactive Brokers" }, Type = QDMS.InstrumentType.Stock });

            _instrumentSourceMock.Verify(
                x => x.FindInstruments(
                    It.IsAny<MyDBContext>(),
                    It.Is<QDMS.Instrument>(y => 
                        y.Symbol == "SPY" &&
                        y.Datasource.Name == "Interactive Brokers" &&
                        y.Type == QDMS.InstrumentType.Stock),
                    It.Is<Func<QDMS.Instrument, bool>>(y => y == null)));
        }

        [Test]
        public void RequestForAllInstrumentsIsForwardedCorrectly()
        {
            _client.FindInstruments();
            Thread.Sleep(50);

            _instrumentSourceMock.Verify(
                x => x.FindInstruments(
                    It.IsAny<MyDBContext>(), 
                    It.Is<QDMS.Instrument>(y => y == null), 
                    It.Is<Func<QDMS.Instrument, bool>>(y => y == null)));
        }
    }
}