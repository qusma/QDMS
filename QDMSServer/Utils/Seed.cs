// -----------------------------------------------------------------------
// <copyright file="Seed.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity.Migrations;
using EntityData;
using QDMS;

namespace QDMSServer
{
    public static class Seed
    {
        public static void DoSeed()
        {
            var context = new MyDBContext();

            #region datasources
            var ib = new Datasource { Name = "Interactive Brokers" };
            var yahoo = new Datasource { Name = "Yahoo" };
            var quandl = new Datasource { Name = "Quandl" };

            context.Datasources.AddOrUpdate(x => x.Name, ib, yahoo, quandl);
            #endregion

            #region underlyingSymbols
            var eur = new UnderlyingSymbol
            {
                Symbol = "6E",
                Rule = new ExpirationRule
                {
                    DaysBefore = 2,
                    DayType = DayType.Business,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceUsesDays = false,
                    ReferenceWeekDay = DayOfTheWeek.Wednesday,
                    ReferenceWeekDayCount = WeekDayCount.Third
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, eur);

            var cl = new UnderlyingSymbol
            {
                Symbol = "CL",
                Rule = new ExpirationRule
                {
                    DaysBefore = 3,
                    DayType = DayType.Business,
                    ReferenceRelativeMonth = RelativeMonth.PreviousMonth,
                    ReferenceUsesDays = true,
                    ReferenceDays = 25,
                    ReferenceDayMustBeBusinessDay = true
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, cl);

            var zw = new UnderlyingSymbol
            {
                Symbol = "ZW",
                Rule = new ExpirationRule
                {
                    DaysBefore = 1,
                    DayType = DayType.Business,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceUsesDays = true,
                    ReferenceDays = 15
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, zw);

            var es = new UnderlyingSymbol
            {
                Symbol = "ES",
                Rule = new ExpirationRule
                {
                    DaysBefore = 0,
                    DayType = DayType.Calendar,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceUsesDays = false,
                    ReferenceWeekDay = DayOfTheWeek.Friday,
                    ReferenceWeekDayCount = WeekDayCount.Third
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, es);


            var nq = new UnderlyingSymbol
            {
                Symbol = "NQ",
                Rule = new ExpirationRule
                {
                    DaysBefore = 0,
                    DayType = DayType.Calendar,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceUsesDays = false,
                    ReferenceWeekDay = DayOfTheWeek.Friday,
                    ReferenceWeekDayCount = WeekDayCount.Third
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, nq);

            var gc = new UnderlyingSymbol
            {
                Symbol = "GC",
                Rule = new ExpirationRule
                {
                    DaysBefore = 2,
                    DayType = DayType.Business,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceUsesDays = false,
                    ReferenceDayIsLastBusinessDayOfMonth = true
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, gc);

            var ub = new UnderlyingSymbol
            {
                Symbol = "UB",
                Rule = new ExpirationRule
                {
                    DaysBefore = 7,
                    DayType = DayType.Business,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceUsesDays = false,
                    ReferenceDayIsLastBusinessDayOfMonth = true
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, ub);

            var vix = new UnderlyingSymbol
            {
                Symbol = "VIX",
                Rule = new ExpirationRule
                {
                    DaysBefore = 30,
                    DayType = DayType.Calendar,
                    ReferenceRelativeMonth = RelativeMonth.NextMonth,
                    ReferenceUsesDays = false,
                    ReferenceWeekDay = DayOfTheWeek.Friday,
                    ReferenceWeekDayCount = WeekDayCount.Third,
                    ReferenceDayMustBeBusinessDay = true
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, vix);
            #endregion

            context.SaveChanges();

            context.Dispose();
        }
    }
}
