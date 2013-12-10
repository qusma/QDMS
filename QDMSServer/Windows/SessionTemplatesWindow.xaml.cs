// -----------------------------------------------------------------------
// <copyright file="SessionTemplatesWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using EntityData;
using MahApps.Metro.Controls;
using QDMS;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for SessionTemplatesWindow.xaml
    /// </summary>
    public partial class SessionTemplatesWindow : MetroWindow
    {
        public ObservableCollection<SessionTemplate> Templates { get; set; }

        public SessionTemplatesWindow()
        {
            InitializeComponent();
            DataContext = this;

            Templates = new ObservableCollection<SessionTemplate>();

            using (var context = new MyDBContext())
            {
                var templates = context.SessionTemplates.Include("Sessions").ToList().OrderBy(x => x.Name);
                foreach (SessionTemplate s in templates)
                {
                    Templates.Add(s);
                }
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditSessionTemplateWindow(null);
            window.ShowDialog();

            if (window.TemplateAdded)
            {
                using (var entityContext = new MyDBContext())
                {
                    Templates.Add(entityContext.SessionTemplates.Include("Sessions").First(x => x.Name == window.TheTemplate.Name));
                }
            }
        }

        private void ModifyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesGrid.SelectedItems.Count == 0) return;

            var window = new EditSessionTemplateWindow((SessionTemplate)TemplatesGrid.SelectedItem);
            window.ShowDialog();
            CollectionViewSource.GetDefaultView(TemplatesGrid.ItemsSource).Refresh();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedTemplate = (SessionTemplate)TemplatesGrid.SelectedItem;
            if (selectedTemplate == null) return;

            using (var context = new MyDBContext())
            {
                var instrumentCount = context.Instruments.Count(x => x.SessionTemplateID == selectedTemplate.ID && x.SessionsSource == SessionsSource.Template);
                if (instrumentCount > 0)
                {
                    MessageBox.Show(string.Format("Can't delete this template it has {0} instruments assigned to it.", instrumentCount));
                    return;
                }
            }

            var result = MessageBox.Show(string.Format("Are you sure you want to delete {0}?", selectedTemplate.Name), "Delete", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            using (var entityContext = new MyDBContext())
            {
                entityContext.SessionTemplates.Attach(selectedTemplate);
                entityContext.SessionTemplates.Remove(selectedTemplate);
                entityContext.SaveChanges();
            }

            Templates.Remove(selectedTemplate);
            CollectionViewSource.GetDefaultView(TemplatesGrid.ItemsSource).Refresh();
        }

        private void TableView_RowDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var window = new EditSessionTemplateWindow((SessionTemplate)TemplatesGrid.SelectedItem);
            window.ShowDialog();
            CollectionViewSource.GetDefaultView(TemplatesGrid.ItemsSource).Refresh();
        }
    }
}
