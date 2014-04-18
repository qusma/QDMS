// -----------------------------------------------------------------------
// <copyright file="MySQLBackup.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MySql.Data.MySqlClient;

namespace QDMSServer
{
    public static class MySQLBackup
    {
        /// <summary>
        /// Back up a MySql database to the specified file.
        /// </summary>
        public static void Backup(string path, string conectionString)
        {
            using (MySqlConnection conn = new MySqlConnection(conectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("", conn))
                {
                    conn.Open();
                    var backup = new MySqlBackup(cmd);
                    backup.ExportToFile(path);
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Back up a MySql database from the specified file.
        /// </summary>
        public static void Restore(string path, string conectionString)
        {
            using (MySqlConnection conn = new MySqlConnection(conectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("", conn))
                {
                    conn.Open();
                    var backup = new MySqlBackup(cmd);
                    backup.ImportFromFile(path);
                    conn.Close();
                }
            }
        }
    }
}
