using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using QDMS;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class RealTimeDataBrokerTest
    {
        private RealTimeDataBroker _broker;
        private Mock<IRealTimeDataSource> _ds;

        [SetUp]
        public void SetUp()
        {
            _ds = new Mock<IRealTimeDataSource>();
            _broker = new RealTimeDataBroker(5555, 5556, new List<IRealTimeDataSource> { _ds.Object });
        }

        [Test]
        public void RequestsCorrectFuturesContractForContinuousFutures()
        {
            Assert.IsTrue(false);

        }
    }
}
