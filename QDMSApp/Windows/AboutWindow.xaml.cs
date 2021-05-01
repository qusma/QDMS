// -----------------------------------------------------------------------
// <copyright file="AboutWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using MahApps.Metro.Controls;

namespace QDMSApp
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : MetroWindow
    {
        public AboutWindow()
        {
            InitializeComponent();

            VersionLabel.Content = string.Format("Version: {0}", 
                GetVersion());
        }

        private void CloseBtn_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private string GetVersion()
        {
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                return System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }

            return "debug";
        }
    }
}
