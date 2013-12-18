// -----------------------------------------------------------------------
// <copyright file="ContinuousFuturesBrokerTest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Moq;
using NUnit.Framework;
using QDMS;
using QDMSServer;
using System;

namespace QDMSTest
{
    [TestFixture]
    public class ContinuousFuturesBrokerTest : IDisposable
    {
        private ContinuousFuturesBroker _broker;
        private Mock<IDataClient> _clientMock;

        public void Dispose()
        {
            if (_broker != null)
            {
                _broker.Dispose();
                _broker = null;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _clientMock = new Mock<IDataClient>();
            _broker = new ContinuousFuturesBroker(_clientMock.Object);
        }

        [Test]
        public void RequestsCorrectContracts()
        {
            var inst = new Instrument();
            var cf = new ContinuousFuture();
            
            inst.ContinuousFuture = cf;

            var underlying = new UnderlyingSymbol();
            var expirationRule = new ExpirationRule();
            underlying.Rule = expirationRule;
            underlying.Symbol = "ES";

            inst.ContinuousFuture.UnderlyingSymbol = underlying;

            var req = new HistoricalDataRequest(
                inst,
                BarSize.OneDay,
                new DateTime(2012, 1, 1),
                new DateTime(2013, 1, 1));

            _broker.RequestHistoricalData(req);

            
        }

        [Test]
        public void CorrectTimeBasedSwitchover()
        {
        }

        [Test]
        public void CorrectVolumeBasedSwitchover()
        {
        }

        [Test]
        public void CorrectOpenInterestBasedSwitchover()
        {

        }

        [Test]
        public void CorrectOpenInterestAndVolumeBasedSwitchover()
        {
        }

        [Test]
        public void CorrectOpenInterestOrVolumeBasedSwitchover()
        {
        }

        [Test]
        public void CorrectContinuousPricesWithRatioAdjustment()
        {
        }

        [Test]
        public void CorrectContinuousPricesWithDifferenceAdjustment()
        {
        }

        [Test]
        public void CorrectContinuousPricesWithNoAdjustment()
        {
        }
    }
}
