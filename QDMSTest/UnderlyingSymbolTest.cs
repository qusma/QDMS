// -----------------------------------------------------------------------
// <copyright file="UnderlyingSymbolTest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using NUnit.Framework;
using QDMS;

namespace QDMSTest
{
    [TestFixture]
    public class UnderlyingSymbolTest
    {
        [Test]
        public void CorrectlyCalculatesVIXFuturesExpirationDates()
        {
            //The Wednesday that is thirty days prior to the third Friday of the calendar month 
            //immediately following the month in which the contract expires ("Final Settlement Date"). 
            //If the third Friday of the month subsequent to expiration of the applicable VIX futures 
            //contract is a CBOE holiday, the Final Settlement Date for the contract shall be thirty days 
            //prior to the CBOE business day immediately preceding that Friday.

            //todo for this to stop failing:
            //move back by a day if the reference day is a holiday? probably not appropriate for all markets
            var vix = new UnderlyingSymbol();
            vix.Rule = new ExpirationRule
            {
                DaysBefore = 30,
                DayType = DayType.Calendar,
                ReferenceRelativeMonth = RelativeMonth.NextMonth,
                ReferenceUsesDays = false,
                ReferenceWeekDay = DayOfTheWeek.Friday,
                ReferenceWeekDayCount = WeekDayCount.Third
            };

            DateTime dec13Expiration = vix.ExpirationDate(2013, 12);
            Assert.AreEqual(new DateTime(2013, 12, 17), dec13Expiration);

            DateTime jan14Expiration = vix.ExpirationDate(2014, 1);
            Assert.AreEqual(new DateTime(2014, 1, 22), jan14Expiration);

            DateTime feb14Expiration = vix.ExpirationDate(2014, 2);
            Assert.AreEqual(new DateTime(2014, 2, 19), feb14Expiration);

            DateTime mar14Expiration = vix.ExpirationDate(2014, 3);
            Assert.AreEqual(new DateTime(2014, 3, 18), mar14Expiration);

            DateTime apr14Expiration = vix.ExpirationDate(2014, 4);
            Assert.AreEqual(new DateTime(2014, 4, 16), apr14Expiration);
        }

        [Test]
        public void CorrectlyCalculatesESFuturesExpirationDates()
        {
            //Trading can occur up to 8:30 a.m. on the 3rd Friday of the contract month
            var es = new UnderlyingSymbol();
            es.Rule = new ExpirationRule
            {
                DaysBefore = 0,
                DayType = DayType.Calendar,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = false,
                ReferenceWeekDay = DayOfTheWeek.Friday,
                ReferenceWeekDayCount = WeekDayCount.Third
            };

            DateTime dec13Expiration = es.ExpirationDate(2013, 12);
            Assert.AreEqual(new DateTime(2013, 12, 20), dec13Expiration);

            DateTime mar14Expiration = es.ExpirationDate(2014, 3);
            Assert.AreEqual(new DateTime(2014, 3, 21), mar14Expiration);

            DateTime jun14Expiration = es.ExpirationDate(2014, 6);
            Assert.AreEqual(new DateTime(2014, 6, 20), jun14Expiration);

            DateTime sep14Expiration = es.ExpirationDate(2014, 9);
            Assert.AreEqual(new DateTime(2014, 9, 19), sep14Expiration);

            DateTime dec14Expiration = es.ExpirationDate(2014, 12);
            Assert.AreEqual(new DateTime(2014, 12, 19), dec14Expiration);
        }

        [Test]
        public void CorrectlyCalculatesCMEWheatFuturesExpirationDates()
        {
            //The business day prior to the 15th calendar day of the contract month.
            var zw = new UnderlyingSymbol();
            zw.Rule = new ExpirationRule
            {
                DaysBefore = 1,
                DayType = DayType.Business,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = true,
                ReferenceDays = 15
            };

            DateTime dec13Expiration = zw.ExpirationDate(2013, 12);
            Assert.AreEqual(new DateTime(2013, 12, 13), dec13Expiration);

            DateTime mar14Expiration = zw.ExpirationDate(2014, 3);
            Assert.AreEqual(new DateTime(2014, 3, 14), mar14Expiration);

            DateTime may14Expiration = zw.ExpirationDate(2014, 5);
            Assert.AreEqual(new DateTime(2014, 5, 14), may14Expiration);

            DateTime jul14Expiration = zw.ExpirationDate(2014, 7);
            Assert.AreEqual(new DateTime(2014, 7, 14), jul14Expiration);

            DateTime sep14Expiration = zw.ExpirationDate(2014, 9);
            Assert.AreEqual(new DateTime(2014, 9, 12), sep14Expiration);

            DateTime dec14Expiration = zw.ExpirationDate(2014, 12);
            Assert.AreEqual(new DateTime(2014, 12, 12), dec14Expiration);
        }

