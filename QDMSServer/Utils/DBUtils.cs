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
            if(Settings.Default.databaseType == "MySql")
                SetMySqlConnectionString();
            else
                SetSqlServerConnectionString();

            ConfigurationManager.RefreshSection("connectionStrings");
        }

        private static void SetSqlServerConnectionString()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings conSettings = config.ConnectionStrings.ConnectionStrings["qdmsEntities"];

            //this is an extremely dirty hack that allows us to change the connection string at runtime
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(conSettings, false);

            conSettings.ConnectionString = GetSqlServerConnectionString(
                "qdms",
                Settings.Default.sqlServerHost,
                Settings.Default.sqlServerUsername,
                Unprotect(Settings.Default.sqlServerPassword),
                useWindowsAuthentication: Settings.Default.sqlServerUseWindowsAuthentication);
            conSettings.ProviderName = "System.Data.SqlClient";

            config.Save();
        }

        private static void SetMySqlConnectionString()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings conSettings = config.ConnectionStrings.ConnectionStrings["qdmsEntities"];

            //this is an extremely dirty hack that allows us to change the connection string at runtime
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(conSettings, false);

            conSettings.ConnectionString = string.Format("User Id={0};Password={1};Host={2};Database=qdms;Persist Security Info=True",
                Settings.Default.mySqlUsername,
                Unprotect(Settings.Default.mySqlPassword),
                Settings.Default.mySqlHost);
            conSettings.ProviderName = "MySql.Data.MySqlClient";

            config.Save();
        }

        /// <summary>
        /// Used for decrypting db passwords.
        /// </summary>
        public static string Unprotect(string encryptedString)
        {
            byte[] buffer;
            try
            {
                buffer = ProtectedData.Unprotect(Convert.FromBase64String(encryptedString), null, DataProtectionScope.CurrentUser);
            }
            catch (Exception ex)
            {
                //if it's empty or incorrectly formatted, we get an exception. Just return an empty string.
                return "";
            }
            return Encoding.Unicode.GetString(buffer);
        }

        /// <summary>
        /// Used for encrypting db passwords.
        /// </summary>
        public static string Protect(string unprotectedString)
        {
            byte[] buffer = ProtectedData.Protect(Encoding.Unicode.GetBytes(unprotectedString), null, DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(buffer);
        }

        public static SqlConnection CreateSqlServerConnection(string database = "qdms", string server = null, string username = null, string password = null, bool noDB = false, bool useWindowsAuthentication = true)
        {
            string connectionString = GetSqlServerConnectionString(database, server, username, password, noDB, useWindowsAuthentication);
            return new SqlConnection(connectionString);
        }

        private static string GetSqlServerConnectionString(string database = "qdms", string server = null, string username = null, string password = null, bool noDB = false, bool useWindowsAuthentication = true)
        {
            string connectionString = String.Format(
                "Data Source={0};",
                server ?? Settings.Default.sqlServerHost);

            if (!noDB)
            {
                connectionString += String.Format("Initial Catalog={0};", database);
            }

            if (!useWindowsAuthentication) //user/pass authentication
            {
                if (password == null)
                {
                    try
                    {
                        password = Unprotect(Settings.Default.sqlServerPassword);
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
            return connectionString;
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

        /// <summary>
        /// Returns the contents of a text file in the Resources folder. Used for the db creation.
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        public static bool CheckDBExists()
        {
            if (Settings.Default.databaseType == "MySql")
                return CheckMySqlDBExists();
            else
                return true; //TODO write
        }

        private static bool CheckMySqlDBExists()
        {
            using (var connection = CreateMySqlConnection(noDB: true))
            {
                connection.Open();
                var cmd = new MySqlCommand("SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = 'qdmsdata'", connection);
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
