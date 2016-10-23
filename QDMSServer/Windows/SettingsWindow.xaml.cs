// -----------------------------------------------------------------------
// <copyright file="SettingsWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        public ObservableCollection<string> EconomicReleaseDataSources { get; set; } = new ObservableCollection<string>(); 
        public string SelectedDefaultEconomicReleaseDatasource { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();

            //Economic Releases
            EconomicReleaseDataSources.Add("FXStreet");
            SelectedDefaultEconomicReleaseDatasource = Properties.Settings.Default.EconomicReleaseDefaultDatasource;

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
            IBHistClientIDTextBox.Text = Properties.Settings.Default.histClientIBID.ToString();
            IBRTDClientIDTextBox.Text = Properties.Settings.Default.rtdClientIBID.ToString();

            //Quandl
            QuandlAPITokenTextBox.Text = Properties.Settings.Default.quandlAuthCode;

            //BarChart
            BarChartAPITokenTextBox.Text = Properties.Settings.Default.barChartApiKey;

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
            MySqlPassword.Password = EncryptionUtils.Unprotect(Properties.Settings.Default.mySqlPassword);

            SqlServerAuthenticationWindowsRadioBtn.IsChecked = Properties.Settings.Default.sqlServerUseWindowsAuthentication;
            SqlServerAuthenticationServerRadioBtn.IsChecked = !Properties.Settings.Default.sqlServerUseWindowsAuthentication;
            SqlServerHost.Text = Properties.Settings.Default.sqlServerHost;
            SqlServerUsername.Text = Properties.Settings.Default.sqlServerUsername;
            SqlServerPassword.Password = EncryptionUtils.Unprotect(Properties.Settings.Default.sqlServerPassword);
            
            //Data jobs
            UpdateJobEmailHost.Text = Properties.Settings.Default.updateJobEmailHost;
            UpdateJobEmailPort.Text = Properties.Settings.Default.updateJobEmailPort.ToString();
            UpdateJobEmailUsername.Text = Properties.Settings.Default.updateJobEmailUsername;
            UpdateJobEmailSender.Text = Properties.Settings.Default.updateJobEmailSender;
            UpdateJobEmail.Text = Properties.Settings.Default.updateJobEmail;
            UpdateJobEmailPassword.Password = EncryptionUtils.Unprotect(Properties.Settings.Default.updateJobEmailPassword);
            UpdateJobTimeout.Text = Properties.Settings.Default.updateJobTimeout.ToString();

            UpdateJobAbnormalities.IsChecked = Properties.Settings.Default.updateJobReportOutliers;
            UpdateJobTimeouts.IsChecked = Properties.Settings.Default.updateJobTimeouts;
            UpdateJobDatasourceErrors.IsChecked = Properties.Settings.Default.updateJobReportErrors;
            UpdateJobNoData.IsChecked = Properties.Settings.Default.updateJobReportNoData;

            DataContext = this;
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

            int ibHistClientID;
            if (int.TryParse(IBHistClientIDTextBox.Text, out ibHistClientID))
            {
                Properties.Settings.Default.histClientIBID = ibHistClientID;
            }

            int ibRTDClientID;
            if (int.TryParse(IBRTDClientIDTextBox.Text, out ibRTDClientID))
            {
                Properties.Settings.Default.rtdClientIBID = ibRTDClientID;
            }

            //Quandl
            Properties.Settings.Default.quandlAuthCode = QuandlAPITokenTextBox.Text;

            //BarChart
            Properties.Settings.Default.barChartApiKey = BarChartAPITokenTextBox.Text;

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
            Properties.Settings.Default.mySqlPassword = EncryptionUtils.Protect(MySqlPassword.Password);

            if (SqlServerAuthenticationWindowsRadioBtn.IsChecked != null)
                Properties.Settings.Default.sqlServerUseWindowsAuthentication = SqlServerAuthenticationWindowsRadioBtn.IsChecked.Value;

            Properties.Settings.Default.sqlServerHost = SqlServerHost.Text;
            Properties.Settings.Default.sqlServerUsername = SqlServerUsername.Text;
            Properties.Settings.Default.sqlServerPassword = EncryptionUtils.Protect(SqlServerPassword.Password);

            //Data jobs
            Properties.Settings.Default.updateJobEmailHost = UpdateJobEmailHost.Text;
            int port;
            if (int.TryParse(UpdateJobEmailPort.Text, out port))
            {
                Properties.Settings.Default.updateJobEmailPort = port;
            }

            Properties.Settings.Default.updateJobEmailUsername = UpdateJobEmailUsername.Text;
            Properties.Settings.Default.updateJobEmailSender = UpdateJobEmailSender.Text;
            Properties.Settings.Default.updateJobEmail = UpdateJobEmail.Text;
            Properties.Settings.Default.updateJobEmailPassword = EncryptionUtils.Protect(UpdateJobEmailPassword.Password);

            int timeout;
            if(int.TryParse(UpdateJobTimeout.Text, out timeout) && timeout > 0)
            {
                Properties.Settings.Default.updateJobTimeout = timeout;
            }

            Properties.Settings.Default.updateJobReportOutliers = UpdateJobAbnormalities.IsChecked.Value;
            Properties.Settings.Default.updateJobTimeouts = UpdateJobTimeouts.IsChecked.Value;
            Properties.Settings.Default.updateJobReportErrors = UpdateJobDatasourceErrors.IsChecked.Value;
            Properties.Settings.Default.updateJobReportNoData = UpdateJobNoData.IsChecked.Value;

            //Economic Releases
            Properties.Settings.Default.EconomicReleaseDefaultDatasource = SelectedDefaultEconomicReleaseDatasource;


            Properties.Settings.Default.Save();

            Close();
        }

        private void NotificationHelpBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(@"http://qusma.com/qpasdocs/index.php/Setting_Up_QDMS_Email_Notifications"));
        }
    }
}
