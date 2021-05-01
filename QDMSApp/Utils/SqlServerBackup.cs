// -----------------------------------------------------------------------
// <copyright file="SqlServerBackup.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.SqlClient;

namespace QDMSApp
{
    public static class SqlServerBackup
    {
        public static void Backup(string connectionString, string database, string file)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("", conn);
                cmd.CommandText = string.Format(
                                        @"USE {0};
                                        GO
                                        BACKUP DATABASE {0}
                                        TO DISK = '{1}'
                                           WITH FORMAT,
                                              NAME = 'QDMS Server backup of {0}';
                                        GO", 
                                        database, 
                                        file);
                cmd.ExecuteNonQuery();
            }
        }

        public static void Restore(string connectionString, string database, string file)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("", conn);
                cmd.CommandText = string.Format(
                                        @"USE master
                                        GO
                                        RESTORE DATABASE {0}
                                            FROM DISK = '{1}'
                                        WITH REPLACE
                                        GO", 
                                        database, 
                                        file);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
