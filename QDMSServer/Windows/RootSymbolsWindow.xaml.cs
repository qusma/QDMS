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
    public partial class RootSymbolsWindow : MetroWindow
    {
        public ObservableCollection<UnderlyingSymbol> Symbols { get; set; }

        public RootSymbolsWindow()
        {
            InitializeComponent();
            DataContext = this;

            Symbols = new ObservableCollection<UnderlyingSymbol>();

            using (var context = new MyDBContext())
            {
                var templates = context.UnderlyingSymbols.OrderBy(x => x.Symbol);
                foreach (UnderlyingSymbol s in templates)
                {
                    Symbols.Add(s);
                }
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditRootSymbolWindow(null);
            window.ShowDialog();

            if (window.SymbolAdded)
            {
                using (var entityContext = new MyDBContext())
                {
                    Symbols.Add(entityContext.UnderlyingSymbols.First(x => x.Symbol == window.TheSymbol.Symbol));
                }
            }
        }

        private void ModifyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SymbolsGrid.SelectedItems.Count == 0) return;

            var window = new EditSessionTemplateWindow((SessionTemplate)SymbolsGrid.SelectedItem);
            window.ShowDialog();
            CollectionViewSource.GetDefaultView(SymbolsGrid.ItemsSource).Refresh();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedSymbol = (UnderlyingSymbol)SymbolsGrid.SelectedItem;
            if (selectedSymbol == null) return;

            using (var context = new MyDBContext())
            {
                var instrumentCount = context.Instruments.Count(x => x.SessionTemplateID == selectedSymbol.ID && x.SessionsSource == SessionsSource.Template);
                if (instrumentCount > 0)
                {
                    MessageBox.Show(string.Format("Can't delete this template it has {0} instruments assigned to it.", instrumentCount));
                    return;
                }
            }

            var result = MessageBox.Show(string.Format("Are you sure you want to delete {0}?", selectedSymbol.Symbol), "Delete", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            using (var entityContext = new MyDBContext())
            {
                entityContext.UnderlyingSymbols.Attach(selectedSymbol);
                entityContext.UnderlyingSymbols.Remove(selectedSymbol);
                entityContext.SaveChanges();
            }

            Symbols.Remove(selectedSymbol);
            CollectionViewSource.GetDefaultView(SymbolsGrid.ItemsSource).Refresh();
        }

        private void TableView_RowDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var window = new EditRootSymbolWindow((UnderlyingSymbol)SymbolsGrid.SelectedItem);
            window.ShowDialog();
            CollectionViewSource.GetDefaultView(SymbolsGrid.ItemsSource).Refresh();
        }
    }
}
