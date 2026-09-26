// -----------------------------------------------------------------------
// <copyright file="ExchangeEndpointsTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using QDMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace QDMSTest.HttpServer
{
    [TestFixture]
    public class ExchangeEndpointsTest : ApiTestBase
    {
        private Exchange _first;
        private Exchange _second;

        [SetUp]
        public async Task SetUp()
        {
            var sessions = new List<ExchangeSession>
            {
                new ExchangeSession { OpeningDay = DayOfTheWeek.Monday, OpeningTime = new TimeSpan(9, 0, 0), ClosingDay = DayOfTheWeek.Monday, ClosingTime = new TimeSpan(15, 0, 0)},
                new ExchangeSession { OpeningDay = DayOfTheWeek.Tuesday, OpeningTime = new TimeSpan(9, 0, 0), ClosingDay = DayOfTheWeek.Tuesday, ClosingTime = new TimeSpan(15, 0, 0)}
            };

            _first = new Exchange { Name = "First", LongName = "First", Timezone = "Eastern Standard Time", Sessions = sessions };
            _second = new Exchange { Name = "Second", LongName = "Second", Timezone = "Eastern Standard Time", Sessions = new List<ExchangeSession>() };

            using var ctx = CreateContext();
            ctx.Exchanges.AddRange(_first, _second);
            await ctx.SaveChangesAsync();
        }

        private async Task<Exchange> LoadFirst()
        {
            using var ctx = CreateContext();
            return await ctx.Exchanges.AsNoTracking().Include(x => x.Sessions).SingleAsync(x => x.ID == _first.ID);
        }

        [Test]
        public async Task GetReturnsAllExchanges()
        {
            var response = await Client.GetAsync("/exchanges");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var exchanges = await Read<List<Exchange>>(response);
            Assert.That(exchanges.Select(x => x.Name), Is.EquivalentTo(new[] { "First", "Second" }));
            Assert.That(exchanges.Single(x => x.Name == "First").Sessions, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetWithIdReturnsSpecificExchange()
        {
            var response = await Client.GetAsync($"/exchanges/{_first.ID}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var exchange = await Read<Exchange>(response);
            Assert.That(exchange.ID, Is.EqualTo(_first.ID));
            Assert.That(exchange.Name, Is.EqualTo("First"));
        }

        [Test]
        public async Task GetWithIdReturns404WhenDoesNotExist()
        {
            var response = await Client.GetAsync("/exchanges/999");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task PostCreatesExchangeAndReturnsItWithId()
        {
            var exchange = new Exchange { Name = "Third", LongName = "Third", Timezone = "UTC" };
            var response = await Client.PostAsync("/exchanges", JsonBody(exchange));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var created = await Read<Exchange>(response);
            Assert.That(created.ID, Is.GreaterThan(0));

            using var ctx = CreateContext();
            Assert.That(await ctx.Exchanges.AnyAsync(x => x.Name == "Third"), Is.True);
        }

        [Test]
        public async Task PostInvalidExchangeReturns400BadRequest()
        {
            var response = await Client.PostAsync("/exchanges", JsonBody(new Exchange { Name = "" }));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task PutInvalidExchangeReturns400BadRequest()
        {
            var response = await Client.PutAsync("/exchanges", JsonBody(new Exchange { Name = "" }));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task PutNonExistentExchangeReturns404Notfound()
        {
            var exchange = new Exchange { ID = 999, Name = "Name", LongName = "LongName", Timezone = "Eastern Standard Time" };
            var response = await Client.PutAsync("/exchanges", JsonBody(exchange));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task SessionsCollectionIsUpdatedCorrectly()
        {
            var newOpenTime = new TimeSpan(10, 0, 0);
            var exchange = await LoadFirst();
            var removed = exchange.Sessions.Single(x => x.OpeningDay == DayOfTheWeek.Monday);
            var changed = exchange.Sessions.Single(x => x.OpeningDay == DayOfTheWeek.Tuesday);

            exchange.Sessions.Remove(removed);
            changed.OpeningTime = newOpenTime;
            exchange.Sessions.Add(new ExchangeSession { OpeningDay = DayOfTheWeek.Wednesday, OpeningTime = new TimeSpan(11, 0, 0), ClosingDay = DayOfTheWeek.Wednesday, ClosingTime = new TimeSpan(15, 0, 0) });

            var response = await Client.PutAsync("/exchanges", JsonBody(exchange));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var updated = await LoadFirst();
            Assert.That(updated.Sessions, Has.Count.EqualTo(2));
            Assert.That(updated.Sessions.Any(x => x.ID == removed.ID), Is.False, "removed session still there");
            Assert.That(updated.Sessions.Single(x => x.ID == changed.ID).OpeningTime, Is.EqualTo(newOpenTime));
            Assert.That(updated.Sessions.Any(x => x.OpeningDay == DayOfTheWeek.Wednesday && x.OpeningTime == new TimeSpan(11, 0, 0)), Is.True);
        }

        [Test]
        public async Task InstrumentsWithExchangeAsSessionSourceHaveTheirSessionsUpdatedWhenExchangeIsUpdated()
        {
            var ds = await AddDatasource();
            int instrumentId;
            using (var ctx = CreateContext())
            {
                var instrument = new Instrument
                {
                    Symbol = "SPY",
                    DatasourceID = ds.ID,
                    SessionsSource = SessionsSource.Exchange,
                    ExchangeID = _first.ID,
                    Sessions = new List<InstrumentSession> { new InstrumentSession { OpeningTime = new TimeSpan(9, 0, 0) } }
                };
                ctx.Instruments.Add(instrument);
                await ctx.SaveChangesAsync();
                instrumentId = instrument.ID.Value;
            }

            var exchange = await LoadFirst();
            foreach (var session in exchange.Sessions)
            {
                session.OpeningTime = new TimeSpan(10, 0, 0);
            }

            var response = await Client.PutAsync("/exchanges", JsonBody(exchange));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using (var ctx = CreateContext())
            {
                var sessions = await ctx.InstrumentSessions.Where(x => x.InstrumentID == instrumentId).ToListAsync();
                Assert.That(sessions, Has.Count.EqualTo(2));
                Assert.That(sessions.All(x => x.OpeningTime == new TimeSpan(10, 0, 0)), Is.True);
            }
        }

        [Test]
        public async Task DeleteReturns404WhenDoesNotExist()
        {
            var response = await Client.DeleteAsync("/exchanges/999");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task DeleteRemovesExchangeAndItsSessions()
        {
            var response = await Client.DeleteAsync($"/exchanges/{_first.ID}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var ctx = CreateContext();
            Assert.That(await ctx.Exchanges.AnyAsync(x => x.ID == _first.ID), Is.False);
            Assert.That(await ctx.ExchangeSessions.AnyAsync(x => x.ExchangeID == _first.ID), Is.False);
        }

        [Test]
        public async Task DeleteOnExchangeThatIsReferencedByAnInstrumentReturns409Conflict()
        {
            var ds = await AddDatasource();
            using (var ctx = CreateContext())
            {
                ctx.Instruments.Add(new Instrument { Symbol = "SPY", DatasourceID = ds.ID, ExchangeID = _first.ID });
                await ctx.SaveChangesAsync();
            }

            var response = await Client.DeleteAsync($"/exchanges/{_first.ID}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [Test]
        public async Task RequestWithoutApiKeyReturns401()
        {
            Client.DefaultRequestHeaders.Remove("Authorization");

            var response = await Client.GetAsync("/exchanges");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
