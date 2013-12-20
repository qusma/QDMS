// -----------------------------------------------------------------------
// <copyright file="ContinuousFuturesBrokerTest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

//TODO upload excel file used to generate target prices somewhere and add a link to it here.

using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using QDMS;
using QDMSServer;
using System;
using EntityData;

namespace QDMSTest
{
    [TestFixture]
    public class ContinuousFuturesBrokerTest : IDisposable
    {
        private ContinuousFuturesBroker _broker;
        private Mock<IDataClient> _clientMock;
        private Mock<IInstrumentSource> _instrumentMgrMock;
        private Instrument _cfInst;
        private HistoricalDataRequest _req;

        [SetUp]
        public void SetUp()
        {
            _clientMock = new Mock<IDataClient>();
            _instrumentMgrMock = new Mock<IInstrumentSource>();
            _broker = new ContinuousFuturesBroker(_clientMock.Object, _instrumentMgrMock.Object);

            _cfInst = new Instrument();
            _cfInst.Type = InstrumentType.Future;
            _cfInst.DatasourceID = 1;
            _cfInst.Datasource = new Datasource { ID = 1, Name = "Interactive Brokers" };

            var cf = new ContinuousFuture();
            cf.Month = 1;
            _cfInst.ContinuousFuture = cf;
            _cfInst.ContinuousFuture.AdjustmentMode = ContinuousFuturesAdjustmentMode.NoAdjustment;

            var underlying = new UnderlyingSymbol();
            underlying.Symbol = "VIX";

            _cfInst.ContinuousFuture.UnderlyingSymbol = underlying;

            _req = new HistoricalDataRequest(
                _cfInst,
                BarSize.OneDay,
                new DateTime(2013, 1, 1),
                new DateTime(2013, 2, 1));
        }

        //This tests the call to the instrument manager, making sure we're looking for the right contracts
        [Test]
        public void SearchesForCorrectContracts()
        {
            _instrumentMgrMock.Setup(x => x.FindInstruments(null, It.IsAny<Instrument>())).Returns(new List<Instrument>());

            _broker.RequestHistoricalData(_req);

            _instrumentMgrMock.Verify(x => x.FindInstruments(null, It.Is<Instrument>(
                i => 
                    i.UnderlyingSymbol == _cfInst.ContinuousFuture.UnderlyingSymbol.Symbol &&
                    i.Type == InstrumentType.Future &&
                    i.DatasourceID == _cfInst.DatasourceID &&
                    i.Datasource == _cfInst.Datasource
                )));
        }

