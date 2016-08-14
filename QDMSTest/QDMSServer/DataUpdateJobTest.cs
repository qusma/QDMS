using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EntityData;
using QDMS;
using QDMSServer;
using Moq;
using NUnit.Framework;
using Quartz;

namespace QDMSTest
{
    [TestFixture]
    public class DataUpdateJobTest
    {
        private Mock<IHistoricalDataBroker> _brokerMock;
        private Mock<IEmailService> _mailMock;
        private Mock<IDataStorage> _localStorageMock;
        private Mock<IJobExecutionContext> _contextMock;
        private Mock<IInstrumentSource> _instrumentManagerMock;
            
        [SetUp]
        public void SetUp()
        {
            _brokerMock = new Mock<IHistoricalDataBroker>();
            _mailMock = new Mock<IEmailService>();
            _localStorageMock = new Mock<IDataStorage>();
            _contextMock = new Mock<IJobExecutionContext>();
            _instrumentManagerMock = new Mock<IInstrumentSource>();

            var jobDetailMock = new Mock<IJobDetail>();
            IDictionary<string,object> detailsMap = new Dictionary<string,object>();
            detailsMap.Add("details", new DataUpdateJobDetails() { Name = "mockjob", Frequency = BarSize.OneDay });

            jobDetailMock.Setup(x => x.JobDataMap).Returns(new JobDataMap(detailsMap));

            _contextMock.Setup(x => x.JobDetail).Returns(jobDetailMock.Object);
        }

        [Test]
        public void JobRequestsCorrectData()
        {
            //TODO write
        }

        [Test]
        public void EmailReportSentOnBrokerError()
        {
            var settings = new UpdateJobSettings(errors: true, timeout: 1, toEmail: "test@test.test", fromEmail: "test@test.test");
            var job = new DataUpdateJob(_brokerMock.Object, _mailMock.Object, settings, _localStorageMock.Object, _instrumentManagerMock.Object);

            _brokerMock
                .Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Throws(new Exception("TestException123"));

            Instrument inst = new Instrument() { ID = 1, Symbol = "SPY", Currency = "USD", Type = InstrumentType.Stock };
            _instrumentManagerMock
                .Setup(x => x.FindInstruments(It.IsAny<Expression<Func<Instrument, bool>>>(), It.IsAny<MyDBContext>()))
                .Returns(new List<Instrument>() { inst });

            _localStorageMock
                .Setup(x => x.GetStorageInfo(It.IsAny<int>()))
                .Returns(new List<StoredDataInfo>() { 
                    new StoredDataInfo() 
                    { 
                        Frequency = BarSize.OneDay, 
                        InstrumentID = inst.ID.Value, 
                        LatestDate = DateTime.Now.AddDays(-2)
                    } });

            job.Execute(_contextMock.Object);

            _mailMock.Verify(x => 
                x.Send(
                    It.IsAny<string>(), 
                    It.Is<string>(y => y == "test@test.test"), 
                    It.IsAny<string>(), 
                    It.Is<string>(y => y.Contains("TestException123"))));
        }

