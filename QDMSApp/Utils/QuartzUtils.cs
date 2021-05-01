// -----------------------------------------------------------------------
// <copyright file="QuartzUtils.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using QDMSApp.Properties;

namespace QDMSApp
{
    public static class QuartzUtils
    {
        public static NameValueCollection GetQuartzSettings(string dbType)
        {
            if (dbType == "MySql")
            {
                return GetQuartzSettingsMySql();
            }
            else if (dbType == "SqlServer")
            {
                return GetQuartzSettingsSqlServer();
            }

            throw new Exception("Unknown db type");
        }

        /// <summary>
        /// Creates the quartz database if it does not exist
        /// </summary>
        /// <param name="dbType"></param>
        public static void InitializeDatabase(string dbType)
        {
            if (dbType == "MySql")
            {
                InitializeMySqlDb();
            }
            else if (dbType == "SqlServer")
            {
                InitializeSqlServerDb();
            }
        }

        private static void InitializeSqlServerDb()
        {
            using (var conn = DBUtils.CreateSqlServerConnection("",
                Settings.Default.sqlServerHost,
                Settings.Default.sqlServerUsername,
                EncryptionUtils.Unprotect(Settings.Default.sqlServerPassword),
                useWindowsAuthentication: Settings.Default.sqlServerUseWindowsAuthentication,
                noDB: true))
            {
                conn.Open();
                using (var cmd = new SqlCommand("", conn))
                {
                    cmd.CommandText = @"SELECT database_id FROM sys.databases WHERE Name = 'qdmsQuartz'";
                    object id = cmd.ExecuteScalar();
                    if (id == null)
                    {
                        //db does not exist, create it
                        var commands = new List<string>();
                        commands.Add("CREATE DATABASE qdmsQuartz");
                        commands.Add("USE qdmsQuartz");
                        Regex regex = new Regex(@"\r{0,1}\nGO\r{0,1}\n");
                        commands.AddRange(regex.Split(Resources.QuartzSqlServerDbInit)); //needed because all the 'GO' things in the script;

                        foreach (string command in commands)
                        {
                            cmd.CommandText = command;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private static void InitializeMySqlDb()
        {
            using (var conn = DBUtils.CreateMySqlConnection(noDB: true))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("", conn))
                {
                    cmd.CommandText = "SHOW DATABASES LIKE 'qdmsQuartz'";
                    var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        //db does not exist, create it
                        reader.Close();

                        cmd.CommandText = @"CREATE DATABASE qdmsQuartz;
                                          USE qdmsQuartz;"
                                          + Resources.QuartzMySqlDbInit;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static NameValueCollection GetQuartzSettingsSqlServer()
        {
            NameValueCollection properties = new NameValueCollection();

            properties["quartz.scheduler.instanceName"] = "QdmsScheduler";
            properties["quartz.scheduler.instanceId"] = "instance_one";
            properties["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz";
            properties["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.SqlServerDelegate, Quartz";
            properties["quartz.jobStore.useProperties"] = "true";
            properties["quartz.jobStore.dataSource"] = "default";
            properties["quartz.jobStore.tablePrefix"] = "QRTZ_";

            //properties["quartz.jobStore.lockHandler.type"] = "Quartz.Impl.AdoJobStore.UpdateLockRowSemaphore, Quartz";

            properties["quartz.dataSource.default.connectionString"] = DBUtils.GetSqlServerConnectionString(
                "qdmsQuartz",
                Settings.Default.sqlServerHost,
                Settings.Default.sqlServerUsername,
                EncryptionUtils.Unprotect(Settings.Default.sqlServerPassword),
                useWindowsAuthentication: Settings.Default.sqlServerUseWindowsAuthentication);
            properties["quartz.dataSource.default.provider"] = "SqlServer-20";

            return properties;
        }

        private static NameValueCollection GetQuartzSettingsMySql()
        {
            NameValueCollection properties = new NameValueCollection();

            properties["quartz.scheduler.instanceName"] = "QdmsScheduler";
            properties["quartz.scheduler.instanceId"] = "instance_one";
            properties["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz";
            properties["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.MySQLDelegate, Quartz";
            properties["quartz.jobStore.useProperties"] = "true";
            properties["quartz.jobStore.dataSource"] = "default";
            properties["quartz.jobStore.tablePrefix"] = "QRTZ_";

            properties["quartz.dataSource.default.connectionString"] = DBUtils.GetMySqlServerConnectionString("qdmsQuartz");
            properties["quartz.dataSource.default.provider"] = "MySql-65";

            return properties;
        }
    }
}
