// -----------------------------------------------------------------------
// <copyright file="TagsWindow.xaml.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Reactive.Linq;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMSServer.ViewModels;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for TagsWindow.xaml
    /// </summary>
    public partial class TagsWindow : MetroWindow
    {
        public TagsViewModel ViewModel { get; set; }

        public TagsWindow(IDataClient client)
        {
            InitializeComponent();
            ViewModel = new TagsViewModel(client, DialogCoordinator.Instance);
            DataContext = ViewModel;
        }

        private async void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await ViewModel.LoadTags.Execute();
        }
    }
}
