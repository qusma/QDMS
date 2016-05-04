// -----------------------------------------------------------------------
// <copyright file="DataStorageFactory.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using QDMSServer.DataSources;

namespace QDMSServer
{
    public static class DataStorageFactory
    {
        public static IDataStorage Get()
        {
            switch(Properties.Settings.Default.databaseType)
            {
                case "MySql":
                    return new MySQLStorage(DBUtils.GetMySqlServerConnectionString("qdmsdata"));

                case "SqlServer":
                    return new SqlServerStorage(DBUtils.GetSqlServerConnectionString("qdmsdata", useWindowsAuthentication: Properties.Settings.Default.sqlServerUseWindowsAuthentication));

                default:
                    return new MySQLStorage(DBUtils.GetMySqlServerConnectionString("qdmsdata"));
            }
        }
    }
}
