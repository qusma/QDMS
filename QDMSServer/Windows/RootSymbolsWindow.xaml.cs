// -----------------------------------------------------------------------
// <copyright file="RootSymbolsWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMSServer.ViewModels;
using System.Reactive.Linq;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for RootSymbolsWindow.xaml
    /// </summary>
    public partial class RootSymbolsWindow : MetroWindow
    {
        public UnderlyingSymbolsViewModel ViewModel { get; }

        public RootSymbolsWindow(IDataClient client)
        {
            InitializeComponent();
            ViewModel = new UnderlyingSymbolsViewModel(client, DialogCoordinator.Instance);
            DataContext = ViewModel;
        }

        private async void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await ViewModel.Load.Execute();
        }
    }
}