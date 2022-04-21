// -----------------------------------------------------------------------
// <copyright file="MyUtilsTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using QDMS;
using QLNet;
using Instrument = QDMS.Instrument;

namespace QDMSTest
{
    [TestFixture]
    public class MyUtilsTest
    {
        [Test]
        public void OrdinalReturnsCorrectSuffix()
        {
            Dictionary<int, string> inputAndExpectedValues = new Dictionary<int, string>
            {
                {1, "1st"},
                {2, "2nd"},
                {3, "3rd"},
                {4, "4th"},
                {5, "5th"},
                {6, "6th"},
                {7, "7th"},
                {8, "8th"},
                {9, "9th"},
                {10, "10th"},
                {11, "11th"},
                {12, "12th"},
                {13, "13th"},
                {14, "14th"},
                {21, "21st"},
                {22, "22nd"},
                {23, "23rd"}
            };

            foreach (var kvp in inputAndExpectedValues)
            {
                Assert.AreEqual(kvp.Value, MyUtils.Ordinal(kvp.Key));
            }
        }

        [Test]
        public void GetFuturesContractSymbolReturnsCorrectSymbol()
        {
            Assert.AreEqual("GCF2", MyUtils.GetFuturesContractSymbol("GC", 1, 2012));
        }

        [Test]
        public void GetFuturesContractSymbolThrowsExceptionWhenMonthOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MyUtils.GetFuturesContractSymbol("GC", 0, 2012));
        }

        [Test]
        public void ConvertToTimestampReturnsCorrectTimestamp()
        {
            Assert.AreEqual(1012907112, MyUtils.ConvertToTimestamp(new DateTime(2002, 2, 5, 11, 5, 12)));
        }

        [Test]
        public void TimestampToDateTimeReturnsCorrectDateTime()
        {
            Assert.AreEqual(new DateTime(2002, 2, 5, 11, 5, 12), MyUtils.TimestampToDateTime(1012907112));
        }

        [Test]
        public void ProtoBufSerializeAndDeserializeResultsInIdenticalObject()
        {
            var inst = new Instrument
            {
                ID = 5,
                Symbol = "test",
                UnderlyingSymbol = "123",
                Exchange = new Exchange
                {
                    ID = 1,
                    Name = "CBOE"
                },
                Type = InstrumentType.Stock
            };
            var ms = new MemoryStream();

            var instDeserialized = MyUtils.ProtoBufDeserialize<Instrument>(MyUtils.ProtoBufSerialize(inst, ms), ms);

            Assert.AreEqual(inst.ID, instDeserialized.ID);
            Assert.AreEqual(inst.Symbol, instDeserialized.Symbol);
            Assert.AreEqual(inst.UnderlyingSymbol, instDeserialized.UnderlyingSymbol);
            Assert.AreEqual(inst.Type, instDeserialized.Type);
            Assert.AreEqual(inst.Exchange.ID, instDeserialized.Exchange.ID);
            Assert.AreEqual(inst.Exchange.Name, instDeserialized.Exchange.Name);
        }

        [Test]
        public void BusinessDaysBetweenReturnsCorrectNumberOfDays()
        {
            Assert.AreEqual(250, MyUtils.BusinessDaysBetween(new DateTime(2002, 12, 15), new DateTime(2003, 12, 15), new UnitedStates()));
            Assert.AreEqual(18, MyUtils.BusinessDaysBetween(new DateTime(2003, 12, 15), new DateTime(2004, 1, 12), new UnitedStates()));
            Assert.AreEqual(0, MyUtils.BusinessDaysBetween(new DateTime(2003, 12, 15), new DateTime(2003, 12, 15), new UnitedStates()));
        }

        [Test]
        public void BusinessDaysBetweenThrowsExceptionWhenEndDateBeforeStartDate()
        {
            Assert.Throws<Exception>(() => MyUtils.BusinessDaysBetween(new DateTime(2000, 1, 1), new DateTime(1999, 1, 1), new UnitedStates()));
        }
    }
}