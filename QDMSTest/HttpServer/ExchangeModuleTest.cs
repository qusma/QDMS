// -----------------------------------------------------------------------
// <copyright file="ExchangeModuleTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Moq;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using NUnit.Framework;
using QDMS;
using QDMS.Server.NancyModules;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace QDMSTest.HttpServer
{
    [TestFixture]
    public class ExchangeModuleTest : ModuleTestBase<Exchange, ExchangeModule>
    {
        private List<Exchange> _data;

        [SetUp]
        public void SetUp()
        {
            var sessions = new List<ExchangeSession>
            {
                new ExchangeSession { ID = 1, OpeningDay = DayOfTheWeek.Monday, OpeningTime = new TimeSpan(9, 0, 0), ClosingTime = new TimeSpan(15, 0, 0)},
                new ExchangeSession { ID = 2, OpeningDay = DayOfTheWeek.Tuesday, OpeningTime = new TimeSpan(9, 0, 0), ClosingTime = new TimeSpan(15, 0, 0)}
            };

            _data = new List<Exchange>
            {
                new Exchange { ID = 1, Name = "First", LongName = "First", Timezone = "Eastern Standard Time", Sessions = sessions },
                new Exchange { ID = 2, Name = "Second" }
            };
            base.SetUp(_data);
        }

        [Test]
        public async Task GetReturnsAllExchanges()
        {
            var response = await Browser.Get("/exchanges", BrowserCtx);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            string s = response.Body.AsString();
            var exchanges = JsonConvert.DeserializeObject<List<Exchange>>(s);
            for (int i = 0; i < exchanges.Count; i++)
            {
                Assert.AreEqual(_data[i].ToString(), exchanges[i].ToString());
            }
        }

        [Test]
        public async Task GetWithIdReturnsSpecificExchange()
        {
            var response = await Browser.Get("/exchanges/1", BrowserCtx);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            string s = response.Body.AsString();
            var exchange = JsonConvert.DeserializeObject<Exchange>(s);
            Assert.AreEqual(1, exchange.ID);
            Assert.AreEqual("First", exchange.Name);
        }

        [Test]
        public async Task GetWithIdReturns404WhenDoesNotExist()
        {
            var response = await Browser.Get("/exchanges/5", BrowserCtx);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Test]
        public async Task PostInvalidExchangeReturns400BadRequest()
        {
            var exchange = new Exchange { Name = "" };
            var response = await Browser.Post("/exchanges", with =>
            {
                with.HttpRequest();
                with.JsonBody(exchange);
            });

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Test]
        public async Task PutInvalidExchangeReturns400BadRequest()
        {
            var exchange = new Exchange { Name = "" };
            var response = await Browser.Put("/exchanges", with =>
            {
                with.HttpRequest();
                with.JsonBody(exchange);
            });

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Test]
        public async Task PutNonExistentExchangeReturns404Notfound()
        {
            var exchange = new Exchange { ID = 5, Name = "Name", LongName = "LongName", Timezone = "Eastern Standard Time" };
            var response = await Browser.Put("/exchanges", with =>
            {
                with.HttpRequest();
                with.JsonBody(exchange);
            });

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Test]
        public async Task SessionsCollectionIsUpdatedCorrectly()
        {
            var newOpenTime = new TimeSpan(10, 0, 0);

            //set up DbSet mocks
            SetUpDbSet(new List<Instrument>());

            SetUpDbSet(new List<ExchangeSession>());

            //create exchange to be sent
            var exchange = (Exchange)_data[0].Clone(); //have to clone it, because the original is the one in the mocked context
            //remove one
            exchange.Sessions.Remove(exchange.Sessions.First());
            //change one
            exchange.Sessions.First().OpeningTime = newOpenTime;
            //add one
            exchange.Sessions.Add(new ExchangeSession { OpeningDay = DayOfTheWeek.Wednesday, OpeningTime = new TimeSpan(11, 0, 0), ClosingTime = new TimeSpan(15, 0, 0) });

            var response = await Browser.Put("/exchanges", with =>
            {
                with.HttpRequest();
                with.JsonBody(exchange);
            });

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            //verify update
            ContextMock.Verify(x => x.UpdateEntryValues(
                It.Is<ExchangeSession>(y => y.ID == 2),
                It.Is<ExchangeSession>(y => y.OpeningTime == newOpenTime)));

            //verify delete
            ContextMock.Verify(x => x.SetEntryState(
                It.Is<ExchangeSession>(y => y.ID == 1),
                It.Is<EntityState>(y => y == EntityState.Deleted)));

            //verify addition
            Assert.IsTrue(_data[0].Sessions.Any(x => x.OpeningDay == DayOfTheWeek.Wednesday && x.OpeningTime == new TimeSpan(11, 0, 0)));
        }

        [Test]
        public async Task InstrumentsWithExchangeAsSessionSourceHaveTheirSessionsUpdatedWhenExchangeIsUpdated()
        {
            //needed to simulate the update of the session
            ContextMock
                .Setup(x => x.UpdateEntryValues(It.IsAny<ExchangeSession>(), It.IsAny<ExchangeSession>()))
                .Callback<object, object>((oldSession, newSession) =>
                    ((ExchangeSession)oldSession).OpeningTime = ((ExchangeSession)newSession).OpeningTime);

            //set up DbSet mocks
            var instrumentSessions = new List<InstrumentSession>
            {
                new InstrumentSession { OpeningTime = new TimeSpan(9, 0, 0) }
            };
            var instrument = new Instrument { SessionsSource = SessionsSource.Exchange, ExchangeID = 1, Sessions = instrumentSessions };
            var instrumentData = new List<Instrument> { instrument };
            SetUpDbSet(instrumentData);

            SetUpDbSet(new List<ExchangeSession>());

            SetUpDbSet(new List<InstrumentSession>());

            //Update session
            var exchange = (Exchange)_data[0].Clone(); //have to clone it, because the original is the one in the mocked context
            exchange.Sessions.First().OpeningTime = new TimeSpan(10, 0, 0);

            var response = await Browser.Put("/exchanges", with =>
            {
                with.HttpRequest();
                with.JsonBody(exchange);
            });

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            //Now the instrument should have the updated session times
            Assert.AreEqual(new TimeSpan(10, 0, 0), instrument.Sessions.First().OpeningTime);
        }

        [Test]
        public async Task DeleteReturns404WhenDoesNotExist()
        {
            var response = await Browser.Delete("/exchanges/5", BrowserCtx);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Test]
        public async Task DeleteCallsRemoveOnDbSet()
        {
            SetUpDbSet(new List<Instrument>());

            var response = await Browser.Delete("/exchanges/1", BrowserCtx);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            DbSetMock.Verify(x => x.Remove(It.Is<Exchange>(e => e.ID == 1)));
        }

        [Test]
        public async Task DeleteOnExchangeThatIsReferencedByAnInstrumentReturns409Conflict()
        {
            var instrument = new Instrument { ExchangeID = 1 };
            SetUpDbSet(new List<Instrument> { instrument });

            var response = await Browser.Delete("/exchanges/1", BrowserCtx);

            Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        }
    }
}