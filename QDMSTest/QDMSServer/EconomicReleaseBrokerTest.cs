// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseBrokerTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Moq;
using NUnit.Framework;
using QDMS;
using QDMS.Server.Brokers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QDMSTest
{
    [TestFixture]
    public class EconomicReleaseBrokerTest
    {
        private Mock<IEconomicReleaseSource> _source1;
        private Mock<IEconomicReleaseSource> _source2;
        private EconomicReleaseBroker _broker;

        [SetUp]
        public void SetUp()
        {
            _source1 = new Mock<IEconomicReleaseSource>();
            _source1.SetupGet(x => x.Name).Returns("Source1");
            _source2 = new Mock<IEconomicReleaseSource>();
            _source2.SetupGet(x => x.Name).Returns("Source2");

            _broker = new EconomicReleaseBroker("Source2", new[] { _source1.Object, _source2.Object });
        }

        [Test]
        public void RequestDirectedToDefaultDataSourceIfNotSpecified()
        {
            var req = new EconomicReleaseRequest(DateTime.Now, DataLocation.ExternalOnly, dataSource: null);
            var res = _broker.RequestEconomicReleases(req).Result;

            _source2.Verify(x => x.RequestData(It.IsAny<DateTime>(), It.IsAny<DateTime>()));
        }

        [Test]
        public void RequestDirectedToSpecifiedDatasource()
        {
            var req = new EconomicReleaseRequest(DateTime.Now, DataLocation.ExternalOnly, dataSource: _source1.Object.Name);
            var res = _broker.RequestEconomicReleases(req).Result;

            _source1.Verify(x => x.RequestData(It.IsAny<DateTime>(), It.IsAny<DateTime>()));
        }

        [Test]
        public void ExternalResultsReturnedCorrectly()
        {
            var data = new List<EconomicRelease>
            {
                new EconomicRelease("1", "US", "USD", DateTime.Now, Importance.Low, null, null, null),
            };

            _source2
                .Setup(x => x.RequestData(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(data);

            var req = new EconomicReleaseRequest(DateTime.Now, DataLocation.ExternalOnly);
            var res = _broker.RequestEconomicReleases(req).Result;

            CollectionAssert.AreEquivalent(data, res);
        }

        [Test]
        public void RequestFilterIsApplied()
        {
            var data = new List<EconomicRelease>
            {
                new EconomicRelease("1", "US", "USD", DateTime.Now, Importance.Low, null, null, null),
                new EconomicRelease("2", "UK", "GBP", DateTime.Now, Importance.High, null, null, null),
                new EconomicRelease("3", "CN", "RMB", DateTime.Now, Importance.High, null, null, null),
                new EconomicRelease("4", "EU", "EUR", DateTime.Now, Importance.None, null, null, null),
                new EconomicRelease("5", "US", "USD", DateTime.Now, Importance.Mid, null, null, null),
                new EconomicRelease("6", "US", "USD", DateTime.Now, Importance.High, null, null, null),
            };

            _source2
                .Setup(x => x.RequestData(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(data);

            var req = new EconomicReleaseRequest(DateTime.Now, DataLocation.ExternalOnly, x => x.Importance >= Importance.Mid && x.Currency == "USD");
            var res = _broker.RequestEconomicReleases(req).Result;

            CollectionAssert.AreEquivalent(data.AsQueryable().Where(req.Filter).ToList(), res);
        }
    }
}