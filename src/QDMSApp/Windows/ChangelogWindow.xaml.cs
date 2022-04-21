// -----------------------------------------------------------------------
// <copyright file="ChangelogWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;

namespace QDMSApp
{
    /// <summary>
    /// Interaction logic for ChangelogWindow.xaml
    /// </summary>
    public partial class ChangelogWindow : MetroWindow
    {
        public ChangelogWindow()
        {
            InitializeComponent();
            ChangelogText.Text = Properties.Resources.CHANGELOG;
        }
    }
}
