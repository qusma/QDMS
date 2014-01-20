using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using MySql.Data.Entity;
using QDMS;
using System.Data.Entity.Migrations;

namespace EntityData.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<MyDBContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            SetSqlGenerator("MySql.Data.MySqlClient", new MySqlMigrationSqlGenerator());
            //SetSqlGenerator("System.Data.SqlClient", new SqlServerMigrationSqlGenerator());
            //TODO solution?
        }
         
        protected override void Seed(MyDBContext context)
        {
            #region datasources
            var ib = new Datasource { Name = "Interactive Brokers" };
            var yahoo = new Datasource { Name = "Yahoo" };
            var quandl = new Datasource { Name = "Quandl" };

            context.Datasources.Add(ib);
            context.Datasources.Add(yahoo);
            context.Datasources.Add(quandl);
            #endregion

            #region underlyingSymbols
            var eur = new UnderlyingSymbol { Symbol = "6E" };
            eur.Rule = new ExpirationRule
            {
                DaysBefore = 2,
                DayType = DayType.Business,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = false,
                ReferenceWeekDay = DayOfTheWeek.Wednesday,
                ReferenceWeekDayCount = WeekDayCount.Third
            };
            context.UnderlyingSymbols.Add(eur);

            var cl = new UnderlyingSymbol { Symbol = "CL" };
            cl.Rule = new ExpirationRule
            {
                DaysBefore = 3,
                DayType = DayType.Business,
                ReferenceRelativeMonth = RelativeMonth.PreviousMonth,
                ReferenceUsesDays = true,
                ReferenceDays = 25,
                ReferenceDayMustBeBusinessDay = true
            };
            context.UnderlyingSymbols.Add(cl);

            var zw = new UnderlyingSymbol { Symbol = "ZW" };
            zw.Rule = new ExpirationRule
            {
                DaysBefore = 1,
                DayType = DayType.Business,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = true,
                ReferenceDays = 15
            };
            context.UnderlyingSymbols.Add(zw);

            var es = new UnderlyingSymbol { Symbol = "ES" };
            es.Rule = new ExpirationRule
            {
                DaysBefore = 0,
                DayType = DayType.Calendar,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = false,
                ReferenceWeekDay = DayOfTheWeek.Friday,
                ReferenceWeekDayCount = WeekDayCount.Third
            };
            context.UnderlyingSymbols.Add(es);


            var nq = new UnderlyingSymbol { Symbol = "NQ" };
            nq.Rule = new ExpirationRule
            {
                DaysBefore = 0,
                DayType = DayType.Calendar,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = false,
                ReferenceWeekDay = DayOfTheWeek.Friday,
                ReferenceWeekDayCount = WeekDayCount.Third
            };
            context.UnderlyingSymbols.Add(nq);

            var gc = new UnderlyingSymbol { Symbol = "GC" };
            gc.Rule = new ExpirationRule
            {
                DaysBefore = 2,
                DayType = DayType.Business,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = false,
                ReferenceDayIsLastBusinessDayOfMonth = true
            };
            context.UnderlyingSymbols.Add(gc);

            var ub = new UnderlyingSymbol { Symbol = "UB" };
            ub.Rule = new ExpirationRule
            {
                DaysBefore = 7,
                DayType = DayType.Business,
                ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                ReferenceUsesDays = false,
                ReferenceDayIsLastBusinessDayOfMonth = true
            };
            context.UnderlyingSymbols.Add(ub);

            var vix = new UnderlyingSymbol { Symbol = "VIX" };
            vix.Rule = new ExpirationRule
            {
                DaysBefore = 30,
                DayType = DayType.Calendar,
                ReferenceRelativeMonth = RelativeMonth.NextMonth,
                ReferenceUsesDays = false,
                ReferenceWeekDay = DayOfTheWeek.Friday,
                ReferenceWeekDayCount = WeekDayCount.Third,
                ReferenceDayMustBeBusinessDay = true
            };
            context.UnderlyingSymbols.Add(vix);
            #endregion
        }
    }
}
