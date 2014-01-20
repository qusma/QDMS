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
                    return new MySQLStorage();

                case "SqlServer":
                    return new SqlServerStorage();

                default:
                    return new MySQLStorage();
            }
        }
    }
}
