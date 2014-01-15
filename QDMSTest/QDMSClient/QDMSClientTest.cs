using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QDMS;

namespace QDMSTest
{
    [TestFixture]
    public class QDMSClientTest
    {
        private QDMSClient.QDMSClient _client;

        [SetUp]
        public void SetUp()
        {
            _client = new QDMSClient.QDMSClient("testingclient", "127.0.0.1", 5555, 5556, 5557, 5558);
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
        }

        [Test]
        public void RequestHistoricalDataReturnsMinusOneWhenDatesAreWrong()
        {
            var req = new HistoricalDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY" },
                StartingDate = new DateTime(2012, 1, 1),
                EndingDate = new DateTime(2011, 1, 1),
                Frequency = BarSize.OneDay
            };

            Assert.AreEqual(-1, _client.RequestHistoricalData(req));
        }

        [Test]
        public void RequestHistoricalDataRaisesErrorEventWhenDatesAreWrong()
        {
            var req = new HistoricalDataRequest
            {
                Instrument = new Instrument { ID = 1, Symbol = "SPY" },
                StartingDate = new DateTime(2012, 1, 1),
                EndingDate = new DateTime(2011, 1, 1),
                Frequency = BarSize.OneDay
            };

            bool errorTriggered = false;
            _client.Error += (sender, e) => errorTriggered = true;

            _client.RequestHistoricalData(req);

            Assert.IsTrue(errorTriggered);
        }

        [Test]
        public void RequestHistoricalDataReturnsMinusOneWhenNotConnected()
        {
        }

        [Test]
        public void RequestHistoricalDataRaisesErrorEventWhenNotConnected()
        {
        }
    }
}
