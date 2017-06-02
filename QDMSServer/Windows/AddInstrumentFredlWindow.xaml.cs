// -----------------------------------------------------------------------
// <copyright file="AddInstrumentFredlWindow.xaml.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Reactive.Linq;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMSServer.ViewModels;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for AddInstrumentQuandlWindow.xaml
    /// </summary>
    public partial class AddInstrumentFredWindow : MetroWindow
    {
        public AddInstrumentFredWindow(IDataClient client)
        {
            InitializeComponent();

            ViewModel = new AddInstrumentFredViewModel(client, DialogCoordinator.Instance);
            DataContext = ViewModel;
        }

        public AddInstrumentFredViewModel ViewModel { get; set; }

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
