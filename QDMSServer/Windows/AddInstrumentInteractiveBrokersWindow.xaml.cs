// -----------------------------------------------------------------------
// <copyright file="AddInstrumentInteractiveBrokersWindow.xaml.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using QDMSServer.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for AddInstrumentInteractiveBrokersWindow.xaml
    /// </summary>
    public partial class AddInstrumentInteractiveBrokersWindow : MetroWindow
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public AddInstrumentInteractiveBrokersWindow()
        {
            try
            {
                ViewModel = new AddInstrumentIbViewModel(DialogCoordinator.Instance);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message);
                Close();
                return;
            }

            DataContext = ViewModel;

            InitializeComponent();

            ShowDialog();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void DXWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.Dispose();
            }
        }

        private void SymbolTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ViewModel.Search.Execute(null);
        }

        public AddInstrumentIbViewModel ViewModel { get; set; }
    }
}