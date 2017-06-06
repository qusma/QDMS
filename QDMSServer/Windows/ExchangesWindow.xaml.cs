// -----------------------------------------------------------------------
// <copyright file="ExchangesWindow.xaml.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMSServer.ViewModels;
using ReactiveUI;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for ExchangesWindow.xaml
    /// </summary>
    public partial class ExchangesWindow : MetroWindow, IViewFor<ExchangesViewModel>
    {
        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (ExchangesViewModel)value;
        }

        public ExchangesViewModel ViewModel
        {
            get => (ExchangesViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(ExchangesViewModel), typeof(ExchangesWindow));

        public ExchangesWindow(IDataClient client)
        {
            InitializeComponent();
            ViewModel = new ExchangesViewModel(client, DialogCoordinator.Instance);
            DataContext = ViewModel;
        }

        private async void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await ViewModel.Load.Execute();
        }

        private void ExchangesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //scroll the selected exchange into view
            Selector selector = sender as Selector;
            DataGrid dataGrid = selector as DataGrid;
            if (dataGrid != null && selector.SelectedItem != null && dataGrid.SelectedIndex >= 0)
            {
                dataGrid.ScrollIntoView(selector.SelectedItem);
            }
        }
    }
}
