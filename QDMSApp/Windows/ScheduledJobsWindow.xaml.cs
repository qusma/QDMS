﻿// -----------------------------------------------------------------------
// <copyright file="SessionTemplatesWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMSApp.ViewModels;
using System.Reactive.Linq;

namespace QDMSApp
{
    /// <summary>
    /// Interaction logic for SessionTemplatesWindow.xaml
    /// </summary>
    public partial class ScheduledJobsWindow : MetroWindow
    {
        public SchedulerViewModel ViewModel { get; }

        public ScheduledJobsWindow(IDataClient client)
        {
            InitializeComponent();
            ViewModel = new SchedulerViewModel(client, DialogCoordinator.Instance);
            DataContext = ViewModel;
        }

        private async void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await ViewModel.Load.Execute();
        }
    }
}