// -----------------------------------------------------------------------
// <copyright file="SessionTemplatesWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;
using QDMSServer.ViewModels;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for SessionTemplatesWindow.xaml
    /// </summary>
    public partial class ScheduledJobsWindow : MetroWindow
    {
        public ScheduledJobsWindow(SchedulerViewModel viewModel)
        {
            InitializeComponent();
            viewModel.RefreshCollections();
            DataContext = viewModel;
        }
    }
}