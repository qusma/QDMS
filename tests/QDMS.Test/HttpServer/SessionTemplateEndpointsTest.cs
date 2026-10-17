// -----------------------------------------------------------------------
// <copyright file="SessionTemplateEndpointsTest.cs" company="">
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
    public class SessionTemplateEndpointsTest : ApiTestBase
    {
        private SessionTemplate _template;

        [SetUp]
        public async Task SetUp()
        {
            var sessions = new List<TemplateSession>
            {
                new TemplateSession { OpeningDay = DayOfTheWeek.Monday, OpeningTime = new TimeSpan(9, 0, 0), ClosingDay = DayOfTheWeek.Monday, ClosingTime = new TimeSpan(15, 0, 0)},
                new TemplateSession { OpeningDay = DayOfTheWeek.Tuesday, OpeningTime = new TimeSpan(9, 0, 0), ClosingDay = DayOfTheWeek.Tuesday, ClosingTime = new TimeSpan(15, 0, 0)}
            };
            _template = new SessionTemplate { Name = "TemplateOne", Sessions = sessions };

            using var ctx = CreateContext();
            ctx.SessionTemplates.Add(_template);
            await ctx.SaveChangesAsync();
        }

        private async Task<int> AddInstrumentUsingTemplate()
        {
            var ds = await AddDatasource();
            using var ctx = CreateContext();
            var instrument = new Instrument
            {
                Symbol = "SPY",
                DatasourceID = ds.ID,
                SessionsSource = SessionsSource.Template,
                SessionTemplateID = _template.ID,
                Sessions = new List<InstrumentSession> { new InstrumentSession { OpeningTime = new TimeSpan(9, 0, 0) } }
            };
            ctx.Instruments.Add(instrument);
            await ctx.SaveChangesAsync();
            return instrument.ID.Value;
        }

        [Test]
        public async Task InstrumentsWithTemplateAsSessionSourceHaveTheirSessionsUpdatedWhenTemplateIsUpdated()
        {
            int instrumentId = await AddInstrumentUsingTemplate();

            SessionTemplate template;
            using (var ctx = CreateContext())
            {
                template = await ctx.SessionTemplates.AsNoTracking().Include(x => x.Sessions).SingleAsync(x => x.ID == _template.ID);
            }
            foreach (var session in template.Sessions)
            {
                session.OpeningTime = new TimeSpan(10, 0, 0);
            }

            var response = await Client.PutAsync("/sessiontemplates", JsonBody(template));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using (var ctx = CreateContext())
            {
                var sessions = await ctx.InstrumentSessions.Where(x => x.InstrumentID == instrumentId).ToListAsync();
                Assert.That(sessions, Has.Count.EqualTo(2));
                Assert.That(sessions.All(x => x.OpeningTime == new TimeSpan(10, 0, 0)), Is.True);
            }
        }

        [Test]
        public async Task DeleteOnTemplateThatIsReferencedByAnInstrumentReturns409Conflict()
        {
            await AddInstrumentUsingTemplate();

            var response = await Client.DeleteAsync($"/sessiontemplates/{_template.ID}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [Test]
        public async Task DeleteUnreferencedTemplateWorks()
        {
            var response = await Client.DeleteAsync($"/sessiontemplates/{_template.ID}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var ctx = CreateContext();
            Assert.That(await ctx.SessionTemplates.AnyAsync(), Is.False);
        }
    }
}
