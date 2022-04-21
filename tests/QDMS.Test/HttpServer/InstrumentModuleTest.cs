// -----------------------------------------------------------------------
// <copyright file="InstrumentModuleTest.cs" company="">
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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace QDMSTest.HttpServer
{
    [TestFixture]
    public class InstrumentModuleTest : ModuleTestBase<Instrument, InstrumentModule>
    {
        private List<Instrument> _data;

        [SetUp]
        public void SetUp()
        {
            _data = new List<Instrument>
            {
                new Instrument { ID = 1, Symbol = "SPY" },
                new Instrument { ID = 2, Symbol = "QQQ" }
            };
            base.SetUp(_data);
        }

        [Test]
        public async Task InstrumentSearchWorks()
        {
            //Set up the repo
            InstrumentRepoMock
                .Setup(x => x.FindInstruments(It.IsAny<Instrument>()))
                .Returns<Instrument>(async inst => await Task.FromResult(_data.AsQueryable().Where(x => x.Symbol == inst.Symbol).ToList()));

            //make the request
            var response = await Browser.Get("/instruments/search", with =>
            {
                with.HttpRequest();
                with.Query("Symbol", "QQQ"); //must use with.Query() for parameters
            });

            //make sure everything is nominal
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            string s = response.Body.AsString();
            var instruments = JsonConvert.DeserializeObject<List<Instrument>>(s);
            Assert.AreEqual(1, instruments.Count);
            Assert.AreEqual("QQQ", instruments[0].Symbol);
        }

        [Test]
        public async Task StoredDataInfoReqWorks()
        {
            _dataStorageMock.Setup(x => x.GetStorageInfo(It.Is<int>(id => id == 1)))
                .Returns<int>(x => 
                    new List<StoredDataInfo>() { new StoredDataInfo() { InstrumentID = x, Frequency = BarSize.OneDay, EarliestDate = new DateTime(2000,1,1), LatestDate = DateTime.Now } });

            //Set up the repo
            InstrumentRepoMock
                .Setup(x => x.FindInstruments(It.IsAny<Expression<Func<Instrument, bool>>>()))
                .Returns(async () => await Task.FromResult(_data.AsQueryable().Where(x => x.ID == 1).ToList()));

            //make the request
            var response = await Browser.Get("/instruments/1/storageinfo", with =>
            {
                with.HttpRequest();
                with.Query("Symbol", "QQQ"); //must use with.Query() for parameters
            });

            //make sure everything is nominal
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task PredicateSearchWorks()
        {
            //Set up the repo
            InstrumentRepoMock
                .Setup(x => x.FindInstruments(It.IsAny<Expression<Func<Instrument, bool>>>()))
                .Returns<Expression<Func<Instrument, bool>>>(f => Task.FromResult(_data.AsQueryable().Where(f).ToList()));

            //make the request
            Expression<Func<Instrument, bool>> filter = i => i.ID == 1 && i.Symbol == "SPY";
            var response = await Browser.Get("/instruments/predsearch", with =>
            {
                with.HttpRequest();
                with.Query("SerializedFilter", filter.Serialize()); //must use with.Query() for parameters
            });

            //make sure everything is nominal
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            string s = response.Body.AsString();
            var instruments = JsonConvert.DeserializeObject<List<Instrument>>(s);
            Assert.AreEqual(1, instruments.Count);
            Assert.AreEqual("SPY", instruments[0].Symbol);
        }
    }
}