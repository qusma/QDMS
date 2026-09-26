// -----------------------------------------------------------------------
// <copyright file="InstrumentEndpointsTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Moq;
using NUnit.Framework;
using QDMS;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace QDMSTest.HttpServer
{
    [TestFixture]
    public class InstrumentEndpointsTest : ApiTestBase
    {
        private Instrument _spy;
        private Instrument _qqq;
        private Datasource _datasource;

        [SetUp]
        public async Task SetUp()
        {
            _datasource = await AddDatasource();
            _spy = new Instrument { Symbol = "SPY", Name = "SPY", DatasourceID = _datasource.ID, Type = InstrumentType.Stock };
            _qqq = new Instrument { Symbol = "QQQ", Name = "QQQ", DatasourceID = _datasource.ID, Type = InstrumentType.Stock };

            using var ctx = CreateContext();
            ctx.Instruments.AddRange(_spy, _qqq);
            await ctx.SaveChangesAsync();
        }

        [Test]
        public async Task GetReturnsAllInstruments()
        {
            var response = await Client.GetAsync("/instruments");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var instruments = await Read<List<Instrument>>(response);
            Assert.That(instruments, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task InstrumentSearchWorks()
        {
            var response = await Client.GetAsync("/instruments/search?Symbol=QQQ");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var instruments = await Read<List<Instrument>>(response);
            Assert.That(instruments, Has.Count.EqualTo(1));
            Assert.That(instruments[0].Symbol, Is.EqualTo("QQQ"));
        }

        [Test]
        public async Task StoredDataInfoReqWorks()
        {
            DataStorageMock.Setup(x => x.GetStorageInfo(_spy.ID.Value))
                .Returns<int>(id => new List<StoredDataInfo>
                {
                    new StoredDataInfo { InstrumentID = id, Frequency = BarSize.OneDay, EarliestDate = new DateTime(2000, 1, 1), LatestDate = DateTime.Now }
                });

            var response = await Client.GetAsync($"/instruments/{_spy.ID}/storageinfo");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var info = await Read<List<StoredDataInfo>>(response);
            Assert.That(info, Has.Count.EqualTo(1));
            Assert.That(info[0].InstrumentID, Is.EqualTo(_spy.ID));
        }

        [Test]
        public async Task PredicateSearchWorks()
        {
            //MetaLinq can't serialize captured variables, so only literals here (same as for real clients)
            Expression<Func<Instrument, bool>> filter = i => i.Symbol == "SPY" && i.Type == InstrumentType.Stock;

            var response = await Client.GetAsync("/instruments/predsearch?SerializedFilter=" + Uri.EscapeDataString(new PredicateSearchRequest(filter).SerializedFilter));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var instruments = await Read<List<Instrument>>(response);
            Assert.That(instruments, Has.Count.EqualTo(1));
            Assert.That(instruments[0].Symbol, Is.EqualTo("SPY"));
        }

        [Test]
        public async Task PostDuplicateInstrumentReturns409Conflict()
        {
            //NULLs are distinct in the unique index (as in the old MySQL schema), so only fully specified instruments can clash
            Exchange exchange;
            using (var ctx = CreateContext())
            {
                exchange = new Exchange { Name = "CME", LongName = "CME", Timezone = "Central Standard Time" };
                ctx.Exchanges.Add(exchange);
                await ctx.SaveChangesAsync();
            }

            Instrument NewFuture() => new Instrument
            {
                Symbol = "ESZ6",
                Name = "ESZ6",
                Type = InstrumentType.Future,
                Datasource = _datasource,
                DatasourceID = _datasource.ID,
                ExchangeID = exchange.ID,
                Expiration = new DateTime(2026, 12, 18),
                Strike = 0,
                Multiplier = 50
            };

            var first = await Client.PostAsync("/instruments", JsonBody(NewFuture()));
            var second = await Client.PostAsync("/instruments", JsonBody(NewFuture()));

            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [Test]
        public async Task DeleteRemovesInstrumentAndItsData()
        {
            var response = await Client.DeleteAsync($"/instruments/{_qqq.ID}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            DataStorageMock.Verify(x => x.DeleteAllInstrumentData(It.Is<Instrument>(i => i.ID == _qqq.ID)));
            var remaining = await Read<List<Instrument>>(await Client.GetAsync("/instruments"));
            Assert.That(remaining, Has.Count.EqualTo(1));
        }
    }
}