        //This tests the request to the client for historical data, ensuring that the right contracts are requested
        [Test]
        public void RequestsDataOnCorrectContracts()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void CorrectTimeBasedSwitchover()
        {
            var expectedPrices = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2012, 11,20), 16.38m },
                { new DateTime(2012, 11,21), 16.44m },
                { new DateTime(2012, 11,23), 16.1m },
                { new DateTime(2012, 11,26), 15.6m },
                { new DateTime(2012, 11,27), 16.15m },
                { new DateTime(2012, 11,28), 15.48m },
                { new DateTime(2012, 11,29), 15.24m },
                { new DateTime(2012, 11,30), 15.54m },
                { new DateTime(2012, 12,3), 16.4m },
                { new DateTime(2012, 12,4), 16.49m },
                { new DateTime(2012, 12,5), 16.1m },
                { new DateTime(2012, 12,6), 16.39m },
                { new DateTime(2012, 12,7), 16.01m },
                { new DateTime(2012, 12,10), 16m },
                { new DateTime(2012, 12,11), 15.56m },
                { new DateTime(2012, 12,12), 16.11m },
                { new DateTime(2012, 12,13), 16.61m },
                { new DateTime(2012, 12,14), 16.86m },
                { new DateTime(2012, 12,17), 16.19m },
                { new DateTime(2012, 12,18), 16.13m },
                { new DateTime(2012, 12,19), 17.1m },
                { new DateTime(2012, 12,20), 17.51m },
                { new DateTime(2012, 12,21), 18.24m },
                { new DateTime(2012, 12,24), 18.64m },
                { new DateTime(2012, 12,26), 19.49m },
                { new DateTime(2012, 12,27), 19.09m },
                { new DateTime(2012, 12,28), 22.35m },
                { new DateTime(2012, 12,31), 17.68m },
                { new DateTime(2013, 1,2), 15.6m },
                { new DateTime(2013, 1,3), 15.9m },
                { new DateTime(2013, 1,4), 15.3m },
                { new DateTime(2013, 1,7), 14.75m },
                { new DateTime(2013, 1,8), 14.65m },
                { new DateTime(2013, 1,9), 14.7m },
                { new DateTime(2013, 1,10), 14.2m },
                { new DateTime(2013, 1,11), 14.14m },
                { new DateTime(2013, 1,14), 14.09m },
                { new DateTime(2013, 1,15), 15.76m },
                { new DateTime(2013, 1,16), 15.5m },
                { new DateTime(2013, 1,17), 15.69m },
                { new DateTime(2013, 1,18), 14.65m },
                { new DateTime(2013, 1,22), 14.05m },
                { new DateTime(2013, 1,23), 13.69m },
                { new DateTime(2013, 1,24), 13.94m },
                { new DateTime(2013, 1,25), 14.09m },
                { new DateTime(2013, 1,28), 14.59m },
                { new DateTime(2013, 1,29), 14.04m },
                { new DateTime(2013, 1,30), 15.15m },
                { new DateTime(2013, 1,31), 14.9m },
                { new DateTime(2013, 2,1), 14.29m },
                { new DateTime(2013, 2,4), 15.29m },
                { new DateTime(2013, 2,5), 14.39m },
                { new DateTime(2013, 2,6), 14.15m },
                { new DateTime(2013, 2,7), 14.09m },
                { new DateTime(2013, 2,8), 13.8m },
                { new DateTime(2013, 2,11), 13.5m },
                { new DateTime(2013, 2,12), 14.78m },
                { new DateTime(2013, 2,13), 14.75m },
                { new DateTime(2013, 2,14), 14.55m },
                { new DateTime(2013, 2,15), 14.5m },
                { new DateTime(2013, 2,19), 13.89m },
                { new DateTime(2013, 2,20), 15.39m },
                { new DateTime(2013, 2,21), 15.5m },
                { new DateTime(2013, 2,22), 14.84m },
                { new DateTime(2013, 2,25), 17.65m },
                { new DateTime(2013, 2,26), 17.06m },
                { new DateTime(2013, 2,27), 15.43m },
                { new DateTime(2013, 2,28), 16.14m },
                { new DateTime(2013, 3,1), 16.39m },
                { new DateTime(2013, 3,4), 14.99m },
                { new DateTime(2013, 3,5), 14.54m },
                { new DateTime(2013, 3,6), 14.7m },
                { new DateTime(2013, 3,7), 14.19m },
                { new DateTime(2013, 3,8), 13.8m },
                { new DateTime(2013, 3,11), 13m },
                { new DateTime(2013, 3,12), 13.24m },
                { new DateTime(2013, 3,13), 12.94m },
                { new DateTime(2013, 3,14), 12.54m },
                { new DateTime(2013, 3,15), 12.54m },
                { new DateTime(2013, 3,18), 13.68m },
                { new DateTime(2013, 3,19), 14.79m }
            };

            //return the contracts requested
            _instrumentMgrMock.Setup(x => x.FindInstruments(null, It.IsAny<Instrument>())).Returns(ContinuousFuturesBrokerTestData.GetVIXFutures());

            var requests = new List<HistoricalDataRequest>();
            var futuresData = ContinuousFuturesBrokerTestData.GetVIXFuturesData();

            _cfInst.ContinuousFuture.RolloverDays = 2;

            //handle the requests for historical data
            int counter = 0;
            _clientMock.Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Returns(() => counter)
                .Callback<HistoricalDataRequest>(req =>
                    {
                        req.RequestID = counter;
                        requests.Add(req);
                        counter++;
                    });

            //hook up the event to receive the data
            var resultingData = new List<OHLCBar>();
            _broker.HistoricalDataArrived += (sender, e) =>
                {
                    resultingData = e.Data;
                };

            //make the request
            _broker.RequestHistoricalData(_req);

            //give back the contract data
            foreach (HistoricalDataRequest r in requests)
            {
                _clientMock.Raise(x => x.HistoricalDataReceived += null, new HistoricalDataEventArgs(r, futuresData[r.Instrument.ID.Value]));
            }

            //finally make sure we have correct continuous future prices
            foreach (OHLCBar bar in resultingData)
            {
                if(expectedPrices.ContainsKey(bar.DT))
                    Assert.AreEqual(expectedPrices[bar.DT], bar.Close, string.Format("At time: {0}", bar.DT));
            }
        }

        [Test]
        public void CorrectVolumeBasedSwitchover()
        {
            var expectedPrices = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2012, 10,19), 17.6m },
                { new DateTime(2012, 10,22), 17.36m },
                { new DateTime(2012, 10,23), 19.21m },
                { new DateTime(2012, 10,24), 18.75m },
                { new DateTime(2012, 10,25), 18.28m },
                { new DateTime(2012, 10,26), 18.27m },
                { new DateTime(2012, 10,31), 18.96m },
                { new DateTime(2012, 11,1), 16.9m },
                { new DateTime(2012, 11,2), 17.8m },
                { new DateTime(2012, 11,5), 18.1m },
                { new DateTime(2012, 11,6), 17.05m },
                { new DateTime(2012, 11,7), 18.86m },
                { new DateTime(2012, 11,8), 18.5m },
                { new DateTime(2012, 11,9), 18.59m },
                { new DateTime(2012, 11,12), 17.08m },
                { new DateTime(2012, 11,13), 18.16m },
                { new DateTime(2012, 11,14), 18.96m },
                { new DateTime(2012, 11,15), 19.11m },
                { new DateTime(2012, 11,16), 18.16m },
                { new DateTime(2012, 11,19), 16.58m },
                { new DateTime(2012, 11,20), 16.38m },
                { new DateTime(2012, 11,21), 16.44m },
                { new DateTime(2012, 11,23), 16.1m },
                { new DateTime(2012, 11,26), 15.6m },
                { new DateTime(2012, 11,27), 16.15m },
                { new DateTime(2012, 11,28), 15.48m },
                { new DateTime(2012, 11,29), 15.24m },
                { new DateTime(2012, 11,30), 15.54m },
                { new DateTime(2012, 12,3), 16.4m },
                { new DateTime(2012, 12,4), 16.49m },
                { new DateTime(2012, 12,5), 16.1m },
                { new DateTime(2012, 12,6), 16.39m },
                { new DateTime(2012, 12,7), 16.01m },
                { new DateTime(2012, 12,10), 16m },
                { new DateTime(2012, 12,11), 15.56m },
                { new DateTime(2012, 12,12), 16.11m },
                { new DateTime(2012, 12,13), 16.61m },
                { new DateTime(2012, 12,14), 16.86m },
                { new DateTime(2012, 12,17), 16.19m },
                { new DateTime(2012, 12,18), 15.55m },
                { new DateTime(2012, 12,19), 17.1m },
                { new DateTime(2012, 12,20), 17.51m },
                { new DateTime(2012, 12,21), 18.24m },
                { new DateTime(2012, 12,24), 18.64m },
                { new DateTime(2012, 12,26), 19.49m },
                { new DateTime(2012, 12,27), 19.09m },
                { new DateTime(2012, 12,28), 22.35m },
                { new DateTime(2012, 12,31), 17.68m },
                { new DateTime(2013, 1,2), 15.6m },
                { new DateTime(2013, 1,3), 15.9m },
                { new DateTime(2013, 1,4), 15.3m },
                { new DateTime(2013, 1,7), 14.75m },
                { new DateTime(2013, 1,8), 16.43m },
                { new DateTime(2013, 1,9), 16.37m },
                { new DateTime(2013, 1,10), 16.08m },
                { new DateTime(2013, 1,11), 15.98m },
                { new DateTime(2013, 1,14), 15.94m },
                { new DateTime(2013, 1,15), 15.76m },
                { new DateTime(2013, 1,16), 15.5m },
                { new DateTime(2013, 1,17), 15.69m },
                { new DateTime(2013, 1,18), 14.65m },
                { new DateTime(2013, 1,22), 14.05m },
                { new DateTime(2013, 1,23), 13.69m },
                { new DateTime(2013, 1,24), 13.94m },
                { new DateTime(2013, 1,25), 14.09m },
                { new DateTime(2013, 1,28), 14.59m },
                { new DateTime(2013, 1,29), 14.04m },
                { new DateTime(2013, 1,30), 15.15m },
                { new DateTime(2013, 1,31), 14.9m },
                { new DateTime(2013, 2,1), 14.29m },
                { new DateTime(2013, 2,4), 15.29m },
                { new DateTime(2013, 2,5), 14.39m },
                { new DateTime(2013, 2,6), 14.15m },
                { new DateTime(2013, 2,7), 14.09m },
                { new DateTime(2013, 2,8), 15.01m },
                { new DateTime(2013, 2,11), 14.93m },
                { new DateTime(2013, 2,12), 14.78m },
                { new DateTime(2013, 2,13), 14.75m },
                { new DateTime(2013, 2,14), 14.55m },
                { new DateTime(2013, 2,15), 14.5m },
                { new DateTime(2013, 2,19), 13.89m },
                { new DateTime(2013, 2,20), 15.39m },
                { new DateTime(2013, 2,21), 15.5m },
                { new DateTime(2013, 2,22), 14.84m },
                { new DateTime(2013, 2,25), 17.65m },
                { new DateTime(2013, 2,26), 17.06m },
                { new DateTime(2013, 2,27), 15.43m },
                { new DateTime(2013, 2,28), 16.14m },
                { new DateTime(2013, 3,1), 16.39m },
                { new DateTime(2013, 3,4), 14.99m },
                { new DateTime(2013, 3,5), 14.54m },
                { new DateTime(2013, 3,6), 14.7m },
                { new DateTime(2013, 3,7), 14.19m },
                { new DateTime(2013, 3,8), 13.8m },
                { new DateTime(2013, 3,11), 13m },
                { new DateTime(2013, 3,12), 13.24m },
                { new DateTime(2013, 3,13), 12.94m },
                { new DateTime(2013, 3,14), 12.54m },
                { new DateTime(2013, 3,15), 12.54m },
                { new DateTime(2013, 3,18), 13.68m },
                { new DateTime(2013, 3,19), 14.79m }
            };

            //return the contracts requested
            _instrumentMgrMock.Setup(x => x.FindInstruments(null, It.IsAny<Instrument>())).Returns(ContinuousFuturesBrokerTestData.GetVIXFutures());

            var requests = new List<HistoricalDataRequest>();
            var futuresData = ContinuousFuturesBrokerTestData.GetVIXFuturesData();

            _cfInst.ContinuousFuture.RolloverDays = 1;
            _cfInst.ContinuousFuture.RolloverType = ContinuousFuturesRolloverType.Volume;

            //handle the requests for historical data
            int counter = 0;
            _clientMock.Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Returns(() => counter)
                .Callback<HistoricalDataRequest>(req =>
                {
                    req.RequestID = counter;
                    requests.Add(req);
                    counter++;
                });

            //hook up the event to receive the data
            var resultingData = new List<OHLCBar>();
            _broker.HistoricalDataArrived += (sender, e) =>
            {
                resultingData = e.Data;
            };

            //make the request
            _broker.RequestHistoricalData(_req);

            //give back the contract data
            foreach (HistoricalDataRequest r in requests)
            {
                _clientMock.Raise(x => x.HistoricalDataReceived += null, new HistoricalDataEventArgs(r, futuresData[r.Instrument.ID.Value]));
            }

            //finally make sure we have correct continuous future prices
            foreach (OHLCBar bar in resultingData)
            {
                if (expectedPrices.ContainsKey(bar.DT))
                    Assert.AreEqual(expectedPrices[bar.DT], bar.Close, string.Format("At time: {0}", bar.DT));
            }


        }

        [Test]
        public void BrokerReturnsCorrectDateRange()
        {
            //return the contracts requested
            _instrumentMgrMock.Setup(x => x.FindInstruments(null, It.IsAny<Instrument>())).Returns(ContinuousFuturesBrokerTestData.GetVIXFutures());

            var requests = new List<HistoricalDataRequest>();
            var futuresData = ContinuousFuturesBrokerTestData.GetVIXFuturesData();

            _cfInst.ContinuousFuture.RolloverDays = 2;

            //handle the requests for historical data
            int counter = 0;
            _clientMock.Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Returns(() => counter)
                .Callback<HistoricalDataRequest>(req =>
                {
                    req.RequestID = counter;
                    requests.Add(req);
                    counter++;
                });

            //hook up the event to receive the data
            var resultingData = new List<OHLCBar>();
            _broker.HistoricalDataArrived += (sender, e) =>
            {
                resultingData = e.Data;
            };

            //make the request
            _broker.RequestHistoricalData(_req);

            //give back the contract data
            foreach (HistoricalDataRequest r in requests)
            {
                _clientMock.Raise(x => x.HistoricalDataReceived += null, new HistoricalDataEventArgs(r, futuresData[r.Instrument.ID.Value]));
            }

            //returned data needs to be within the StartingDate - EndingDate range
            Assert.IsTrue(_req.StartingDate <= resultingData[0].DT, "Starting Date");
            Assert.IsTrue(_req.EndingDate >= resultingData.Last().DT, "Ending Date");
        }

        [Test]
        public void CorrectOpenInterestBasedSwitchover()
        {
            var expectedPrices = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2012, 10,19), 17.6m },
                { new DateTime(2012, 10,22), 17.36m },
                { new DateTime(2012, 10,23), 19.21m },
                { new DateTime(2012, 10,24), 18.75m },
                { new DateTime(2012, 10,25), 18.28m },
                { new DateTime(2012, 10,26), 18.27m },
                { new DateTime(2012, 10,31), 18.96m },
                { new DateTime(2012, 11,1), 16.9m },
                { new DateTime(2012, 11,2), 17.8m },
                { new DateTime(2012, 11,5), 18.1m },
                { new DateTime(2012, 11,6), 17.05m },
                { new DateTime(2012, 11,7), 19.46m },
                { new DateTime(2012, 11,8), 19.26m },
                { new DateTime(2012, 11,9), 19.57m },
                { new DateTime(2012, 11,12), 18.31m },
                { new DateTime(2012, 11,13), 18.16m },
                { new DateTime(2012, 11,14), 18.96m },
                { new DateTime(2012, 11,15), 19.11m },
                { new DateTime(2012, 11,16), 18.16m },
                { new DateTime(2012, 11,19), 16.58m },
                { new DateTime(2012, 11,20), 16.38m },
                { new DateTime(2012, 11,21), 16.44m },
                { new DateTime(2012, 11,23), 16.1m },
                { new DateTime(2012, 11,26), 15.6m },
                { new DateTime(2012, 11,27), 16.15m },
                { new DateTime(2012, 11,28), 15.48m },
                { new DateTime(2012, 11,29), 15.24m },
                { new DateTime(2012, 11,30), 15.54m },
                { new DateTime(2012, 12,3), 16.4m },
                { new DateTime(2012, 12,4), 16.49m },
                { new DateTime(2012, 12,5), 16.1m },
                { new DateTime(2012, 12,6), 16.39m },
                { new DateTime(2012, 12,7), 16.01m },
                { new DateTime(2012, 12,10), 16m },
                { new DateTime(2012, 12,11), 16.46m },
                { new DateTime(2012, 12,12), 17.11m },
                { new DateTime(2012, 12,13), 17.31m },
                { new DateTime(2012, 12,14), 17.17m },
                { new DateTime(2012, 12,17), 16.46m },
                { new DateTime(2012, 12,18), 16.13m },
                { new DateTime(2012, 12,19), 17.1m },
                { new DateTime(2012, 12,20), 17.51m },
                { new DateTime(2012, 12,21), 18.24m },
                { new DateTime(2012, 12,24), 18.64m },
                { new DateTime(2012, 12,26), 19.49m },
                { new DateTime(2012, 12,27), 19.09m },
                { new DateTime(2012, 12,28), 22.35m },
                { new DateTime(2012, 12,31), 17.68m },
                { new DateTime(2013, 1,2), 15.6m },
                { new DateTime(2013, 1,3), 15.9m },
                { new DateTime(2013, 1,4), 15.3m },
                { new DateTime(2013, 1,7), 14.75m },
                { new DateTime(2013, 1,8), 14.65m },
                { new DateTime(2013, 1,9), 16.37m },
                { new DateTime(2013, 1,10), 16.08m },
                { new DateTime(2013, 1,11), 15.98m },
                { new DateTime(2013, 1,14), 15.94m },
                { new DateTime(2013, 1,15), 15.76m },
                { new DateTime(2013, 1,16), 15.5m },
                { new DateTime(2013, 1,17), 15.69m },
                { new DateTime(2013, 1,18), 14.65m },
                { new DateTime(2013, 1,22), 14.05m },
                { new DateTime(2013, 1,23), 13.69m },
                { new DateTime(2013, 1,24), 13.94m },
                { new DateTime(2013, 1,25), 14.09m },
                { new DateTime(2013, 1,28), 14.59m },
                { new DateTime(2013, 1,29), 14.04m },
                { new DateTime(2013, 1,30), 15.15m },
                { new DateTime(2013, 1,31), 14.9m },
                { new DateTime(2013, 2,1), 14.29m },
                { new DateTime(2013, 2,4), 15.86m },
                { new DateTime(2013, 2,5), 15.57m },
                { new DateTime(2013, 2,6), 15.33m },
                { new DateTime(2013, 2,7), 15.31m },
                { new DateTime(2013, 2,8), 15.01m },
                { new DateTime(2013, 2,11), 14.93m },
                { new DateTime(2013, 2,12), 14.78m },
                { new DateTime(2013, 2,13), 14.75m },
                { new DateTime(2013, 2,14), 14.55m },
                { new DateTime(2013, 2,15), 14.5m },
                { new DateTime(2013, 2,19), 13.89m },
                { new DateTime(2013, 2,20), 15.39m },
                { new DateTime(2013, 2,21), 15.5m },
                { new DateTime(2013, 2,22), 14.84m },
                { new DateTime(2013, 2,25), 17.65m },
                { new DateTime(2013, 2,26), 17.06m },
                { new DateTime(2013, 2,27), 15.43m },
                { new DateTime(2013, 2,28), 16.14m },
                { new DateTime(2013, 3,1), 16.39m },
                { new DateTime(2013, 3,4), 14.99m },
                { new DateTime(2013, 3,5), 14.54m },
                { new DateTime(2013, 3,6), 14.7m },
                { new DateTime(2013, 3,7), 14.19m },
                { new DateTime(2013, 3,8), 13.8m },
                { new DateTime(2013, 3,11), 13m },
                { new DateTime(2013, 3,12), 13.24m },
                { new DateTime(2013, 3,13), 12.94m },
                { new DateTime(2013, 3,14), 12.54m },
                { new DateTime(2013, 3,15), 12.54m },
                { new DateTime(2013, 3,18), 13.68m },
                { new DateTime(2013, 3,19), 14.79m }
            };

            //return the contracts requested
            _instrumentMgrMock.Setup(x => x.FindInstruments(null, It.IsAny<Instrument>())).Returns(ContinuousFuturesBrokerTestData.GetVIXFutures());

            var requests = new List<HistoricalDataRequest>();
            var futuresData = ContinuousFuturesBrokerTestData.GetVIXFuturesData();

            _cfInst.ContinuousFuture.RolloverDays = 1;
            _cfInst.ContinuousFuture.RolloverType = ContinuousFuturesRolloverType.OpenInterest;

            //handle the requests for historical data
            int counter = 0;
            _clientMock.Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Returns(() => counter)
                .Callback<HistoricalDataRequest>(req =>
                {
                    req.RequestID = counter;
                    requests.Add(req);
                    counter++;
                });

            //hook up the event to receive the data
            var resultingData = new List<OHLCBar>();
            _broker.HistoricalDataArrived += (sender, e) =>
            {
                resultingData = e.Data;
            };

            //make the request
            _broker.RequestHistoricalData(_req);

            //give back the contract data
            foreach (HistoricalDataRequest r in requests)
            {
                _clientMock.Raise(x => x.HistoricalDataReceived += null, new HistoricalDataEventArgs(r, futuresData[r.Instrument.ID.Value]));
            }

            //finally make sure we have correct continuous future prices
            foreach (OHLCBar bar in resultingData)
            {
                if (expectedPrices.ContainsKey(bar.DT))
                    Assert.AreEqual(expectedPrices[bar.DT], bar.Close, string.Format("At time: {0}", bar.DT));
            }
        }

        [Test]
        public void CorrectOpenInterestAndVolumeBasedSwitchover()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void CorrectOpenInterestOrVolumeBasedSwitchover()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void CorrectContinuousPricesWithRatioAdjustment()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void CorrectContinuousPricesWithDifferenceAdjustment()
        {
            var expectedPrices = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2012, 11,21), 20.36m },
                { new DateTime(2012, 11,23), 20.02m },
                { new DateTime(2012, 11,26), 19.52m },
                { new DateTime(2012, 11,27), 20.07m },
                { new DateTime(2012, 11,28), 19.4m },
                { new DateTime(2012, 11,29), 19.16m },
                { new DateTime(2012, 11,30), 19.46m },
                { new DateTime(2012, 12,3), 20.32m },
                { new DateTime(2012, 12,4), 20.41m },
                { new DateTime(2012, 12,5), 20.02m },
                { new DateTime(2012, 12,6), 20.31m },
                { new DateTime(2012, 12,7), 19.93m },
                { new DateTime(2012, 12,10), 19.92m },
                { new DateTime(2012, 12,11), 19.48m },
                { new DateTime(2012, 12,12), 20.03m },
                { new DateTime(2012, 12,13), 20.53m },
                { new DateTime(2012, 12,14), 20.78m },
                { new DateTime(2012, 12,17), 20.11m },
                { new DateTime(2012, 12,18), 19.47m },
                { new DateTime(2012, 12,19), 20.44m },
                { new DateTime(2012, 12,20), 20.85m },
                { new DateTime(2012, 12,21), 21.58m },
                { new DateTime(2012, 12,24), 21.98m },
                { new DateTime(2012, 12,26), 22.83m },
                { new DateTime(2012, 12,27), 22.43m },
                { new DateTime(2012, 12,28), 25.69m },
                { new DateTime(2012, 12,31), 21.02m },
                { new DateTime(2013, 1,2), 18.94m },
                { new DateTime(2013, 1,3), 19.24m },
                { new DateTime(2013, 1,4), 18.64m },
                { new DateTime(2013, 1,7), 18.09m },
                { new DateTime(2013, 1,8), 17.99m },
                { new DateTime(2013, 1,9), 18.04m },
                { new DateTime(2013, 1,10), 17.54m },
                { new DateTime(2013, 1,11), 17.48m },
                { new DateTime(2013, 1,14), 17.43m },
                { new DateTime(2013, 1,15), 17.54m },
                { new DateTime(2013, 1,16), 17.28m },
                { new DateTime(2013, 1,17), 17.47m },
                { new DateTime(2013, 1,18), 16.43m },
                { new DateTime(2013, 1,22), 15.83m },
                { new DateTime(2013, 1,23), 15.47m },
                { new DateTime(2013, 1,24), 15.72m },
                { new DateTime(2013, 1,25), 15.87m },
                { new DateTime(2013, 1,28), 16.37m },
                { new DateTime(2013, 1,29), 15.82m },
                { new DateTime(2013, 1,30), 16.93m },
                { new DateTime(2013, 1,31), 16.68m },
                { new DateTime(2013, 2,1), 16.07m },
                { new DateTime(2013, 2,4), 17.07m },
                { new DateTime(2013, 2,5), 16.17m },
                { new DateTime(2013, 2,6), 15.93m },
                { new DateTime(2013, 2,7), 15.87m },
                { new DateTime(2013, 2,8), 15.58m },
                { new DateTime(2013, 2,11), 15.28m },
                { new DateTime(2013, 2,12), 14.78m },
                { new DateTime(2013, 2,13), 14.75m },
                { new DateTime(2013, 2,14), 14.55m },
                { new DateTime(2013, 2,15), 14.5m },
                { new DateTime(2013, 2,19), 13.89m },
                { new DateTime(2013, 2,20), 15.39m },
                { new DateTime(2013, 2,21), 15.5m },
                { new DateTime(2013, 2,22), 14.84m },
                { new DateTime(2013, 2,25), 17.65m },
                { new DateTime(2013, 2,26), 17.06m },
                { new DateTime(2013, 2,27), 15.43m },
                { new DateTime(2013, 2,28), 16.14m },
                { new DateTime(2013, 3,1), 16.39m },
                { new DateTime(2013, 3,4), 14.99m },
                { new DateTime(2013, 3,5), 14.54m },
                { new DateTime(2013, 3,6), 14.7m },
                { new DateTime(2013, 3,7), 14.19m },
                { new DateTime(2013, 3,8), 13.8m },
                { new DateTime(2013, 3,11), 13m },
                { new DateTime(2013, 3,12), 13.24m },
                { new DateTime(2013, 3,13), 12.94m },
                { new DateTime(2013, 3,14), 12.54m },
                { new DateTime(2013, 3,15), 12.54m },
                { new DateTime(2013, 3,18), 13.68m },
                { new DateTime(2013, 3,19), 14.79m }
            };

            _req.EndingDate = new DateTime(2013, 3, 1);

            //return the contracts requested
            _instrumentMgrMock.Setup(x => x.FindInstruments(null, It.IsAny<Instrument>())).Returns(ContinuousFuturesBrokerTestData.GetVIXFutures());

            var requests = new List<HistoricalDataRequest>();
            var futuresData = ContinuousFuturesBrokerTestData.GetVIXFuturesData();

            _cfInst.ContinuousFuture.RolloverDays = 2;
            _cfInst.ContinuousFuture.AdjustmentMode = ContinuousFuturesAdjustmentMode.Difference;

            //handle the requests for historical data
            int counter = 0;
            _clientMock.Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Returns(() => counter)
                .Callback<HistoricalDataRequest>(req =>
                {
                    req.RequestID = counter;
                    requests.Add(req);
                    counter++;
                });

            //hook up the event to receive the data
            var resultingData = new List<OHLCBar>();
            _broker.HistoricalDataArrived += (sender, e) =>
            {
                resultingData = e.Data;
            };

            //make the request
            _broker.RequestHistoricalData(_req);

            //give back the contract data
            foreach (HistoricalDataRequest r in requests)
            {
                _clientMock.Raise(x => x.HistoricalDataReceived += null, new HistoricalDataEventArgs(r, futuresData[r.Instrument.ID.Value]));
            }

            //finally make sure we have correct continuous future prices
            foreach (OHLCBar bar in resultingData)
            {
                if (expectedPrices.ContainsKey(bar.DT))
                    Assert.AreEqual(expectedPrices[bar.DT], bar.Close, string.Format("At time: {0}", bar.DT));
            }
        }

        [Test]
        public void CorrectContinuousPricesWithNoAdjustment()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void CorrectContinuousPricesOfNthMonthContract()
        {
            _cfInst.ContinuousFuture.Month = 2;
        }

        public void Dispose()
        {
            if (_broker != null)
            {
                _broker.Dispose();
                _broker = null;
            }
        }


        
    }
}
