// -----------------------------------------------------------------------
// <copyright file="HistoricalRequestWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
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

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

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
            StartDate = new DateTime(1950, 1, 1, 0, 0, 0, 0);
            EndDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, 0);

            if (!TheInstrument.ID.HasValue) return;

            Show();
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

                    //find largest significant decimal by sampling the prices at the start and end of the series
                    var decPlaces = new List<int>();
                    for (int i = 0; i < Math.Min(20, e.Data.Count); i++)
                    {
                        decPlaces.Add(e.Data[i].Open.CountDecimalPlaces());
                        decPlaces.Add(e.Data[e.Data.Count - 1 - i].Close.CountDecimalPlaces());
                    }

                    //set the column format to use that number so we don't get any useless trailing 0s
                    if(decPlaces.Count > 0)
                        SetPriceColumnFormat(decPlaces.Max());


                    foreach (OHLCBar bar in e.Data)
                    {
                        Data.Add(bar);
                    }
                });
        }

        private void LoadDataBtn_Click(object sender, RoutedEventArgs e)
        {
            Data.Clear();

            DateTime start = StartDate + StartTime.TimeOfDay;
            DateTime end = EndDate + EndTime.TimeOfDay;

            _client.RequestHistoricalData(new HistoricalDataRequest(
                TheInstrument,
                (BarSize)ResolutionComboBox.SelectedItem,
                start,
                end,
                dataLocation: (DataLocation)DataLocationComboBox.SelectedItem,
                saveToLocalStorage: SaveToLocalStorageCheckBox.IsChecked ?? true,
                rthOnly: RTHOnlyCheckBox.IsChecked ?? true));
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

        /// <summary>
        /// Sets the formatting for the price columns to a given number of decimal places.
        /// </summary>
        private void SetPriceColumnFormat(int decimalPlaces)
        {
            //need to re-do the binding because simply changing the StringFormat results in a crash
            OpenCol.Binding = new Binding("Open") { StringFormat = "{0:F" + decimalPlaces + "}" };
            HighCol.Binding = new Binding("High") { StringFormat = "{0:F" + decimalPlaces + "}" };
            LowCol.Binding = new Binding("Low") { StringFormat = "{0:F" + decimalPlaces + "}" };
            CloseCol.Binding = new Binding("Close") { StringFormat = "{0:F" + decimalPlaces + "}" };

            AdjOpenCol.Binding = new Binding("AdjOpen") { StringFormat = "{0:F" + decimalPlaces + "}" };
            AdjHighCol.Binding = new Binding("AdjHigh") { StringFormat = "{0:F" + decimalPlaces + "}" };
            AdjLowCol.Binding = new Binding("AdjLow") { StringFormat = "{0:F" + decimalPlaces + "}" };
            AdjCloseCol.Binding = new Binding("AdjClose") { StringFormat = "{0:F" + decimalPlaces + "}" };
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
            _client.Dispose();
        }
    }
}
