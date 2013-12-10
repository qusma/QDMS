// -----------------------------------------------------------------------
// <copyright file="HistoricalRequestWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Windows;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using QDMS;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for DataEditWindow.xaml
    /// </summary>
    public partial class HistoricalRequestWindow : MetroWindow
    {
        public Instrument TheInstrument { get; set; }
        public ObservableCollection<OHLCBar> Data { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        private QDMSClient.QDMSClient _client;

        public HistoricalRequestWindow(Instrument instrument)
        {
            InitializeComponent();
            DataContext = this;

            Random r = new Random(); //we have to randomize the name of the client, can't reuse the identity

            Title = string.Format("Data Request - {0} @ {1}", instrument.Symbol, instrument.Datasource.Name);

            _client = new QDMSClient.QDMSClient(
            string.Format("DataRequestClient-{0}", r.Next()),
            "localhost",
            Properties.Settings.Default.rtDBReqPort,
            Properties.Settings.Default.rtDBPubPort,
            Properties.Settings.Default.instrumentServerPort,
            Properties.Settings.Default.hDBPort);
            
            _client.HistoricalDataReceived += _client_HistoricalDataReceived;
            _client.Error += _client_Error;
            _client.Connect();

            Data = new ObservableCollection<OHLCBar>();

            TheInstrument = instrument;

            StartTime = new DateTime(1950, 1, 1, 0, 0, 0, 0);
            EndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, 0);

            if (!TheInstrument.ID.HasValue) return;

            ShowDialog();
        }

        void _client_Error(object sender, ErrorArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusLabel.Content = e.ErrorMessage;
                });
        }

        void _client_HistoricalDataReceived(object sender, HistoricalDataEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusLabel.Content = string.Format("Loaded {0} Bars", e.Data.Count);
                    foreach (OHLCBar bar in e.Data)
                    {
                        Data.Add(bar);
                    }
                });
        }

        private void LoadDataBtn_Click(object sender, RoutedEventArgs e)
        {
            Data.Clear();

            _client.RequestHistoricalData(new HistoricalDataRequest(
                TheInstrument,
                (BarSize)ResolutionComboBox.SelectedItem,
                StartTime,
                EndTime,
                ForceFreshDataCheckBox.IsChecked ?? false,
                LocalStorageOnlyCheckBox.IsChecked ?? false,
                SaveToLocalStorageCheckBox.IsChecked ?? true,
                RTHOnlyCheckBox.IsChecked ?? true));
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = @"CSV File (*.csv)|*.csv",
                FileName = string.Format("{0} {1} {2:ddMMyyyy}-{3:ddMMyyyy}", TheInstrument.Symbol, (BarSize)ResolutionComboBox.SelectedItem, DateTime.Now, DateTime.Now)
            };
            var result = dialog.ShowDialog();
            if (result.Value)
            {
                var filePath = dialog.FileName;
                Data.ToCSVFile(filePath);
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _client.HistoricalDataReceived -= _client_HistoricalDataReceived;
            _client.Error -= _client_Error;
            _client.Disconnect();
        }
    }
}
