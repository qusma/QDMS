// -----------------------------------------------------------------------
// <copyright file="DataEditWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using EntityData;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using QDMS;
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

            StartTime = new DateTime(1950, 1, 1, 0, 0, 0, 0);
            EndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, 0);

            if (!TheInstrument.ID.HasValue) return;

            //grab the data info
            using (var localStorage = new MySQLStorage())
            {
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
            using (var localStorage = new MySQLStorage())
            {
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(TheInstrument.Exchange.Timezone);
                var bars = localStorage.GetData(TheInstrument, StartTime, EndTime, (BarSize)ResolutionComboBox.SelectedItem);
                foreach (OHLCBar b in bars)
                {
                    //do any required time zone coversions
                    if (TimezoneComboBox.Text == "UTC")
                    {
                        b.DT = TimeZoneInfo.ConvertTimeToUtc(b.DT, tzInfo);
                    }
                    else if (TimezoneComboBox.Text == "Local")
                    {
                        b.DT = TimeZoneInfo.ConvertTime(b.DT, _tzInfo, TimeZoneInfo.Local);
                    }
                    Data.Add(b);
                }
                _loadedFrequency = (BarSize)ResolutionComboBox.SelectedItem;
            }

            StatusLabel.Content = string.Format("Loaded {0} Bars", Data.Count);
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            //get the selected bars
            var rows = DataGrid.SelectedItems;
            if (rows.Count < 0) return;

            var result = MessageBox.Show(string.Format("Are you sure you want to delete {0} rows?", rows.Count), "Delete Rows", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            var toDelete = rows.Cast<OHLCBar>().ToList();

            using (var localStorage = new MySQLStorage())
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
            using (var localStorage = new MySQLStorage())
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
    }
}
