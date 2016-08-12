// -----------------------------------------------------------------------
// <copyright file="HistoricalDataBrokerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
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
        private Mock<IContinuousFuturesBroker> _cfBrokerMock;
        private Instrument _instrument;

        [SetUp]
        public void SetUp()
        {
            _instrument = new Instrument
            {
                ID = 1,
                Symbol = "SPY",
                Datasource = new Datasource { ID = 1, Name = "MockSource" }
            };

            _instrument.Exchange = new Exchange()
            {
                ID = 1,
                Name = "Exchange",
                Timezone = "Eastern Standard Time"
            };

            _dataSourceMock = new Mock<IHistoricalDataSource>();
            _dataSourceMock.SetupGet(x => x.Name).Returns("MockSource");
            _dataSourceMock.SetupGet(x => x.Connected).Returns(false);

            _localStorageMock = new Mock<IDataStorage>();

            _cfBrokerMock = new Mock<IContinuousFuturesBroker>();
            _cfBrokerMock.SetupGet(x => x.Connected).Returns(true);

            _broker = new HistoricalDataBroker(_cfBrokerMock.Object, _localStorageMock.Object, new List<IHistoricalDataSource> { _dataSourceMock.Object });

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

            var request = new DataAdditionRequest(BarSize.OneDay, _instrument, data, true);

            _broker.AddData(request);
            _localStorageMock.Verify(x => x.AddData(
                It.Is<List<OHLCBar>>(b => b.Count == 1 && b[0].Close == 4),
                It.Is<Instrument>(i => i.ID == 1 && i.Symbol == "SPY" && i.Datasource.Name == "MockSource"),
                It.Is<BarSize>(z => z == BarSize.OneDay),
                It.Is<bool>(k => k == true),
                It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public void HistoricalDataRequestsAreForwardedToTheCorrectDataSource()
        {
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                dataLocation: DataLocation.ExternalOnly,
                saveToLocalStorage: false,
                rthOnly: true);

            _broker.RequestHistoricalData(request);

            _dataSourceMock.Verify(x => x.RequestHistoricalData(
                It.Is<HistoricalDataRequest>(
                    i =>
                        i.Instrument.ID == 1 &&
                        i.Frequency == BarSize.OneDay &&
                        i.StartingDate.Year == 2012 &&
                        i.StartingDate.Month == 1 &&
                        i.StartingDate.Day == 1 &&
                        i.EndingDate.Year == 2013 &&
                        i.EndingDate.Month == 1 &&
                        i.EndingDate.Day == 1 &&
                        i.DataLocation == DataLocation.ExternalOnly && 
                        i.SaveDataToStorage == false &&
                        i.RTHOnly == true)), Times.Once);
        }

        [Test]
        public void RequestEndingTimesAreCorrectlyConstrainedToThePresentTimeInTheInstrumentsTimeZone()
        {
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2100, 1, 1),
                dataLocation: DataLocation.ExternalOnly,
                saveToLocalStorage: false,
                rthOnly: true);

            var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var now = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, est);

            var modifiedRequest = new HistoricalDataRequest();
            _dataSourceMock.Setup(x => x
                .RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Callback<HistoricalDataRequest>(req => modifiedRequest = req);

            _broker.RequestHistoricalData(request);

            Assert.AreEqual(now.Year, modifiedRequest.EndingDate.Year, string.Format("Expected: {0} Was: {1}", modifiedRequest.EndingDate.Year, now.Year));
            Assert.AreEqual(now.Month, modifiedRequest.EndingDate.Month, string.Format("Expected: {0} Was: {1}", modifiedRequest.EndingDate.Month, now.Month));
            Assert.AreEqual(now.Day, modifiedRequest.EndingDate.Day, string.Format("Expected: {0} Was: {1}", modifiedRequest.EndingDate.Day, now.Day));
            Assert.AreEqual(now.Hour, modifiedRequest.EndingDate.Hour, string.Format("Expected: {0} Was: {1}", modifiedRequest.EndingDate.Hour, now.Hour));
            Assert.AreEqual(now.Minute, modifiedRequest.EndingDate.Minute, string.Format("Expected: {0} Was: {1}", modifiedRequest.EndingDate.Minute, now.Minute));
        }

        [Test]
        public void RequestsAreCorrectlySplitIntoSubrequestsWhenOnlyPartOfTheDataIsAvailable()
        {
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                dataLocation: DataLocation.Both,
                saveToLocalStorage: false,
                rthOnly: true);

            StoredDataInfo sdInfo = new StoredDataInfo()
            {
                EarliestDate = new DateTime(2012, 6, 1),
                LatestDate = new DateTime(2012, 9, 1),
                Frequency = BarSize.OneDay,
                InstrumentID = 1
            };

            _localStorageMock.Setup(x => x.GetStorageInfo(1, BarSize.OneDay)).Returns(sdInfo);

            _broker.RequestHistoricalData(request);

            //first subrequest
            _dataSourceMock.Verify(x => x.RequestHistoricalData(
                It.Is<HistoricalDataRequest>(y =>
                    y.StartingDate.Month == 1 &&
                    y.StartingDate.Day == 1 &&
                    y.EndingDate.Month == 5 &&
                    y.EndingDate.Day == 31
                    )), Times.Once);

            //second subrequest
            _dataSourceMock.Verify(x => x.RequestHistoricalData(
                It.Is<HistoricalDataRequest>(y =>
                    y.StartingDate.Month == 9 &&
                    y.StartingDate.Day == 1 &&
                    y.EndingDate.Month == 1 &&
                    y.EndingDate.Day == 1
                    )), Times.Once);
        }

        [Test]
        public void ForceFreshDataFlagIsObeyed()
        {
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                dataLocation: DataLocation.ExternalOnly,
                saveToLocalStorage: false,
                rthOnly: true);

            _broker.RequestHistoricalData(request);

            _localStorageMock.Verify(x => x.GetStorageInfo(It.IsAny<int>(), It.IsAny<BarSize>()), Times.Never);
            _localStorageMock.Verify(x => x.GetData(It.IsAny<Instrument>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<BarSize>()), Times.Never);
        }

        [Test]
        public void LocalStorageOnlyFlagIsObeyed()
        {
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                dataLocation: DataLocation.LocalOnly,
                saveToLocalStorage: false,
                rthOnly: true);

            _broker.RequestHistoricalData(request);

            _dataSourceMock.Verify(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()), Times.Never);
            _localStorageMock.Verify(x => x.RequestHistoricalData(
                It.Is<HistoricalDataRequest>(
                    i =>
                        i.Instrument.ID == 1 &&
                        i.Frequency == BarSize.OneDay &&
                        i.StartingDate.Year == 2012 &&
                        i.StartingDate.Month == 1 &&
                        i.StartingDate.Day == 1 &&
                        i.EndingDate.Year == 2013 &&
                        i.EndingDate.Month == 1 &&
                        i.EndingDate.Day == 1 &&
                        i.DataLocation == DataLocation.LocalOnly && 
                        i.SaveDataToStorage == false &&
                        i.RTHOnly == true)));
        }

        [Test]
        public void SavesToLocalStorageWhenSaveToLocalStorageFlagIsSet()
        {
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                dataLocation: DataLocation.ExternalOnly,
                saveToLocalStorage: true,
                rthOnly: true);

            var data = new List<OHLCBar>
            {
                new OHLCBar {Open = 1, High = 2, Low = 3, Close = 4, DT = new DateTime(2000, 1, 1) }
            };

            //we need to set up a callback with the request after it has had an AssignedID assigned to it.
            HistoricalDataRequest newRequest = new HistoricalDataRequest();
            _dataSourceMock
                .Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Callback<HistoricalDataRequest>(req => newRequest = req);

            _broker.RequestHistoricalData(request);

            _dataSourceMock.Raise(x => x.HistoricalDataArrived += null, new HistoricalDataEventArgs(newRequest, data));

            _localStorageMock.Verify(
                x => x.AddData(
                    It.Is<List<OHLCBar>>(y => y.Count == 1),
                    It.Is<Instrument>(y => y.ID == 1),
                    It.Is<BarSize>(y => y == BarSize.OneDay),
                    It.Is<bool>(y => y == true),
                    It.IsAny<bool>()),
                    Times.Once);
        }

        [Test]
        public void DoesNotSaveToLocalStorageWhenSaveToLocalStorageFlagIsNotSet()
        {
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                dataLocation: DataLocation.ExternalOnly,
                saveToLocalStorage: false,
                rthOnly: true);

            var data = new List<OHLCBar>
            {
                new OHLCBar {Open = 1, High = 2, Low = 3, Close = 4, DT = new DateTime(2000, 1, 1) }
            };

            //we need to set up a callback with the request after it has had an AssignedID assigned to it.
            HistoricalDataRequest newRequest = new HistoricalDataRequest();
            _dataSourceMock
                .Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Callback<HistoricalDataRequest>(req => newRequest = req);

            _broker.RequestHistoricalData(request);

            _dataSourceMock.Raise(x => x.HistoricalDataArrived += null, new HistoricalDataEventArgs(newRequest, data));

            _localStorageMock.Verify(
                x => x.AddData(It.IsAny<List<OHLCBar>>(), It.IsAny<Instrument>(), It.IsAny<BarSize>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public void DataRequestsOnContinuousFuturesAreForwardedToTheCFBroker()
        {
            _instrument.IsContinuousFuture = true;

            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                dataLocation: DataLocation.ExternalOnly,
                saveToLocalStorage: false,
                rthOnly: true);

            _broker.RequestHistoricalData(request);

            _cfBrokerMock.Verify(x => x.RequestHistoricalData(
                It.Is<HistoricalDataRequest>(
                    i =>
                        i.Instrument.ID == 1 &&
                        i.Frequency == BarSize.OneDay &&
                        i.StartingDate.Year == 2012 &&
                        i.StartingDate.Month == 1 &&
                        i.StartingDate.Day == 1 &&
                        i.EndingDate.Year == 2013 &&
                        i.EndingDate.Month == 1 &&
                        i.EndingDate.Day == 1 &&
                        i.DataLocation == DataLocation.ExternalOnly &&
                        i.SaveDataToStorage == false &&
                        i.RTHOnly == true)), Times.Once);
        }

        [Test]
        public void RegularTradingHoursAreFilteredWhenRTHFlagIsSet()
        {
            _instrument.Sessions = new List<InstrumentSession>
            {
                new InstrumentSession 
                { 
                    OpeningDay = DayOfTheWeek.Sunday, 
                    OpeningTime = new TimeSpan(8,0,0),
                    ClosingDay = DayOfTheWeek.Sunday,
                    ClosingTime = new TimeSpan(15, 0, 0)
                }
            };

            var request = new HistoricalDataRequest(
                _instrument, 
                BarSize.FiveMinutes, 
                new DateTime(2012, 1, 1, 5, 0, 0), 
                new DateTime(2012, 1, 1, 18, 0, 0),
                dataLocation: DataLocation.ExternalOnly,
                saveToLocalStorage: true,
                rthOnly: true);

            var data = new List<OHLCBar>();

            //generate the data
            DateTime currentDate = request.StartingDate;
            while (currentDate < request.EndingDate)
            {
                data.Add(new OHLCBar { Open = 1, High = 2, Low = 3, Close = 4, DT = currentDate });
                currentDate = currentDate.AddMinutes(5);
            }

            //we need to set up a callback with the request after it has had an AssignedID assigned to it.
            HistoricalDataRequest newRequest = new HistoricalDataRequest();
            _dataSourceMock
                .Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Callback<HistoricalDataRequest>(req => newRequest = req);

            //hook up the data return event
            var returnedData = new List<OHLCBar>();
            _broker.HistoricalDataArrived += (a, s) => returnedData.AddRange(s.Data);

            //send in the request
            _broker.RequestHistoricalData(request);

            //fake the data return from the datasource
            _dataSourceMock.Raise(x => x.HistoricalDataArrived += null, new HistoricalDataEventArgs(newRequest, data));

            //now verify what we get back
            Assert.AreEqual(0, returnedData.Count(x => x.DT.TimeOfDay < _instrument.Sessions.First().OpeningTime));
            Assert.AreEqual(0, returnedData.Count(x => x.DT.TimeOfDay > _instrument.Sessions.First().ClosingTime));
        }

        [Test]
        public void DataArrivedEventIsRaisedWhenDataSourceReturnsData()
        {
            bool eventRaised = false;

            _broker.HistoricalDataArrived += (sender, e) => eventRaised = true;

            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                dataLocation: DataLocation.ExternalOnly,
                saveToLocalStorage: false,
                rthOnly: true);

            var data = new List<OHLCBar>
            {
                new OHLCBar {Open = 1, High = 2, Low = 3, Close = 4, DT = new DateTime(2000, 1, 1) }
            };

            //we need to set up a callback with the request after it has had an AssignedID assigned to it.
            HistoricalDataRequest newRequest = new HistoricalDataRequest();
            _dataSourceMock
                .Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Callback<HistoricalDataRequest>(req => newRequest = req);

            _broker.RequestHistoricalData(request);

            _dataSourceMock.Raise(x => x.HistoricalDataArrived += null, new HistoricalDataEventArgs(newRequest, data));

            Assert.IsTrue(eventRaised);
        }


        [Test]
        public void BrokerConnectsToDataSources()
        {
            _dataSourceMock.Verify(x => x.Connect(), Times.Once);
        }

        [Test]
        public void RequestsAreGivenAUniqueAssignedIDGreaterThanZero()
        {
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                dataLocation: DataLocation.ExternalOnly,
                saveToLocalStorage: false,
                rthOnly: true);

            var assignedIDs = new List<int>();
            _dataSourceMock
                .Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Callback<HistoricalDataRequest>(req => assignedIDs.Add(req.AssignedID));

            for (int i = 0; i < 3; i++)
            {
            	_broker.RequestHistoricalData(request);
            }

            Assert.AreEqual(3, assignedIDs.Count);
            Assert.AreEqual(3, assignedIDs.Distinct().Count());
            Assert.AreEqual(0, assignedIDs.Count(x => x < 0));
        }

        [Test]
        public void ThrowsExceptionWhenMakingRequestForInstrumentWithDataSourceThatDoesNotExist()
        {
            _instrument.Datasource.Name = "ASDfasdf___________________aasdf";
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                 dataLocation: DataLocation.Both,
                 saveToLocalStorage: false,
                 rthOnly: true);
            Assert.Throws<Exception>(() => _broker.RequestHistoricalData(request));
        }

        [Test]
        public void ThrowsExceptionWhenMakingRequestForInstrumentWithDataSourceThatDoesNotExistAndForcingFreshData()
        {
            _instrument.Datasource.Name = "ASDfasdf___________________aasdf";
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                 dataLocation: DataLocation.ExternalOnly,
                 saveToLocalStorage: false,
                 rthOnly: true);
            Assert.Throws<Exception>(() => _broker.RequestHistoricalData(request));
        }

        [Test]
        public void ThrowsExceptionWhenMakingRequestForInstrumentWithDataSourceThatIsDisconnected()
        {
            var request = new HistoricalDataRequest(_instrument, BarSize.OneDay, new DateTime(2012, 1, 1), new DateTime(2013, 1, 1),
                dataLocation: DataLocation.ExternalOnly,
                 saveToLocalStorage: false,
                 rthOnly: true);

            _dataSourceMock.SetupGet(x => x.Connected).Returns(false);

            Assert.Throws<Exception>(() =>_broker.RequestHistoricalData(request));
        }
    }
}