        [Test]
        public void NoEmailReportSentIfEmailIsEmpty()
        {
            var settings = new UpdateJobSettings(errors: true, timeout: 1, toEmail: "");
            var job = new DataUpdateJob(_brokerMock.Object, _mailMock.Object, settings, _localStorageMock.Object, _instrumentManagerMock.Object);

            _brokerMock
                .Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Throws(new Exception("TestException123"));

            Instrument inst = new Instrument() { ID = 1, Symbol = "SPY", Currency = "USD", Type = InstrumentType.Stock };
            _instrumentManagerMock
                .Setup(x => x.FindInstruments(It.IsAny<Expression<Func<Instrument, bool>>>(), It.IsAny<MyDBContext>()))
                .Returns(new List<Instrument>() { inst });

            _localStorageMock
                .Setup(x => x.GetStorageInfo(It.IsAny<int>()))
                .Returns(new List<StoredDataInfo>() { 
                    new StoredDataInfo() 
                    { 
                        Frequency = BarSize.OneDay, 
                        InstrumentID = inst.ID.Value, 
                        LatestDate = DateTime.Now.AddDays(-2)
                    } });

            job.Execute(_contextMock.Object);

            _mailMock.Verify(x =>
                x.Send(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                    Times.Never);
        }

        [Test]
        public void EmailReportSentOnUnfulfilledRequests()
        {
            var settings = new UpdateJobSettings(errors: true, timeout: 1, toEmail: "test@test.test", fromEmail: "test@test.test");
            var job = new DataUpdateJob(_brokerMock.Object, _mailMock.Object, settings, _localStorageMock.Object, _instrumentManagerMock.Object);

            Instrument inst = new Instrument() { ID = 1, Symbol = "SPY", Currency = "USD", Type = InstrumentType.Stock };
            _instrumentManagerMock
                .Setup(x => x.FindInstruments(It.IsAny<Expression<Func<Instrument, bool>>>(), It.IsAny<MyDBContext>()))
                .Returns(new List<Instrument>() { inst });

            _localStorageMock
                .Setup(x => x.GetStorageInfo(It.IsAny<int>()))
                .Returns(new List<StoredDataInfo>() { 
                    new StoredDataInfo() 
                    { 
                        Frequency = BarSize.OneDay, 
                        InstrumentID = inst.ID.Value, 
                        LatestDate = DateTime.Now.AddDays(-2)
                    } });

            job.Execute(_contextMock.Object);

            _mailMock.Verify(x => 
                x.Send(
                    It.IsAny<string>(), 
                    It.Is<string>(y => y == "test@test.test"), 
                    It.IsAny<string>(),
                    It.Is<string>(y => y.Contains("could not be fulfilled"))));
        }

        [Test]
        public void EmailReportSentOnNoData()
        {
            var settings = new UpdateJobSettings(errors: true, timeout: 5, toEmail: "test@test.test", fromEmail: "test@test.test");
            var job = new DataUpdateJob(_brokerMock.Object, _mailMock.Object, settings, _localStorageMock.Object, _instrumentManagerMock.Object);

            Instrument inst = new Instrument() { ID = 1, Symbol = "SPY", Currency = "USD", Type = InstrumentType.Stock };
            _instrumentManagerMock
                .Setup(x => x.FindInstruments(It.IsAny<Expression<Func<Instrument, bool>>>(), It.IsAny<MyDBContext>()))
                .Returns(new List<Instrument>() { inst });

            _localStorageMock
                .Setup(x => x.GetStorageInfo(It.IsAny<int>()))
                .Returns(new List<StoredDataInfo>() { 
                    new StoredDataInfo() 
                    { 
                        Frequency = BarSize.OneDay, 
                        InstrumentID = inst.ID.Value, 
                        LatestDate = DateTime.Now.AddDays(-2)
                    } });
            
            HistoricalDataRequest req = null;
            _brokerMock
                .Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Callback<HistoricalDataRequest>(y => req = y);

            Task.Run(() => job.Execute(_contextMock.Object));

            Thread.Sleep(2000);

            _brokerMock.Raise(x => x.HistoricalDataArrived += null, new HistoricalDataEventArgs(req, new List<OHLCBar>()));

            Thread.Sleep(2000);

            _mailMock.Verify(x =>
                x.Send(
                    It.IsAny<string>(),
                    It.Is<string>(y => y == "test@test.test"),
                    It.IsAny<string>(),
                    It.Is<string>(y => y.Contains("downloaded 0 bars"))));
        }

        [Test]
        public void EmailReportSentOnAbnormalData()
        {
            var settings = new UpdateJobSettings(errors: true, timeout: 5, toEmail: "test@test.test", fromEmail: "test@test.test");
            var job = new DataUpdateJob(_brokerMock.Object, _mailMock.Object, settings, _localStorageMock.Object, _instrumentManagerMock.Object);

            Instrument inst = new Instrument() { ID = 1, Symbol = "SPY", Currency = "USD", Type = InstrumentType.Stock };
            _instrumentManagerMock
                .Setup(x => x.FindInstruments(It.IsAny<Expression<Func<Instrument, bool>>>(), It.IsAny<MyDBContext>()))
                .Returns(new List<Instrument>() { inst });

            _localStorageMock
                .Setup(x => x.GetStorageInfo(It.IsAny<int>()))
                .Returns(new List<StoredDataInfo>() { 
                    new StoredDataInfo() 
                    { 
                        Frequency = BarSize.OneDay, 
                        InstrumentID = inst.ID.Value, 
                        LatestDate = DateTime.Now.AddDays(-2)
                    } });

            HistoricalDataRequest req = null;
            _brokerMock
                .Setup(x => x.RequestHistoricalData(It.IsAny<HistoricalDataRequest>()))
                .Callback<HistoricalDataRequest>(y => 
                    {
                        req = y;

                        //Return data with an obvious "break"
                        var data = new List<OHLCBar>
                        {
                            new OHLCBar() { Open = 1m, High = 1.5m, Low = 0.9m, Close = 1.1m, DT = req.StartingDate.AddDays(-1)},
                            new OHLCBar() { Open = 2m, High = 3.5m, Low = 1.9m, Close = 2.1m, DT = req.StartingDate},
                        };

                        _localStorageMock
                            .Setup(x => x.GetData(It.IsAny<Instrument>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<BarSize>()))
                            .Returns(data);
                    });

            Task.Run(() => job.Execute(_contextMock.Object));

            Thread.Sleep(2000);

            _brokerMock.Raise(x => x.HistoricalDataArrived += null, new HistoricalDataEventArgs(req, new List<OHLCBar>()));

            Thread.Sleep(100);

            _mailMock.Verify(x =>
                x.Send(
                    It.IsAny<string>(),
                    It.Is<string>(y => y == "test@test.test"),
                    It.IsAny<string>(),
                    It.Is<string>(y => y.Contains("Possible dirty data detected"))));
        }


    }
}
