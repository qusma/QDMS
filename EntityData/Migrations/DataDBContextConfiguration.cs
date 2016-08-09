// -----------------------------------------------------------------------
// <copyright file="Configuration.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity.Migrations;
using MySql.Data.Entity;

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
        }

        protected override void Seed(DataDBContext context)
        {
            base.Seed(context);
        }
    }
}