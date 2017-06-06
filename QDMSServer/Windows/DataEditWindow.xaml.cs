// -----------------------------------------------------------------------
// <copyright file="DataEditWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EntityData;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using QDMS;
using QDMS.Server;
using QDMS.Utils;
using QDMSServer.DataSources;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for DataEditWindow.xaml
    /// </summary>
    public partial class DataEditWindow : MetroWindow
    {
        public Instrument TheInstrument { get; set; }
        public ObservableCollection<OHLCBar> Data { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        private BarSize _loadedFrequency;
        private string _loadedTimeZone;
        private TimeZoneInfo _tzInfo;

        public DataEditWindow(Instrument instrument)
        {
            InitializeComponent();
            DataContext = this;

            Data = new ObservableCollection<OHLCBar>();

            //grab and update the instrument
            using (var context = new MyDBContext())
            {
                context.Instruments.Attach(instrument);
                context.Entry(instrument).Reload();
                TheInstrument = instrument;
            }

            string timezone = TheInstrument.Exchange == null ? "UTC" : TheInstrument.Exchange.Timezone;
            _tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);

            StartTime = new DateTime(1950, 1, 1, 0, 0, 0, 0);
            EndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, 0);

            if (!TheInstrument.ID.HasValue) return;

            //grab the data info
            using (var localStorage = DataStorageFactory.Get())
            {
                //TODO remove dependency on local storage here, use client instead
                var storageInfo = localStorage.GetStorageInfo(TheInstrument.ID.Value);

                if (storageInfo.Count == 0) //if it doesn't have any data, we just exit
                {
                    MessageBox.Show("This instrument has no data.");
                    Hide();
                }
                else
                {
                    foreach (StoredDataInfo s in storageInfo) //fill the resolution box
                    {
                        ResolutionComboBox.Items.Add(s.Frequency);
                    }
                }
            }

            
            
        }

        private void LoadDataBtn_Click(object sender, RoutedEventArgs e)
        {
            Data.Clear();

            //grab the data
            using (var localStorage = DataStorageFactory.Get())
            {
                
                var bars = localStorage.GetData(TheInstrument, StartTime, EndTime, (BarSize)ResolutionComboBox.SelectedItem);

                //find largest significant decimal by sampling the prices at the start and end of the series
                var decPlaces = new List<int>();
                for (int i = 0; i < Math.Min(bars.Count, 20); i++)
                {
                    decPlaces.Add(bars[i].Open.CountDecimalPlaces());
                    decPlaces.Add(bars[bars.Count - 1 - i].Close.CountDecimalPlaces());
                }

                //set the column format to use that number so we don't get any useless trailing 0s
                SetPriceColumnFormat(decPlaces.Max());

                foreach (OHLCBar b in bars)
                {
                    //do any required time zone coversions
                    if (TimezoneComboBox.Text == "UTC")
                    {
                        b.DT = TimeZoneInfo.ConvertTimeToUtc(b.DT, _tzInfo);
                    }
                    else if (TimezoneComboBox.Text == "Local")
                    {
                        b.DT = TimeZoneInfo.ConvertTime(b.DT, _tzInfo, TimeZoneInfo.Local);
                    }
                    Data.Add(b);
                }
                _loadedFrequency = (BarSize)ResolutionComboBox.SelectedItem;
                _loadedTimeZone = TimezoneComboBox.Text;
            }

            StatusLabel.Content = string.Format("Loaded {0} Bars", Data.Count);
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

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            //get the selected bars
            var rows = DataGrid.SelectedItems;
            if (rows.Count < 0) return;

            var result = MessageBox.Show(string.Format("Are you sure you want to delete {0} rows?", rows.Count), "Delete Rows", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            var toDelete = rows.Cast<OHLCBar>().ToList();

            //data is stored in the db at exchange time. But we may have loaded it at UTC or local.
            //If so, we must convert it back.
            foreach (OHLCBar b in toDelete)
            {
                if (_loadedTimeZone == "UTC")
                {
                    b.DT = TimeZoneInfo.ConvertTimeFromUtc(b.DT, _tzInfo);
                }
                else if (_loadedTimeZone == "Local")
                {
                    b.DT = TimeZoneInfo.ConvertTime(b.DT, TimeZoneInfo.Local, _tzInfo);
                }
            }


            using (var localStorage = DataStorageFactory.Get())
            {
                localStorage.DeleteData(TheInstrument, _loadedFrequency, toDelete);
            }

            foreach (OHLCBar bar in toDelete)
            {
                Data.Remove(bar);
            }
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

        private void SaveChangesBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var localStorage = DataStorageFactory.Get())
            {
                try
                {
                    localStorage.UpdateData(Data.ToList(), TheInstrument, _loadedFrequency, true);
                    MessageBox.Show("Successfully updated data.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while updating data: " + ex.Message);
                }
            }
        }

        private void AdjustBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var tmpData = Data.ToList();
            PriceAdjuster.AdjustData(ref tmpData);
            Data.Clear();
            foreach (OHLCBar b in tmpData)
            {
                Data.Add(b);
            }
        }
    }
}
