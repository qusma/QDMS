// -----------------------------------------------------------------------
// <copyright file="AddInstrumentQuandlWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMSApp.ViewModels;
using System.Reactive.Linq;
using System.Windows;

namespace QDMSApp
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