// -----------------------------------------------------------------------
// <copyright file="InstrumentTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using NUnit.Framework;
using QDMS;

namespace QDMSTest
{
    [TestFixture]
    public class InstrumentTest
    {
        [Test]
        public void GetTZInfoReturnsUTCIfExchangeIsUndefined()
        {
            var inst = new Instrument();
            TimeZoneInfo tz = inst.GetTZInfo();
            Assert.AreEqual("UTC", tz.Id);
        }

        [Test]
        public void GetTZInfoReturnsUTCIfExchangeTimezoneIsEmptyString()
        {
            var inst = new Instrument { Exchange = new Exchange { Timezone = "" } };
            TimeZoneInfo tz = inst.GetTZInfo();
            Assert.AreEqual("UTC", tz.Id);
        }

        [Test]
        public void GetTZInfoThrowsExceptionIfNonExistingTimezoneIsSpecified()
        {
            var inst = new Instrument { Exchange = new Exchange { Timezone = "asdf____" } };
            Assert.Throws<TimeZoneNotFoundException>(() =>
            {
                TimeZoneInfo tz = inst.GetTZInfo();
            });
        }
    }
}