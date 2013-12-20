// -----------------------------------------------------------------------
// <copyright file="AddInstrumentQuandlWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EntityData;
using MahApps.Metro.Controls;
using NLog;
using QDMS;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for AddInstrumentQuandlWindow.xaml
    /// </summary>
    public partial class AddInstrumentQuandlWindow : MetroWindow
    {
        public ObservableCollection<Exchange> Exchanges { get; set; }
        public ObservableCollection<Instrument> Instruments { get; set; }
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public List<Instrument> AddedInstruments { get; set; }

        private readonly Datasource _thisDS;

        public AddInstrumentQuandlWindow()
        {
            DataContext = this;

            AddedInstruments = new List<Instrument>();
            Exchanges = new ObservableCollection<Exchange>();

            Instruments = new ObservableCollection<Instrument>();

            InitializeComponent();

            ExchangeComboBox.ItemsSource = Exchanges;
            PrimaryExchangeComboBox.ItemsSource = Exchanges;

            using (var entityContext = new MyDBContext())
            {
                _thisDS = entityContext.Datasources.First(x => x.Name == "Quandl");
                foreach (Exchange e in entityContext.Exchanges.AsEnumerable())
                {
                    Exchanges.Add(e);
                }
            }

            

            ShowDialog();
        }

        private void Search(int page = 1)
        {
            Instruments.Clear();
            int count = 0;
            var foundInstruments = QuandlUtils.FindInstruments(SymbolTextBox.Text, out count, page);
            foreach (var i in foundInstruments)
            {
                i.Datasource = _thisDS;
                i.DatasourceID = _thisDS.ID;
                i.Multiplier = 1;
                Instruments.Add(i);
            }

            StatusLabel.Content = count + " contracts found";

            CurrentPageTextBox.Text = page.ToString();
        }

        private void SymbolTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Search();
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            foreach (Instrument newInstrument in InstrumentGrid.SelectedItems)
            {
                if (newInstrument.Exchange != null)
                    newInstrument.ExchangeID = newInstrument.Exchange.ID;
                if (newInstrument.PrimaryExchange != null)
                    newInstrument.PrimaryExchangeID = newInstrument.PrimaryExchange.ID;

                try
                {
                    if (InstrumentManager.AddInstrument(newInstrument))
                        count++;
                    AddedInstruments.Add(newInstrument);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
            StatusLabel.Content = string.Format("{0}/{1} instruments added.", count, InstrumentGrid.SelectedItems.Count);
        }

        private void PageForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            int currentPage;
            bool parsed = int.TryParse(CurrentPageTextBox.Text, out currentPage);
            if (parsed)
            {
                currentPage++;
                Search(currentPage);
            }
        }

        private void PageBackBtn_OnClick(object sender, RoutedEventArgs e)
        {
            int currentPage;
            bool parsed = int.TryParse(CurrentPageTextBox.Text, out currentPage);
            if (parsed && currentPage > 1)
            {
                currentPage++;
                Search(currentPage);
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int currentPage;
                bool parsed = int.TryParse(CurrentPageTextBox.Text, out currentPage);
                if (parsed)
                {
                    currentPage++;
                    Search(currentPage);
                }
            }
        }
    }
}
