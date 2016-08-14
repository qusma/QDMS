// -----------------------------------------------------------------------
// <copyright file="InstrumentsServerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using EntityData;
using MetaLinq;
using Moq;
using NUnit.Framework;
using QDMS;
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
                        y.Type == QDMS.InstrumentType.Stock)));
        }

        [Test]
        public void RequestForAllInstrumentsIsForwardedCorrectly()
        {
            _client.FindInstruments();
            Thread.Sleep(50);

            _instrumentSourceMock.Verify(
                x => x.FindInstruments(
                    It.IsAny<MyDBContext>(), 
                    It.Is<QDMS.Instrument>(y => y == null)));
        }

        [Test]
        public void ExpressionSearchIsTransmittedCorrectly()
        {
            //send the expression, then test it against the one received to make sure they're identical
            Expression<Func<Instrument, bool>> exp = x => x.Symbol == "SPY" && x.Type == InstrumentType.CFD && !x.IsContinuousFuture;

            var ms = new MemoryStream();
            EditableExpression editableExpr = EditableExpression.CreateEditableExpression(exp);
            XmlSerializer xs = new XmlSerializer(editableExpr.GetType());
            xs.Serialize(ms, editableExpr);

            Expression<Func<Instrument, bool>> receivedExpr = null;
            _instrumentSourceMock.Setup(x =>
                x.FindInstruments(It.IsAny<Expression<Func<Instrument, bool>>>(), It.IsAny<MyDBContext>()))
            .Callback<Expression<Func<Instrument, bool>>, MyDBContext>((x, y) => receivedExpr = x);

            _client.FindInstruments(exp);
            Thread.Sleep(100);

            Assert.IsNotNull(receivedExpr);

            var ms2 = new MemoryStream();
            EditableExpression receivedEditableExpr = EditableExpression.CreateEditableExpression(exp);
            xs.Serialize(ms2, receivedEditableExpr);

            Assert.AreEqual(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8.GetString(ms2.ToArray()));
        }
    }
}