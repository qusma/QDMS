// -----------------------------------------------------------------------
// <copyright file="DBUtils.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text;
using MySql.Data.MySqlClient;
using QDMSServer.Properties;
using System.Security.Cryptography;

namespace QDMSServer
{
    public static class DBUtils
    {
        public static void SetConnectionString()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings conSettings = config.ConnectionStrings.ConnectionStrings["qdmsEntitiesMySql"];

            //this is an extremely dirty hack that allows us to change the connection string at runtime
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(conSettings, false);

            conSettings.ConnectionString = string.Format("User Id={0};Password={1};Host={2};Database=qdms;Persist Security Info=True",
                Settings.Default.mySqlUsername,
                Unprotect(Settings.Default.mySqlPassword),
                Settings.Default.mySqlHost);

            config.Save();
            ConfigurationManager.RefreshSection("connectionStrings");
            
        }

        public static string Unprotect(string encryptedString)
        {
            byte[] buffer = ProtectedData.Unprotect(Convert.FromBase64String(encryptedString), null, DataProtectionScope.CurrentUser);

            return Encoding.Unicode.GetString(buffer);
        }

        public static string Protect(string unprotectedString)
        {
            byte[] buffer = ProtectedData.Protect(Encoding.Unicode.GetBytes(unprotectedString), null, DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(buffer);
        }

        public static SqlConnection CreateSqlServerConnection(string database = "qdms", string server = null, string username = null, string password = null, bool noDB = false)
        {
            string connectionString = String.Format(
                "Data Source={0};",
                server ?? Settings.Default.mySqlHost);

            if (!noDB)
            {
                connectionString += String.Format("Initial Catalog={0};", database);
            }

            if(!string.IsNullOrEmpty(username)) //user/pass authentication
            {
                if(password == null)
                {
                    try
                    {
                        password = Unprotect(Settings.Default.mySqlPassword);
                    }
                    catch
                    {
                        password = "";
                    }
                }
                connectionString += string.Format("User ID={0};Password={1};", username, password);
            }
            else //windows authentication
            {
                connectionString += "Integrated Security=True;";
            }

            return new SqlConnection(connectionString);
        }

        public static MySqlConnection CreateMySqlConnection(string database = "qdms", string server = null, string username = null, string password = null, bool noDB = false)
        {
            if (password == null)
            {
                try
                {
                    password = Unprotect(Settings.Default.mySqlPassword);
                }
                catch
                {
                    password = "";
                }
            }

            string connectionString = String.Format(
                "server={0};" +
                "user id={1};" +
                "Password={2};",
                server ?? Settings.Default.mySqlHost,
                username ?? Settings.Default.mySqlUsername,
                password
                );
            
            if (!noDB)
            {
                connectionString += String.Format("database={0};", database);
            }

            connectionString +=
                "allow user variables=true;" +
                "persist security info=true;" +
                "Convert Zero Datetime=True";

            return new MySqlConnection(connectionString);
        }

        public static string GetSQLResource(string name)
        {
            string sql = "";

            string filename = "QDMSServer.Resources." + name;

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename))
            {
                if (stream != null)
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        sql = streamReader.ReadToEnd();
                    }
                }
            }

            return sql;
        }

        public static bool CheckDBExists()
        {
            using (var connection = CreateMySqlConnection(noDB: true))
            {
                connection.Open();
                var cmd = new MySqlCommand("SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = 'qdms'", connection);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) //database does not exist!
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
