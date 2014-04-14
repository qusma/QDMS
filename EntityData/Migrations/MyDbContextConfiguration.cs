// -----------------------------------------------------------------------
// <copyright file="Configuration.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Configuration;
using System.Data.Entity.Migrations;
using MySql.Data.Entity;

namespace EntityData.Migrations
{
    public class MyDbContextConfiguration : DbMigrationsConfiguration<MyDBContext>
    {
        public MyDbContextConfiguration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;

            SetSqlGenerator("MySql.Data.MySqlClient", new MySqlMigrationSqlGenerator());
            //SetSqlGenerator("System.Data.SqlClient", new SqlServerMigrationSqlGenerator());

            //Dirty hack. Check the database. If we're using MySql we need to use a special HistoryContext
            //to bypass the problem of the too-long key when the default charset is UTF8.
            string provider = ConfigurationManager.ConnectionStrings["qdmsEntities"].ProviderName;

            if (provider == "MySql.Data.MySqlClient")
            {
                SetHistoryContextFactory(MySqlProviderInvariantName.ProviderName,
                    (existingConnection, defaultSchema) => new MySqlHistoryContext(existingConnection, defaultSchema));
            }
            
        }
         
        protected override void Seed(MyDBContext context)
        {
            base.Seed(context);
        }
    }
}
