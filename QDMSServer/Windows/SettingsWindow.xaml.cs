// -----------------------------------------------------------------------
// <copyright file="SettingsWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Windows;
using MahApps.Metro.Controls;
using NLog;
using NLog.Targets;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();

            //ports for the servers
            RTDPubPortTextBox.Text = Properties.Settings.Default.rtDBPubPort.ToString();
            RTDReqPortTextBox.Text = Properties.Settings.Default.rtDBReqPort.ToString();
            HDPortTextBox.Text = Properties.Settings.Default.hDBPort.ToString();
            InstrumentServerPortTextBox.Text = Properties.Settings.Default.instrumentServerPort.ToString();

            //logs
            LogFolderTextBox.Text = Properties.Settings.Default.logDirectory;

            //IB Settings
            IBHostTextBox.Text = Properties.Settings.Default.ibClientHost;
            IBPortTextBox.Text = Properties.Settings.Default.ibClientPort.ToString();
            IBClientIDTextBox.Text = Properties.Settings.Default.ibClientID.ToString();

            //Quandl
            QuandlAPITokenTextBox.Text = Properties.Settings.Default.quandlAuthCode;

            //Database
            if (Properties.Settings.Default.databaseType == "MySql")
            {
                DbTypeMySql.IsChecked = true;
                DbTypeSqlServer.IsChecked = false;
            }
            else
            {
                DbTypeMySql.IsChecked = false;
                DbTypeSqlServer.IsChecked = true;
            }

            MySqlHost.Text = Properties.Settings.Default.mySqlHost;
            MySqlUsername.Text = Properties.Settings.Default.mySqlUsername;
            MySqlPassword.Password = DBUtils.Unprotect(Properties.Settings.Default.mySqlPassword);

            SqlServerAuthenticationWindowsRadioBtn.IsChecked = Properties.Settings.Default.sqlServerUseWindowsAuthentication;
            SqlServerAuthenticationServerRadioBtn.IsChecked = !Properties.Settings.Default.sqlServerUseWindowsAuthentication;
            SqlServerHost.Text = Properties.Settings.Default.sqlServerHost;
            SqlServerUsername.Text = Properties.Settings.Default.sqlServerUsername;
            SqlServerPassword.Password = DBUtils.Unprotect(Properties.Settings.Default.sqlServerPassword);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            //ports settings
            int rtDBPubPort;
            if (int.TryParse(RTDPubPortTextBox.Text, out rtDBPubPort))
            {
                Properties.Settings.Default.rtDBPubPort = rtDBPubPort;
            }

            int rtDBReqPort;
            if (int.TryParse(RTDReqPortTextBox.Text, out rtDBReqPort))
            {
                Properties.Settings.Default.rtDBReqPort = rtDBReqPort;
            }

            int hDBPort;
            if (int.TryParse(HDPortTextBox.Text, out hDBPort))
            {
                Properties.Settings.Default.hDBPort = hDBPort;
            }

            int instrumentServerPort;
            if (int.TryParse(InstrumentServerPortTextBox.Text, out instrumentServerPort))
            {
                Properties.Settings.Default.instrumentServerPort = instrumentServerPort;
            }

            //logging settings...create folder if necessary
            try
            {
                if (!Directory.Exists(LogFolderTextBox.Text))
                {
                    Directory.CreateDirectory(LogFolderTextBox.Text);
                }
                Properties.Settings.Default.logDirectory = LogFolderTextBox.Text;
                ((FileTarget)LogManager.Configuration.FindTargetByName("default")).FileName = LogFolderTextBox.Text + "Log.log";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting log directory: " + ex.Message);
            }


            //IB Settings
            Properties.Settings.Default.ibClientHost = IBHostTextBox.Text;

            int ibPort;
            if (int.TryParse(IBPortTextBox.Text, out ibPort))
            {
                Properties.Settings.Default.ibClientPort = ibPort;
            }

            int ibClientID;
            if (int.TryParse(IBClientIDTextBox.Text, out ibClientID))
            {
                Properties.Settings.Default.ibClientID = ibClientID;
            }

            //Quandl
            Properties.Settings.Default.quandlAuthCode = QuandlAPITokenTextBox.Text;

            //Database
            if (DbTypeMySql.IsChecked.HasValue && DbTypeMySql.IsChecked.Value)
            {
                Properties.Settings.Default.databaseType = "MySql";
            }
            else
            {
                Properties.Settings.Default.databaseType = "SqlServer";
            }

            Properties.Settings.Default.mySqlHost = MySqlHost.Text;
            Properties.Settings.Default.mySqlUsername = MySqlUsername.Text;
            Properties.Settings.Default.mySqlPassword = DBUtils.Protect(MySqlPassword.Password);

            if (SqlServerAuthenticationWindowsRadioBtn.IsChecked != null)
                Properties.Settings.Default.sqlServerUseWindowsAuthentication = SqlServerAuthenticationWindowsRadioBtn.IsChecked.Value;

            Properties.Settings.Default.sqlServerHost = SqlServerHost.Text;
            Properties.Settings.Default.sqlServerUsername = SqlServerUsername.Text;
            Properties.Settings.Default.sqlServerPassword = DBUtils.Protect(SqlServerPassword.Password);

            Properties.Settings.Default.Save();

            Close();
        }
    }
}
