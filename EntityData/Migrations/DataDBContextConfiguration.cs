// -----------------------------------------------------------------------
// <copyright file="Configuration.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Configuration;
using System.Data.Entity.Migrations;
using MySql.Data.Entity;
using QDMS;

//Note: the namespace is a hack, the two context files have to be in different namespaces for the thing to work.
namespace EntityData.Migrations.DataDBContextNS
{
    internal sealed class DataDBContextConfiguration : DbMigrationsConfiguration<DataDBContext>
    {
        public DataDBContextConfiguration()
        {
            AutomaticMigrationsEnabled = true;
            SetSqlGenerator("MySql.Data.MySqlClient", new MySqlMigrationSqlGenerator());
            //SetSqlGenerator("System.Data.SqlClient", new SqlServerMigrationSqlGenerator());


            //Dirty hack. Check the database. If we're using MySql we need to use a special HistoryContext
            //to bypass the problem of the too-long key when the default charset is UTF8.
            string provider = ConfigurationManager.ConnectionStrings["qdmsDataEntities"].ProviderName;

            if (provider == "MySql.Data.MySqlClient")
            {
                SetHistoryContextFactory(MySqlProviderInvariantName.ProviderName,
                    (existingConnection, defaultSchema) => new EntityData.Migrations.MySqlHistoryContext(existingConnection, defaultSchema));
            }
        }

        protected override void Seed(DataDBContext context)
        {
            base.Seed(context);
        }
    }
}