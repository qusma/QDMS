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

            Properties.Settings.Default.Save();

            Close();
        }
    }
}
