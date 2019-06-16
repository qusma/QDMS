// -----------------------------------------------------------------------
// <copyright file="SessionTemplateModuleTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Moq;
using Nancy;
using Nancy.Testing;
using NUnit.Framework;
using QDMS;
using QDMS.Server.NancyModules;
using QDMSTest.HttpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QDMSTest.HttpServer
{
    [TestFixture]
    public class SessionTemplateModuleTest : ModuleTestBase<SessionTemplate, SessionTemplateModule>
    {
        private List<SessionTemplate> _data;

        [SetUp]
        public void SetUp()
        {
            var sessions = new List<TemplateSession>
            {
                new TemplateSession { ID = 1, OpeningDay = DayOfTheWeek.Monday, OpeningTime = new TimeSpan(9, 0, 0), ClosingTime = new TimeSpan(15, 0, 0)},
                new TemplateSession { ID = 2, OpeningDay = DayOfTheWeek.Tuesday, OpeningTime = new TimeSpan(9, 0, 0), ClosingTime = new TimeSpan(15, 0, 0)}
            };

            _data = new List<SessionTemplate>
            {
                new SessionTemplate { ID = 1, Name = "TemplateOne", Sessions = sessions }
            };
            base.SetUp(_data);
        }

        [Test]
        public async Task InstrumentsWithTemplateAsSessionSourceHaveTheirSessionsUpdatedWhenTemplateIsUpdated()
        {
            //needed to simulate the update of the session
            ContextMock
                .Setup(x => x.UpdateEntryValues(It.IsAny<TemplateSession>(), It.IsAny<TemplateSession>()))
                .Callback<object, object>((oldSession, newSession) =>
                    ((TemplateSession)oldSession).OpeningTime = ((TemplateSession)newSession).OpeningTime);

            //set up DbSet mocks
            var instrumentSessions = new List<InstrumentSession>
            {
                new InstrumentSession { OpeningTime = new TimeSpan(9, 0, 0) }
            };
            var instrument = new Instrument { SessionsSource = SessionsSource.Template, SessionTemplateID = 1, Sessions = instrumentSessions };
            var instrumentData = new List<Instrument> { instrument };
            SetUpDbSet(instrumentData);

            SetUpDbSet(new List<TemplateSession>());

            SetUpDbSet(new List<InstrumentSession>());

            //Update session
            var template = (SessionTemplate)_data[0].Clone(); //have to clone it, because the original is the one in the mocked context
            template.Sessions.First().OpeningTime = new TimeSpan(10, 0, 0);

            var response = await Browser.Put("/sessiontemplates", with =>
            {
                with.HttpRequest();
                with.JsonBody(template);
            });

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            //Now the instrument should have the updated session times
            Assert.AreEqual(new TimeSpan(10, 0, 0), instrument.Sessions.First().OpeningTime);
        }

        [Test]
        public async Task DeleteOnTemplateThatIsReferencedByAnInstrumentReturns409Conflict()
        {
            var instrument = new Instrument { SessionsSource = SessionsSource.Template, SessionTemplateID = 1 };
            SetUpDbSet(new List<Instrument> { instrument });

            var response = await Browser.Delete("/sessiontemplates/1", BrowserCtx);

            Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        }
    }
}