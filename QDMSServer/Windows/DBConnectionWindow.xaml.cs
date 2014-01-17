// -----------------------------------------------------------------------
// <copyright file="DBConnectionWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Windows;
using MahApps.Metro.Controls;
using MySql.Data.MySqlClient;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for DBConnectionWindow.xaml
    /// </summary>
    public partial class DBConnectionWindow : MetroWindow
    {
        public DBConnectionWindow()
        {
            InitializeComponent();
            HostTextBox.Text = Properties.Settings.Default.dbHost;
            UsernameTextBox.Text = Properties.Settings.Default.dbUsername;
            PasswordTextBox.Password = "asdf";
        }

        private void TestConnectionBtn_Click(object sender, RoutedEventArgs e)
        {
            MySqlConnection connection = DBUtils.CreateMySqlConnection(server: HostTextBox.Text, username: UsernameTextBox.Text, password: PasswordTextBox.Password);
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed with error: " + ex.Message);
                return;
            }
            
            MessageBox.Show("Connection succeeded.");
            connection.Close();
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            MySqlConnection connection = DBUtils.CreateMySqlConnection(server: HostTextBox.Text, username: UsernameTextBox.Text, password: PasswordTextBox.Password);
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed with error: " + ex.Message);
                return;
            }
            connection.Close();

            Properties.Settings.Default.dbHost = HostTextBox.Text;
            Properties.Settings.Default.dbUsername = UsernameTextBox.Text;
            Properties.Settings.Default.dbPassword = DBUtils.Protect(PasswordTextBox.Password);

            Properties.Settings.Default.Save();

            Close();
        }
    }
}
