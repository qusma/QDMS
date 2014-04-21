// -----------------------------------------------------------------------
// <copyright file="AddInstrumentInteractiveBrokersWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EntityData;
using Krs.Ats.IBNet;
using MahApps.Metro.Controls;
using NLog;
using QDMS;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for AddInstrumentInteractiveBrokersWindow.xaml
    /// </summary>
    public partial class AddInstrumentInteractiveBrokersWindow : MetroWindow
    {
        public ObservableCollection<KeyValuePair<int, string>> Exchanges { get; set; }

        public ObservableCollection<InstrumentType> InstrumentTypes { get; set; }

        public ObservableCollection<Instrument> Instruments { get; set; }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public List<Instrument> AddedInstruments { get; set; }

        private readonly IBClient _client;
        private int _nextRequestID;
        private readonly Datasource _thisDS;
        private readonly Dictionary<string, Exchange> _exchanges;

        public AddInstrumentInteractiveBrokersWindow()
        {
            Random r = new Random();
            _client = new IBClient();

            try
            {
                //random connection id for this one...
                _client.Connect(Properties.Settings.Default.ibClientHost, Properties.Settings.Default.ibClientPort, r.Next(1000, 200000));
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not connect to TWS: " + e.Message, "Error");
                Close();
                return;
            }

            AddedInstruments = new List<Instrument>();

            _client.NextValidId += _client_NextValidId;
            _client.ContractDetails += _client_ContractDetails;
            _client.Error += _client_Error;
            _client.ConnectionClosed += _client_ConnectionClosed;
            _client.ContractDetailsEnd += _client_ContractDetailsEnd;

            Exchanges = new ObservableCollection<KeyValuePair<int, string>> { new KeyValuePair<int, string>(0, "All") };
            _exchanges = new Dictionary<string, Exchange>();

            using (var context = new MyDBContext())
            {
                _thisDS = context.Datasources.First(x => x.Name == "Interactive Brokers");

                foreach (Exchange e in context.Exchanges)
                {
                    Exchanges.Add(new KeyValuePair<int, string>(e.ID, e.Name));
                    _exchanges.Add(e.Name, e);
                }
            }

            InitializeComponent();
            DataContext = this;

            Instruments = new ObservableCollection<Instrument>();
            InstrumentTypes = new ObservableCollection<InstrumentType>();

            //list the available types from our enum
            var values = MyUtils.GetEnumValues<InstrumentType>();
            foreach (var val in values)
            {
                InstrumentTypes.Add(val);
            }

            ShowDialog();
        }

        private void _client_ContractDetailsEnd(object sender, ContractDetailsEndEventArgs e)
        {
            Dispatcher.Invoke(() => StatusLabel.Content = Instruments.Count + " contracts arrived");
        }

        private void _client_ConnectionClosed(object sender, ConnectionClosedEventArgs e)
        {
            Dispatcher.Invoke(() => _logger.Log(NLog.LogLevel.Error, string.Format("Instrument Adder connection closed.")));
        }

        private void _client_Error(object sender, ErrorEventArgs e)
        {
            if (e.ErrorMsg == "No security definition has been found for the request")
                Dispatcher.Invoke(() => StatusLabel.Content = e.ErrorMsg);
            else
                Dispatcher.Invoke(() => _logger.Log(NLog.LogLevel.Error, string.Format("{0} - {1}", e.ErrorCode, e.ErrorMsg)));
        }

        private void _client_ContractDetails(object sender, ContractDetailsEventArgs e)
        {
            var instrument = TWSUtils.ContractDetailsToInstrument(e.ContractDetails);
            instrument.Datasource = _thisDS;
            instrument.DatasourceID = _thisDS.ID;
            if (e.ContractDetails.Summary.Exchange != null && _exchanges.ContainsKey(e.ContractDetails.Summary.Exchange))
            {
                instrument.Exchange = _exchanges[e.ContractDetails.Summary.Exchange];
                instrument.ExchangeID = instrument.Exchange.ID;
            }
            else
            {
                Dispatcher.Invoke(() => _logger.Log(NLog.LogLevel.Error, "Could not find exchange in database: " + e.ContractDetails.Summary.Exchange));
                return;
            }

            if (e.ContractDetails.Summary.PrimaryExchange != null && _exchanges.ContainsKey(e.ContractDetails.Summary.PrimaryExchange))
            {
                instrument.PrimaryExchange = _exchanges[e.ContractDetails.Summary.PrimaryExchange];
                instrument.PrimaryExchangeID = instrument.PrimaryExchange.ID;
            }
            else if (!string.IsNullOrEmpty(e.ContractDetails.Summary.PrimaryExchange))
            {
                Dispatcher.Invoke(() => _logger.Log(NLog.LogLevel.Error, "Could not find exchange in database: " + e.ContractDetails.Summary.PrimaryExchange));
                return;
            }

            Dispatcher.Invoke(() => Instruments.Add(instrument));
        }

        private void _client_NextValidId(object sender, NextValidIdEventArgs e)
        {
            _nextRequestID = e.OrderId;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            _client.Disconnect();
            Hide();
        }

        private void DXWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _client.Dispose();
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            foreach (Instrument newInstrument in InstrumentGrid.SelectedItems)
            {
                try
                {
                    if (InstrumentManager.AddInstrument(newInstrument))
                    {
                        count++;
                        AddedInstruments.Add(newInstrument);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
            StatusLabel.Content = string.Format("{0}/{1} instruments added.", count, InstrumentGrid.SelectedItems.Count);
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void SymbolTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Search();
        }

        private void Search()
        {
            Instruments.Clear();
            var contract = new Contract
            {
                Symbol = SymbolTextBox.Text,
                SecurityType = TWSUtils.SecurityTypeConverter((InstrumentType)InstrumentTypeBox.SelectedItem),
                Exchange = ExchangeBox.Text == "All" ? "" : ExchangeBox.Text,
                IncludeExpired =
                    IncludeExpiredCheckBox.IsChecked != null && (bool)IncludeExpiredCheckBox.IsChecked
            };

            if (Expirationpicker.SelectedDate.HasValue)
                contract.Expiry = Expirationpicker.SelectedDate.Value.ToString("yyyyMM");

            if (StrikeTextBox.Text != "")
            {
                double strike;
                bool success = double.TryParse(StrikeTextBox.Text, out strike);
                if (success)
                    contract.Strike = strike;
            }

            _client.RequestContractDetails(_nextRequestID, contract);
        }
    }
}