        [Test]
        public void CorrectlyCalculatesCLFuturesExpirationDates()
        {
            //Trading in the current delivery month shall cease on the third business day prior to the 
            //twenty-fifth calendar day of the month preceding the delivery month. If the twenty-fifth 
            //calendar day of the month is a non-business day, trading shall cease on the third business 
            //day prior to the last business day preceding the twenty-fifth calendar day.
            var cl = new UnderlyingSymbol();
            cl.Rule = new ExpirationRule
            {
                DaysBefore = 3,
                DayType = DayType.Business,
                ReferenceRelativeMonth = RelativeMonth.PreviousMonth,
                ReferenceUsesDays = true,
                ReferenceDays = 25
            };

            DateTime jan14Expiration = cl.ExpirationDate(2014, 1);
            Assert.AreEqual(new DateTime(2013, 12, 19), jan14Expiration);

            DateTime feb14Expiration = cl.ExpirationDate(2014, 2);
            Assert.AreEqual(new DateTime(2014, 1, 20), feb14Expiration);

            DateTime mar14Expiration = cl.ExpirationDate(2014, 3);
            Assert.AreEqual(new DateTime(2014, 2, 20), mar14Expiration);

            DateTime apr14Expiration = cl.ExpirationDate(2014, 4);
            Assert.AreEqual(new DateTime(2014, 3, 20), apr14Expiration);

            DateTime may14Expiration = cl.ExpirationDate(2014, 5);
            Assert.AreEqual(new DateTime(2014, 4, 22), may14Expiration);
        }

        [Test]
        public void CorrectlyCalculatesUltraTBondFuturesExpirationDates()
        {
            //Seventh business day preceding the last business day of the delivery month. 
            //Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
            var ub = new UnderlyingSymbol();
            ub.Rule = new ExpirationRule
            {
                DaysBefore = 7,
                DayType = DayType.Business,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = false,
                ReferenceDayIsLastBusinessDayOfMonth = true
            };

            DateTime dec13Expiration = ub.ExpirationDate(2013, 12);
            Assert.AreEqual(new DateTime(2013, 12, 19), dec13Expiration);

            DateTime mar14Expiration = ub.ExpirationDate(2014, 3);
            Assert.AreEqual(new DateTime(2014, 3, 20), mar14Expiration);

            DateTime jun14Expiration = ub.ExpirationDate(2014, 6);
            Assert.AreEqual(new DateTime(2014, 6, 19), jun14Expiration);

            DateTime sep14Expiration = ub.ExpirationDate(2014, 9);
            Assert.AreEqual(new DateTime(2014, 9, 19), sep14Expiration);
        }

        [Test]
        public void CorrectlyCalculatesGCFuturesExpirationDates()
        {
            //Trading terminates on the third last business day of the delivery month.
            var ub = new UnderlyingSymbol();
            ub.Rule = new ExpirationRule
            {
                DaysBefore = 2,
                DayType = DayType.Business,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = false,
                ReferenceDayIsLastBusinessDayOfMonth = true
            };

            DateTime dec13Expiration = ub.ExpirationDate(2013, 12);
            Assert.AreEqual(new DateTime(2013, 12, 27), dec13Expiration);

            DateTime jan14Expiration = ub.ExpirationDate(2014, 1);
            Assert.AreEqual(new DateTime(2014, 1, 29), jan14Expiration);

            DateTime feb14Expiration = ub.ExpirationDate(2014, 2);
            Assert.AreEqual(new DateTime(2014, 2, 26), feb14Expiration);

            DateTime mar14Expiration = ub.ExpirationDate(2014, 3);
            Assert.AreEqual(new DateTime(2014, 3, 27), mar14Expiration);

            DateTime apr14Expiration = ub.ExpirationDate(2014, 4);
            Assert.AreEqual(new DateTime(2014, 4, 28), apr14Expiration);
        }

        [Test]
        public void CorrectlyCalculates6EFuturesExpirationDates()
        {
            //9:16 a.m. Central Time (CT) on the second business day immediately preceding 
            //the third Wednesday of the contract month (usually Monday).
            var eur = new UnderlyingSymbol();
            eur.Rule = new ExpirationRule
            {
                DaysBefore = 2,
                DayType = DayType.Business,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = false,
                ReferenceWeekDay = DayOfTheWeek.Wednesday,
                ReferenceWeekDayCount = WeekDayCount.Third
            };

            DateTime dec13Expiration = eur.ExpirationDate(2013, 12);
            Assert.AreEqual(new DateTime(2013, 12, 16), dec13Expiration);

            DateTime mar14Expiration = eur.ExpirationDate(2014, 3);
            Assert.AreEqual(new DateTime(2014, 3, 17), mar14Expiration);

            DateTime jun14Expiration = eur.ExpirationDate(2014, 6);
            Assert.AreEqual(new DateTime(2014, 6, 16), jun14Expiration);

            DateTime sep14Expiration = eur.ExpirationDate(2014, 9);
            Assert.AreEqual(new DateTime(2014, 9, 15), sep14Expiration);

            DateTime dec14Expiration = eur.ExpirationDate(2014, 12);
            Assert.AreEqual(new DateTime(2014, 12, 15), dec14Expiration);
        }




        //TODO add more contracts
        //for wheat, cl to work we need an option for a business day offset from the reference day
    }
}
