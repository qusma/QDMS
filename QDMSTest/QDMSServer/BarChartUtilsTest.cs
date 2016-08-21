// -----------------------------------------------------------------------
// <copyright file="BarChartUtilsTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QDMS;
using QDMS.Server.DataSources.BarChart;

namespace QDMSTest
{
    [TestFixture]
    public class BarChartUtilsTest
    {
        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        public void BarTimeAdjustmentsHappenCorrectly()
        {
            string json = @"{
	""status"": {
		""code"": 200,
		""message"": ""Success.""
	},
	""results"": [{
		""symbol"": ""IBM"",
		""timestamp"": ""2016-08-15T09:00:00-04:00"",
		""tradingDay"": ""2016-08-15"",
		""open"": 162.4,
		""high"": 162.97,
		""low"": 162.38,
		""close"": 162.77,
		""volume"": 215371
	},
	{
		""symbol"": ""IBM"",
		""timestamp"": ""2016-08-15T10:00:00-04:00"",
		""tradingDay"": ""2016-08-15"",
		""open"": 162.8,
		""high"": 162.95,
		""low"": 162.61,
		""close"": 162.63,
		""volume"": 222815
	}]
}";
            
            var exchange = new Exchange
            {
                Timezone = "Eastern Standard Time"
            };

            var instrument = new Instrument()
            {
                Symbol = "IBM",
                Exchange = exchange,
                Sessions = new List<InstrumentSession>{
                new InstrumentSession{ OpeningDay = DayOfTheWeek.Monday, OpeningTime = new TimeSpan(9, 30, 0) } }
            };

            var request = new HistoricalDataRequest(instrument, BarSize.OneHour, DateTime.Now, DateTime.Now);
            List<OHLCBar> bars = BarChartUtils.ParseJson(JObject.Parse(json), request);

            //opening time set to the session opening instead of earlier
            Assert.AreEqual(new DateTime(2016, 8, 15, 9, 30, 0), bars[0].DTOpen.Value);

            //Bar closing time set correctly
            Assert.AreEqual(new DateTime(2016, 8, 15, 10, 0, 0), bars[0].DT);


            //timezone conversion has happened properly
            Assert.AreEqual(new DateTime(2016, 8, 15, 10, 0, 0), bars[1].DTOpen.Value);
        }
    }
}
