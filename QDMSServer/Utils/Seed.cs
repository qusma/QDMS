// -----------------------------------------------------------------------
// <copyright file="Seed.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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

            #region sessiontemplates
            var sessiontemplates = new []
            {
                new SessionTemplate { Name = "U.S. Equities RTH" },
                new SessionTemplate { Name = "U.S. Equities (w/ Post)" },
                new SessionTemplate { Name = "U.S. Equities (w/ Pre)" },
                new SessionTemplate { Name = "U.S. Equities (w/ Pre & Post)" },
                new SessionTemplate { Name = "CME: Equity Index Futures (GLOBEX)" },
                new SessionTemplate { Name = "CME: Equity Index Futures (Open Outcry)" },
                new SessionTemplate { Name = "CME: Equity Index Futures [E-Mini] (GLOBEX)" },
                new SessionTemplate { Name = "CME: FX Futures (GLOBEX)" },
            };
            foreach (SessionTemplate s in sessiontemplates)
            {
                context.SessionTemplates.AddOrUpdate(x => x.Name, s);
            }


            #endregion

            #region templatesessions
            var templatesessions = new[]
            {
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities RTH")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities RTH")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities RTH")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities RTH")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Friday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities RTH")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Friday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Friday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre & Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre & Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre & Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre & Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Friday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre & Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(8, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(8, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(8, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(8, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(8, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Sunday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 30, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (Open Outcry)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 30, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (Open Outcry)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 30, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (Open Outcry)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 30, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (Open Outcry)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 30, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Friday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (Open Outcry)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Sunday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: FX Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: FX Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: FX Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "CME: FX Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Sunday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: FX Futures (GLOBEX)")
                }
            };

            foreach (TemplateSession t in templatesessions)
            {
                context.TemplateSessions.Add(t);
            }
            #endregion

            #region exchanges
            var exchanges = new[] 
                {
                    new Exchange { Name = "AB", LongName = "American Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "AEB", LongName = "Euronext Netherlands", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ALSE", LongName = "Alberta Stock Exchange", Timezone = "Mountain Standard Time" },
                    new Exchange { Name = "AMEX", LongName = "American Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "AMS", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ANTSE", LongName = "Antwerp Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "AO", LongName = "American Stock Exchange (Options)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "API", LongName = "American Petroleum Institute", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "ARCA", LongName = "Archipelago", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "ARCX", LongName = "Archipelago Electronic Communications Network", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "ASE", LongName = "Amsterdam Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ASX", LongName = "Australian Stock Exchange", Timezone = "A.U.S. Eastern Standard Time" },
                    new Exchange { Name = "ASXI", LongName = "Australian Stock Exchange", Timezone = "A.U.S. Eastern Standard Time" },
                    new Exchange { Name = "ATA", LongName = "AEX-Agrarische Termynmarkt", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ATAASE", LongName = "Athens Stock Exchange", Timezone = "GTB Standard Time" },
                    new Exchange { Name = "ATH", LongName = "Athens Stock Exchange", Timezone = "GTB Standard Time" },
                    new Exchange { Name = "ATHI", LongName = "Athens Stock Exchange", Timezone = "GTB Standard Time" },
                    new Exchange { Name = "AUSSE", LongName = "Australian Stock Exchange", Timezone = "A.U.S. Eastern Standard Time" },
                    new Exchange { Name = "B", LongName = "Boston Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BARB", LongName = "Barclays Bank", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BARSE", LongName = "Barcelona Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BASB", LongName = "Basle Bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BASE", LongName = "Buenos Aires Stock Exchange", Timezone = "S.A. Eastern Standard Time" },
                    new Exchange { Name = "BASLE", LongName = "Basle Stocks", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BB", LongName = "Bulletin Board", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BELFOX", LongName = "Euronext Brussels", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BER", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BERNB", LongName = "Bern Bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BERSE", LongName = "Berlin Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BET", LongName = "Budapest Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BFE", LongName = "Baltic Freight Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "BMBSE", LongName = "Bombay Stock Exchange", Timezone = "India Standard Time" },
                    new Exchange { Name = "BMF", LongName = "Bolsa Mecadario Futuro", Timezone = "E. South America Standard Time" },
                    new Exchange { Name = "BO", LongName = "Boston Options Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BOGSE", LongName = "Bogota Stock Exchange", Timezone = "S.A. Pacific Standard Time" },
                    new Exchange { Name = "BOX", LongName = "Boston Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BRN", LongName = "Swiss Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BRU", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BRUSE", LongName = "Brussels Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BRUT", LongName = "Brut", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BSE", LongName = "Bern Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BSP", LongName = "Sao Paulo Stock Exchange", Timezone = "E. South America Standard Time" },
                    new Exchange { Name = "BSPI", LongName = "Sao Paulo Stock Exchange", Timezone = "E. South America Standard Time" },
                    new Exchange { Name = "BT", LongName = "Chicago Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "BTRADE", LongName = "Bloomberg Tradebook", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BUD", LongName = "Budapest Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BUDI", LongName = "Budapest Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BUE", LongName = "Buenos Aires Stock Exchange", Timezone = "S.A. Eastern Standard Time" },
                    new Exchange { Name = "BUEI", LongName = "Buenos Aires Stock Exchange", Timezone = "S.A. Eastern Standard Time" },
                    new Exchange { Name = "BUTLR", LongName = "Butler Harlow", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BVME", LongName = "Italian Exchange - Cash Market", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "C", LongName = "Cincinnati Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CAES", LongName = "Computer Assisted Execution System", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CALC", LongName = "Calculated Indices", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CANX", LongName = "Canadian Mutual Funds (Cannex)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CARSE", LongName = "Caracas Stock Exchange", Timezone = "Pacific S.A. Standard Time" },
                    new Exchange { Name = "CBFX", LongName = "Chemical Bank Forex", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CBOE", LongName = "Chicago Board Options Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CBOT", LongName = "Chicago Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CBT", LongName = "Chicago Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CDE", LongName = "Canadian Derivatives Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CEC", LongName = "Comodities Exchange Center", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CEX", LongName = "Citibank Forex", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CF", LongName = "CBOE Futures Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CFE", LongName = "CBOE Futures Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CFFE", LongName = "Cantor Financial Futures Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CFOREX", LongName = "Crossmar Foreign Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CFXT", LongName = "S&P Comstock Composite Forex", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CME", LongName = "Chicago Mercantile Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CO", LongName = "Chicago Board Options Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "COATS", LongName = "Candian OTC Automated Trading System", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "COLSE", LongName = "Colombo Stock Exchange", Timezone = "Central Asia Standard Time" },
                    new Exchange { Name = "COMEX", LongName = "Commodity Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "COMP", LongName = "Composite Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "COMX", LongName = "Comex Metals", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "COPSE", LongName = "Copenhagen Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "CPC", LongName = "Computer Petroleum Corp", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CSC", LongName = "Coffee, Sugar, and Cocoa Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CSE", LongName = "Cincinnati Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CSEI", LongName = "Coppenhagen Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "D", LongName = "NASDAQ ADF", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "DBN", LongName = "STOXX Indices", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "DBX", LongName = "Stuttgart Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "DBXI", LongName = "Stuttgart Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "DSE", LongName = "Dusseldorf Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "DT", LongName = "Dow Jones", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "DTB", LongName = "EUREX", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "DUB", LongName = "Ireland Stock Exchange", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "DUS", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EBS", LongName = "Swiss Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EBSBW", LongName = "Swiss Market Feed's EBS Project  - Bonds & Warrant", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EBSSTK", LongName = "Swiss Market Feed's EBS Project - Stocks", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EC", LongName = "Chicago Board of Trade (E-mini)", Timezone = "Central Standard Time" },
                    new Exchange { Name = "ECBOT", LongName = "Chicago Board of Trade E-CBOT", Timezone = "Central Standard Time" },
                    new Exchange { Name = "EEB", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EEX", LongName = "European Energy Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EIBI", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EM", LongName = "Chicago Mercantile Exchange (E-mini)", Timezone = "Central Standard Time" },
                    new Exchange { Name = "EOE", LongName = "European Options Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ESE", LongName = "Edmonton Stock Exchange", Timezone = "Mountain Standard Time" },
                    new Exchange { Name = "EUREX", LongName = "Eurex Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EUREXUS", LongName = "Eurex US", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EUS", LongName = "Eurex US Futures", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EUX", LongName = "Eurex", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "FOREX", LongName = "FOREX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "FOX", LongName = "London Future & Options Exchange", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "FRA", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "FTA", LongName = "Euronext NL", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "FTSA", LongName = "Athens FTSE Indices", Timezone = "GTB Standard Time" },
                    new Exchange { Name = "FTSE", LongName = "FTSE Index Values", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "FTSJ", LongName = "Johannesburg FTSE Indices", Timezone = "South Africa Standard Time" },
                    new Exchange { Name = "FUKSE", LongName = "Fukuoaka Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "FWB", LongName = "Frankfurt Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "FX", LongName = "FOREX", Timezone = "Central Standard Time" },
                    new Exchange { Name = "GARVIN", LongName = "Garvin Bonds", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "GB", LongName = "ICAP (Garvin) Bonds", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "GENEVA", LongName = "Geneva Stocks", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "GENEVB", LongName = "Geneva Bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "GLOBEX", LongName = "Chicago Mercantile Exchange (CME GLOBEX)", Timezone = "Central Standard Time" },
                    new Exchange { Name = "HAM", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "HAN", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "HEL", LongName = "Helsinski Stock Exchange", Timezone = "FLE Standard Time" },
                    new Exchange { Name = "HELI", LongName = "Helsinski Stock Exchange", Timezone = "FLE Standard Time" },
                    new Exchange { Name = "HELSE", LongName = "Helsinski Stock Exchange", Timezone = "FLE Standard Time" },
                    new Exchange { Name = "HIRSE", LongName = "Hiroshima Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "HKEX", LongName = "Hong Kong Stock Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "HKFE", LongName = "Hong Kong Futures Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "HKG", LongName = "Hong Kong Stock Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "HKGI", LongName = "Hang Seng Indices", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "HKME", LongName = "Hong Kong Metals Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "HKSE", LongName = "Hong Kong Stock Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "HMBSE", LongName = "Hamburg Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "HSE", LongName = "Helsinski Stock Exchange", Timezone = "FLE Standard Time" },
                    new Exchange { Name = "HSEI", LongName = "Helsinski Stock Exchange", Timezone = "FLE Standard Time" },
                    new Exchange { Name = "HY", LongName = "", Timezone = "" },
                    new Exchange { Name = "IBIS", LongName = "Ibis", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ICE", LongName = "Intercontinental Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "ICEI", LongName = "Iceland Stock Exchange", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "ICP", LongName = "ICAP OTC", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "IDEAL", LongName = "IDEAL IB FOREX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "IDEALPRO", LongName = "IDEAL IB FOREX PRO", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "IDEM", LongName = "Borsa Italiana", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "IDX", LongName = "World Indices", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "IGB", LongName = "Italian Government Bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "INDEX", LongName = "", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "INSNET", LongName = "Instinet", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "INT3B", LongName = "", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "INT3P", LongName = "", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "IO", LongName = "International Securities Exchange (Options)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "IPE", LongName = "International Petroleum Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "IRISE", LongName = "Irish Stock Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "ISE", LongName = "Istanbul Stock Exchange", Timezone = "GTB Standard Time" },
                    new Exchange { Name = "ISEI", LongName = "Iceland Stock Exchange", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "ISLAND", LongName = "INET", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "JAKSE", LongName = "Jakarta Stock Exchange", Timezone = "S.E. Asia Standard Time" },
                    new Exchange { Name = "JASDA", LongName = "Japan Securities Dealers Association", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "JOH", LongName = "Johannesburg Stock Exchange", Timezone = "South Africa Standard Time" },
                    new Exchange { Name = "JOHSE", LongName = "Johannesburg Stock Exchange", Timezone = "South Africa Standard Time" },
                    new Exchange { Name = "JSE", LongName = "Johannesburg Stock Exchange", Timezone = "South Africa Standard Time" },
                    new Exchange { Name = "KARSE", LongName = "Karachi Stock Exchange", Timezone = "West Asia Standard Time" },
                    new Exchange { Name = "KCBOT", LongName = "Kansas City Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "KCBT", LongName = "Kansas City Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "KLS", LongName = "Kuala Lumpur Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "KLSI", LongName = "Kuala Lumpur Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "KOQ", LongName = "KOSDAQ", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KOQI", LongName = "KOSDAQ", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KOR", LongName = "Korea Stock Exchange", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KOREA", LongName = "Seoul Korea Stock Exchange", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KORI", LongName = "Korea Stock Exchange", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KRX", LongName = "Korea Stock Exchange", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KSE", LongName = "Korea Stock Exchange", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KUALA", LongName = "Kuala Lumpur Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "KYOSE", LongName = "Kyoto Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "LCE", LongName = "London Commodity Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LIFE", LongName = "London Inter. Financial Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LIFFE", LongName = "London Inter. Financial Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LIFFE_NF", LongName = "London Inter. Financial Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LIMSE", LongName = "Lima Stock Exchange", Timezone = "S.A. Pacific Standard Time" },
                    new Exchange { Name = "LINC", LongName = "Data Broadcasting Corporation", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "LIS", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "LISSE", LongName = "Lisbon Stock Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LME", LongName = "London Metal Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LON", LongName = "LSE UK Level 1", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LSE", LongName = "London Stock Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LSIN", LongName = "LSE International Market Service", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LSO", LongName = "LIFFE short options add ons and delta factors", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LTO", LongName = "London Traded Options", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LUX", LongName = "Luxembourg Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "LUXI", LongName = "Luxembourg Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "LUXSE", LongName = "Luxembourg Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "M", LongName = "Chicago (Midwest) Stock Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "MAC", LongName = "Madrid Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MADSE", LongName = "Madrid Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MASE", LongName = "MASE Westpac (London Bullion Market)", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "MATF", LongName = "Matif", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MATIF", LongName = "Euronext France", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MATIFF", LongName = "MATIF Financial Futures", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MBFX", LongName = "Midland Bank Forex", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "MC", LongName = "Montreal Futures Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MCEDB", LongName = "Milan CED Borsa", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MDE", LongName = "Madrid Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MDEI", LongName = "Madrid Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ME", LongName = "Montreal Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MEFF", LongName = "Spanish Financial Futures Market: Barcelona", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MEFFO", LongName = "Spanish Options Market: Madrid", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MEFFRV", LongName = "Spanish Futures & Options Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MEFFRY", LongName = "Mercando Ispaniol de Futuros Finansial", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MEX", LongName = "Mexico Stock Exchange", Timezone = "Mexico Standard Time" },
                    new Exchange { Name = "MEXI", LongName = "Mexico Stock Exchange", Timezone = "Mexico Standard Time" },
                    new Exchange { Name = "MEXSE", LongName = "Mexico Stock Exchange", Timezone = "Mexico Standard Time" },
                    new Exchange { Name = "MF", LongName = "Mutual Funds", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MFE", LongName = "Montreal Futures Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MGE", LongName = "Minneapolis Grain Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "MIC", LongName = "Bonneville Market Information Corp.", Timezone = "S.A. Pacific Standard Time" },
                    new Exchange { Name = "MICEX", LongName = "Moscow Interbank Currency Exchange", Timezone = "Russian Standard Time" },
                    new Exchange { Name = "MIDAM", LongName = "MidAmerica Commodity Exchange", Timezone = "Central America Standard Time" },
                    new Exchange { Name = "MIDWES", LongName = "Midwest Stock Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "MM", LongName = "Money Market Fund", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MONE", LongName = "Mercado Espanol de Futuros Finacial", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MONEP", LongName = "Paris Options", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MSE", LongName = "Montreal Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MSEI", LongName = "Milan Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MSO", LongName = "Montreal Stock Options", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MSPT", LongName = "Milan Stock Pit trading and corporate bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MT", LongName = "Chicago Mercantile Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "MTS", LongName = "Milan Telematico Stocks", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MUN", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MUNSE", LongName = "Munich Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MVSE", LongName = "Montevideo Stock Exchange", Timezone = "S.A. Eastern Standard Time" },
                    new Exchange { Name = "MX", LongName = "Montreal Futures Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "N", LongName = "New York Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASD", LongName = "NASDAQ", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASDAQ", LongName = "NASDAQ National Market", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASDC", LongName = "National Assoc. of Securities Dealers (Canada)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASDSC", LongName = "NASDAQ Small Caps", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NAT", LongName = "Nat West Bank Forex", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "NB", LongName = "New York Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NGYSE", LongName = "Nagoya Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "NIGSE", LongName = "Niigata Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "NIKKEI", LongName = "Nikkei-Needs Database", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "NLK", LongName = "Amsterdam", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "NLX", LongName = "National Labor Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NO", LongName = "New York Stock Exchange (Options)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NOTCBF", LongName = "Nasdaq OTC Bulletin Board Service : Foreign Issues", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NPE", LongName = "OMX Nordpool", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "NQLX", LongName = "Nasdaq LIFFE Markets", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NSE", LongName = "National Stock Exchange of India", Timezone = "India Standard Time" },
                    new Exchange { Name = "NYBOT", LongName = "New York Board of Trade", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYCE", LongName = "New York Cotton Exchange(CEC)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYFE", LongName = "New York Futures Exchange(CEC)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYLCD", LongName = "London Inter. Financial Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "NYLID", LongName = "London Inter. Financial Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "NYLUS", LongName = "Chicago Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "NYME", LongName = "New York Mercantile Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYMEX", LongName = "New York Mercantile Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYMI", LongName = "New York Mercantile Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYSE", LongName = "New York Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYSELIFFE", LongName = "New York Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NZSE", LongName = "New Zealand Stock Exchange", Timezone = "New Zealand Standard Time" },
                    new Exchange { Name = "NZX", LongName = "New Zealand Stock Exchange", Timezone = "New Zealand Standard Time" },
                    new Exchange { Name = "NZXI", LongName = "New Zealand Stock Exchange", Timezone = "New Zealand Standard Time" },
                    new Exchange { Name = "OETOB", LongName = "Austrian Stock and Options Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OFX", LongName = "OFEX London", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "OM", LongName = "OMX Derivatives Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OMFE", LongName = "Oslo Opsjon", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OMS", LongName = "Stockholm Exchange - Derivatives Market", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ONE", LongName = "OneChicago", Timezone = "Central Standard Time" },
                    new Exchange { Name = "OPRA", LongName = "Option Price Reporting Authority", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "OPTS", LongName = "Generic Options Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "OSASE", LongName = "Osaka Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "OSE", LongName = "Osaka Securities Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "OSE.JPN", LongName = "Osaka Securities Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "OSEI", LongName = "Osaka Securities Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "OSL", LongName = "Oslo Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OSLI", LongName = "Oslo Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OSLSE", LongName = "Oslo Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OT", LongName = "Pink Sheets", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "OTC", LongName = "Over The Counter", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "OTCBB", LongName = "OTC Bulletin Board", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "P", LongName = "Pacific Stock Exchange", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "PACIFI", LongName = "Pacific Stock Exchange", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "PAR", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "PASE", LongName = "Paris Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "PBT", LongName = "Philadelphia Board of Trade", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "PF", LongName = "PetroFlash", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "PHLX", LongName = "Philadelphia Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "PHPSE", LongName = "Philippine Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "PO", LongName = "Pacific Stock Exchange (Options)", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "PRIMI", LongName = "Milan Option", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "PSE", LongName = "Pacific Exchange", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "PSOFT", LongName = "Paris Softs", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "Q", LongName = "NASDAQ NMS", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "RIOSE", LongName = "Rio de Janeiro Stock Exchange", Timezone = "E. South America Standard Time" },
                    new Exchange { Name = "RTS", LongName = "Russian Trading System", Timezone = "Russian Standard Time" },
                    new Exchange { Name = "S", LongName = "NASDAQ Small Cap", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "SANSE", LongName = "Santiago Stock Exchange", Timezone = "Pacific S.A. Standard Time" },
                    new Exchange { Name = "SAPSE", LongName = "Sapporo Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "SBF", LongName = "Euronext France", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SEAQ", LongName = "International (US Securities traded on London Exch", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "SEAQL2", LongName = "SEAQ International Level 2", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "SEAQTR", LongName = "SEAQ International Trades Data", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "SES", LongName = "Singapore Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SESI", LongName = "Singapore Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SET", LongName = "Bangkok Stock Exchange of Thailand", Timezone = "S.E. Asia Standard Time" },
                    new Exchange { Name = "SETI", LongName = "Bangkok Stock Exchange of Thailand", Timezone = "S.E. Asia Standard Time" },
                    new Exchange { Name = "SFB", LongName = "Stockholm Exchange - Stock Market", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SFE", LongName = "Sydney Futures Exchange", Timezone = "A.U.S. Eastern Standard Time" },
                    new Exchange { Name = "SGX", LongName = "Singapore Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SHANG", LongName = "Shanghai Stock Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "SHENZ", LongName = "Shenzen Stock Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "SICOVA", LongName = "Paris Bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SIMEX", LongName = "Singapore International Monetary Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SIMX", LongName = "Singapore International Monetary Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SING", LongName = "Stock Exchange of Singapore", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SMART", LongName = "Smart", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "SNFE", LongName = "Sydney Futures Exchange", Timezone = "A.U.S. Eastern Standard Time" },
                    new Exchange { Name = "SOFET", LongName = "Soffex Swiss Futures Financial Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SOFFEX", LongName = "EUREX", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SPAIN", LongName = "Mercato Continue Espana", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SPCC", LongName = "SPC Combined", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SPE", LongName = "SPECTRON", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SPECI", LongName = "Special", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "SPSE", LongName = "Sao Paulo Stock Exchange", Timezone = "E. South America Standard Time" },
                    new Exchange { Name = "SSE", LongName = "Stockholm Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SSEI", LongName = "Stockholm Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "STKSE", LongName = "Stockholm Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "STREET", LongName = "Street Software", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "STX", LongName = "STOXX Indices", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SWB", LongName = "Stuttgart Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SWE", LongName = "Swiss Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SWEI", LongName = "Swiss Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SWX", LongName = "Swiss Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SWXI", LongName = "Swiss Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "T", LongName = "NASDAQ Listed Stocks", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "TAISE", LongName = "Taipei Stock Exchange of Taiwan", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "TC", LongName = "Toronto Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "TELSE", LongName = "Tel Aviv Stock Exchange", Timezone = "Israel Standard Time" },
                    new Exchange { Name = "TFE", LongName = "Toronto Futures Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "TIFFE", LongName = "Tokyo International Financial Futures Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "TSE", LongName = "Toronto Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "TSE.JPN", LongName = "Tokyo Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "TSEI", LongName = "Tokyo Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "TSO", LongName = "Toronto Stock Options", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "TUL", LongName = "Tullett and Tokyo", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "U", LongName = "NASDAQ Bulletin Board", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "UNDEF", LongName = "Undefined", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "USDA", LongName = "U.S. Department of Agriculture", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "USDC", LongName = "U.S. Department of Commerce", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "UT", LongName = "Chicago Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "V", LongName = "NASDAQ Bulletin Board (pink)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "VC", LongName = "TSX Venture Exchange (CDNX)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "VIE", LongName = "Wiener Borse Vienna Stock Exchange", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "VIEI", LongName = "Wiener Borse Vienna Stock Exchange", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "VIESE", LongName = "Vienna Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "VIRTX", LongName = "VIRT-X", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "VSE", LongName = "Vancouver Stock Exchange", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "VSO", LongName = "Vancouver Stock Options", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "VWAP", LongName = "IB VWAP Dealing Network", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "W", LongName = "CBOE (Non options) (QQQ, SPY)", Timezone = "Central Standard Time" },
                    new Exchange { Name = "WAR", LongName = "Warsaw Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "WARI", LongName = "Warsaw Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "WBE", LongName = "Wiener Borse Vienna Stock Exchange", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "WBEI", LongName = "Wiener Borse Vienna Stock Exchange", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "WCE", LongName = "Winnipeg Commodity Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "WOLFF", LongName = "Rudolf Wolff (London Metals)", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "X", LongName = "Philadelphia Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "XET", LongName = "Deutshe Borse Stocks Level 1", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "XETRA", LongName = "XETRA Exchange", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "XO", LongName = "Philadelphia Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "ZS", LongName = "Zurich Stocks", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "ZURIB", LongName = "Zurich Bonds", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "VENTURE", LongName = "TSX Venture", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CHX", LongName = " Chicago Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "DRCTEDGE", LongName = "Direct Edge", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NSX", LongName = "National Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BEX", LongName = "NASDAQ OMX BX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CBSX", LongName = "CBOE Stock Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "BATS", LongName = "BATS", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "EDGEA", LongName = "Direct Edge", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CHIXEN", LongName = "CHI-X Europe Ltd Clearnet", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BATEEN", LongName = "BATS Europe", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "VALUE", LongName = "IB Value Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "LAVA", LongName = "LavaFlow ECN", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CSFBALGO", LongName = "", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "JEFFALGO", LongName = "", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BYX", LongName = "BATS BYX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "PSX", LongName = "NASDAQ OMX PSX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "GEMINI", LongName = "ISE Gemini", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASDAQBX", LongName = "NASDAQ OMX BX Options Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASDAQOM", LongName = "NASDAQ OMX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CBOE2", LongName = "CBOE C2", Timezone = "Central Standard Time" },
                    new Exchange { Name = "MIAX", LongName = "MIAX Options Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "A", LongName = "American Stock Exchange", Timezone = "Eastern Standard Time" }
                };

            foreach (Exchange e in exchanges)
            {
                context.Exchanges.AddOrUpdate(x => x.Name, e);
            }
            #endregion

            context.SaveChanges();

            #region exchangesessions
            #endregion

            #region instruments

            var spy = new Instrument
            {
                Symbol = "SPY",
                Currency = "USD",
                Type = InstrumentType.Stock,
                UnderlyingSymbol = "SPY",
                Datasource = context.Datasources.First(x => x.Name == "Yahoo"),
                Name = "SPDR S&P 500 ETF Trust",
                Multiplier = 1,
                MinTick = 0.01m,
                Industry = "Funds",
                Category = "Equity Fund",
                Subcategory = "Growth&Income-Large Cap",
                Exchange = context.Exchanges.First(x => x.Name == "NYSE"),
                SessionsSource = SessionsSource.Template,
                SessionTemplateID = context.SessionTemplates.First(x => x.Name == "U.S. Equities RTH").ID,
            };
            spy.Sessions = new List<InstrumentSession>();

            foreach (TemplateSession t in context.TemplateSessions.Where(x => x.TemplateID == spy.SessionTemplateID))
            {
                spy.Sessions.Add(t.ToInstrumentSession());
            }

            context.Instruments.AddOrUpdate(x => new {x.Symbol, x.DatasourceID, x.Expiration, x.ExchangeID}, spy);
            #endregion

            context.SaveChanges();

            context.Dispose();
        }
    }
}
