// -----------------------------------------------------------------------
// <copyright file="DbCreationTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Reflection;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using EntityData;
using MySql.Data.Entity;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class DbCreationTest
    {
        private readonly string _mySqlPassword = "Password12!";
        private readonly string _mySqlUsername = "root";
        private readonly string _mySqlHost = "127.0.0.1";

        private readonly string _sqlServerPassword = "Password12!";
        private readonly string _sqlServerHost = "(local)\\SQL2016";
        private readonly string _sqlServerUsername = "sa";
        private readonly bool _useWindowsAuthentication = false;

        [Test]
        public void MySqlDbIsCreatedSuccessfully()
        {
            using (var conn = new MySqlConnection(GetMySqlConnString(_mySqlUsername, _mySqlPassword, _mySqlHost)))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("", conn))
                {
                    cmd.CommandText = @"DROP DATABASE IF EXISTS qdms_test;
                                        CREATE DATABASE qdms_test;
                                        DROP DATABASE IF EXISTS qdmsdata_test;
                                        CREATE DATABASE qdmsdata_test;";
                    cmd.ExecuteNonQuery();
                }
            }

            SetConnectionString("qdmsEntities", GetMySqlConnString(_mySqlUsername, _mySqlPassword, _mySqlHost, "qdms_test"), "MySql.Data.MySqlClient");
            SetConnectionString("qdmsDataEntities", GetMySqlConnString(_mySqlUsername, _mySqlPassword, _mySqlHost, "qdmsdata_test"), "MySql.Data.MySqlClient");

            ConfigurationManager.RefreshSection("connectionStrings");

            DbConfiguration.SetConfiguration(new MySqlEFConfiguration());

            using (var ctx = new MyDBContext())
            {
                ctx.Database.Initialize(true);
                Seed.SeedDatasources(ctx);
                Seed.DoSeed();
            }

            

            using (var ctx = new DataDBContext())
            {
                ctx.Database.Initialize(true);
            }
        }

        private static string GetMySqlConnString(string username, string password, string host, string db = null)
        {
             string connStr = string.Format("User Id={0};Password={1};Host={2};Persist Security Info=True;",
                username,
                password,
                host);

            if (!string.IsNullOrEmpty(db))
            {
                connStr+= $"Database={db};";
            }

            connStr +=
                "allow user variables=true;" +
                "persist security info=true;" +
                "Convert Zero Datetime=True";

            return connStr;
        }

        [Test]
        public void SqlServerDbIsCreatedSuccessfully()
        {
            using (var conn = new SqlConnection(GetSqlServerConnString("master", _sqlServerHost, _sqlServerUsername, _sqlServerPassword, false, _useWindowsAuthentication)))
            {
                conn.Open();
                using (var cmd = new SqlCommand("", conn))
                {
                    cmd.CommandText = @"IF EXISTS(SELECT name FROM sys.databases WHERE name = 'qdms_test')
                                            DROP DATABASE qdms_test";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = @"IF EXISTS(SELECT name FROM sys.databases WHERE name = 'qdmsdata_test')
                                            DROP DATABASE qdmsdata_test";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "CREATE DATABASE qdms_test";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "CREATE DATABASE qdmsdata_test";
                    cmd.ExecuteNonQuery();
                }
            }

            SetConnectionString("qdmsEntities", GetSqlServerConnString("qdms_test", _sqlServerHost, _sqlServerUsername, _sqlServerPassword, false, _useWindowsAuthentication), "System.Data.SqlClient");
            SetConnectionString("qdmsDataEntities", GetSqlServerConnString("qdmsdata_test", _sqlServerHost, _sqlServerUsername, _sqlServerPassword, false, _useWindowsAuthentication), "System.Data.SqlClient");

            ConfigurationManager.RefreshSection("connectionStrings");

            using (var ctx = new MyDBContext())
            {
                ctx.Database.Initialize(true);
                Seed.SeedDatasources(ctx);
                Seed.DoSeed();
            }

            using (var ctx = new DataDBContext())
            {
                ctx.Database.Initialize(true);
            }
        }

        internal static string GetSqlServerConnString(string database, string server, string username = null, string password = null, bool noDB = false, bool useWindowsAuthentication = true)
        {
            string connectionString = $"Data Source={server};";

            if (!noDB)
            {
                connectionString += $"Initial Catalog={database};";
            }

            if (!useWindowsAuthentication) //user/pass authentication
            {
                connectionString += $"User ID={username};Password={password};";
            }
            else //windows authentication
            {
                connectionString += "Integrated Security=True;";
            }

            return connectionString;
        }

        private static void SetConnectionString(string connName, string connStr, string providerName)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings conSettings = config.ConnectionStrings.ConnectionStrings[connName];

            //this is an extremely dirty hack that allows us to change the connection string at runtime
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(conSettings, false);

            conSettings.ConnectionString = connStr;
            conSettings.ProviderName = providerName;

            config.Save();
        }
    }
}
