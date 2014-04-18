// -----------------------------------------------------------------------
// <copyright file="DbBackup.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Configuration;
using Xceed.Wpf.Toolkit;

namespace QDMSServer
{
    public static class DbBackup
    {
        public static void Backup(string connectionStringName, string database)
        {
            try
            {
                if (Properties.Settings.Default.databaseType == "MySql")
                {
                    BackupMySql(connectionStringName);
                }
                else
                {
                    BackupSqlServer(connectionStringName, database);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while creating backup: " + ex.Message);
            }
        }

        public static void Restore(string connectionStringName, string database)
        {
            try
            {
                if (Properties.Settings.Default.databaseType == "MySql")
                {
                    RestoreMySql(connectionStringName);
                }
                else
                {
                    RestoreSqlServer(connectionStringName, database);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while restoring backup: " + ex.Message);
            }
        }

        private static void RestoreMySql(string connectionStringName)
        {
            string path;
            System.Windows.Forms.OpenFileDialog file = new System.Windows.Forms.OpenFileDialog
            {
                DefaultExt = ".sql",
                Filter = @"SQL Scripts (.sql)|*.sql"
            };

            if (file.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                path = file.FileName;
            }
            else
            {
                return;
            }

            MySQLBackup.Restore(path, ConfigurationManager.ConnectionStrings[connectionStringName].ToString());
        }

        private static void RestoreSqlServer(string connectionStringName, string database)
        {
            string path;
            System.Windows.Forms.OpenFileDialog file = new System.Windows.Forms.OpenFileDialog
            {
                DefaultExt = ".bak",
                Filter = @"SQL Server Backups (.bak)|*.bak"
            };

            if (file.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                path = file.FileName;
            }
            else
            {
                return;
            }

            SqlServerBackup.Restore(ConfigurationManager.ConnectionStrings[connectionStringName].ToString(), database, path);
        }

        private static void BackupMySql(string connectionStringName)
        {
            string path;
            System.Windows.Forms.SaveFileDialog file = new System.Windows.Forms.SaveFileDialog
            {
                FileName = "qdms.sql",
                DefaultExt = ".sql",
                Filter = @"SQL Scripts (.sql)|*.sql"
            };

            if (file.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                path = file.FileName;
            }
            else
            {
                return;
            }

            MySQLBackup.Backup(path, ConfigurationManager.ConnectionStrings[connectionStringName].ToString());
        }

        private static void BackupSqlServer(string connectionStringName, string database)
        {
            string path;
            System.Windows.Forms.SaveFileDialog file = new System.Windows.Forms.SaveFileDialog
            {
                FileName = "qdms.bak",
                DefaultExt = ".bak",
                Filter = @"SQL Server Backups (.bak)|*.bak"
            };

            if (file.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                path = file.FileName;
            }
            else
            {
                return;
            }

            SqlServerBackup.Backup(ConfigurationManager.ConnectionStrings[connectionStringName].ToString(), database, path);
        }
    }
}
