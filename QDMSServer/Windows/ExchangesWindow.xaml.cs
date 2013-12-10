// -----------------------------------------------------------------------
// <copyright file="ExchangesWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using EntityData;
using MahApps.Metro.Controls;
using QDMS;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for ExchangesWindow.xaml
    /// </summary>
    public partial class ExchangesWindow : MetroWindow
    {
        public ObservableCollection<Exchange> Exchanges { get; set; }
        private Timer _filterTimer;


        public ExchangesWindow()
        {
            InitializeComponent();
            DataContext = this;

            Exchanges = new ObservableCollection<Exchange>();

            List<Exchange> tmpExchanges;
            using (var entityContext = new MyDBContext())
            {
                tmpExchanges = entityContext.Exchanges.Include("Sessions").OrderBy(x => x.Name).ToList();
            }
            foreach (Exchange e in tmpExchanges)
            {
                Exchanges.Add(e);
            }

            ExchangesGrid.ItemsSource = Exchanges;

            _filterTimer = new Timer();
            _filterTimer.Enabled = false;
            _filterTimer.AutoReset = false;
            _filterTimer.Interval = 100; //milliseconds
            _filterTimer.Elapsed += _filterTimer_Elapsed;
        }

        //delay the search by a bit so that we don't trigger it multiple times when writing a single word
        void _filterTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(ApplyFilter);
        }

        private void ApplyFilter()
        {
            var viewSource = CollectionViewSource.GetDefaultView(ExchangesGrid.ItemsSource);
            if (SearchBox.Text == "")
            {
                //no filter
                viewSource.Filter = null;
            }
            else
            {
                string search = SearchBox.Text.ToLower();
                viewSource.Filter = new Predicate<object>(x =>
                    ((Exchange)x).Name.ToLower().Contains(search)
                    || (((Exchange)x).LongName != null && ((Exchange)x).LongName.ToLower().Contains(search)));
            }
        }

        private void TableView_RowDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var window = new EditExchangeWindow((Exchange)ExchangesGrid.SelectedItem);
            window.ShowDialog();
            CollectionViewSource.GetDefaultView(ExchangesGrid.ItemsSource).Refresh();
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new EditExchangeWindow(null);
            window.ShowDialog();
            
            if (window.ExchangeAdded)
            {
                using (var entityContext = new MyDBContext())
                {
                    Exchanges.Add(entityContext.Exchanges.First(x => x.Name == window.TheExchange.Name));
                }
            }
        }

        private void ModifyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ExchangesGrid.SelectedItems.Count == 0) return;

            var window = new EditExchangeWindow((Exchange)ExchangesGrid.SelectedItem);
            window.ShowDialog();
            CollectionViewSource.GetDefaultView(ExchangesGrid.ItemsSource).Refresh();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedExchange = (Exchange)ExchangesGrid.SelectedItem;
            if(selectedExchange == null) return;

            using (var context = new MyDBContext())
            {
                var instrumentCount = context.Instruments.Count(x => x.ExchangeID == selectedExchange.ID);
                if (instrumentCount > 0)
                {
                    MessageBox.Show(string.Format("Can't delete this exchange it has {0} instruments assigned to it.", instrumentCount));
                    return;
                }
            }

            var result = MessageBox.Show(string.Format("Are you sure you want to delete {0}?", selectedExchange.Name), "Delete", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            using (var entityContext = new MyDBContext())
            {
                entityContext.Exchanges.Attach(selectedExchange);
                entityContext.Exchanges.Remove(selectedExchange);
                entityContext.SaveChanges();
            }

            Exchanges.Remove(selectedExchange);
            CollectionViewSource.GetDefaultView(ExchangesGrid.ItemsSource).Refresh();
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            _filterTimer.Start();
        }
    }
}
