// -----------------------------------------------------------------------
// <copyright file="AddInstrumentQuandlWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using MahApps.Metro.Controls;
using NLog;
using QDMS;
using QDMS.Server;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;
using QDMSServer.ViewModels;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for AddInstrumentQuandlWindow.xaml
    /// </summary>
    public partial class AddInstrumentQuandlWindow : MetroWindow
    {
        public AddInstrumentQuandlViewModel ViewModel { get; set; }
        public AddInstrumentQuandlWindow(IDataClient client)
        {
            InitializeComponent();

            ViewModel = new AddInstrumentQuandlViewModel(client, DialogCoordinator.Instance, Properties.Settings.Default.quandlAuthCode);
            DataContext = ViewModel;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.Load.Execute();
        }
    }
}