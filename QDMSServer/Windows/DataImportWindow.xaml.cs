// -----------------------------------------------------------------------
// <copyright file="DataImportWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using EntityData;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using QDMS;
using QDMSServer.DataSources;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for DataImportWindow.xaml
    /// </summary>
    public partial class DataImportWindow : MetroWindow
    {
        public DataTable Data { get; set; }
        private Instrument _instrument;

        public DataImportWindow(Instrument instrument)
        {
            InitializeComponent();
            
            //reload the instrument first to make sure we have up-to-date data
            using (var context = new MyDBContext())
            {
                context.Instruments.Attach(instrument);
                context.Entry(instrument).Reload();
                _instrument = instrument;
            }

            Title += " - " + _instrument.Symbol;

            //fill frequency combo box
            var values = MyUtils.GetEnumValues<BarSize>();
            foreach (BarSize s in values)
            {
                FrequencyComboBox.Items.Add(s);
            }
            FrequencyComboBox.SelectedItem = BarSize.OneDay;

            MinDT.Value = new DateTime(1950, 1, 1);
            MaxDT.Value = DateTime.Now;
        }

        private void SelectFileBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.Filter = "CSV Files (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*";

            bool? result = dlg.ShowDialog();

            if (!result.Value) return;


            FilePathTextBox.Text = dlg.FileName;
            //open the file
            try
            {
                var builder = new StringBuilder();
                using (StreamReader sr = new StreamReader(dlg.FileName))
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        builder.AppendLine(sr.ReadLine());
                        if (sr.EndOfStream) break;
                    }
                }
                FileContentsTextBox.Text = builder.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read file: " + ex.Message);
                return;
            }

            //parse the data
            DoParse();
        }

        private void DoParse()
        {
            if (StartingLine == null) return; //separator index change can be triggered too early, so we just bail out

            char[] separator = DelimiterBox.Text.ToCharArray();
            Data = new DataTable();

            int startLine;
            bool success = int.TryParse(StartingLine.Text, out startLine);
            if (!success) startLine = 1;
            startLine = startLine - 1;

            if (startLine < 0) return;

            string[] lines = Regex.Split(FileContentsTextBox.Text, @"(?:\r\n){1,}");
            if (startLine >= lines.Length) return;

            int colCount = 0;

            for (int i = startLine; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;

                string[] items = lines[i].Split(separator);
                if (i == startLine)
                {
                    for (int j = 0; j < items.Length; j++)
                    {
                        //if it's the first line, we have to create the appropriate columns in the datatable
                        Data.Columns.Add("col" + j);
                    }
                    colCount = items.Length;
                }

                var dr = Data.NewRow();
                for (int j = 0; j < Math.Min(colCount, items.Length); j++)
                {
                    dr["col" + j] = items[j].Trim();
                }
                Data.Rows.Add(dr);
            }
            TheDataGrid.ItemsSource = null;
            TheDataGrid.ItemsSource = Data.DefaultView;

            DoFormatColoring();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DelimiterBox_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            DoParse();
        }

        private void DoFormatColoring()
        {
            if (Data.Columns.Contains("Date"))
            {
                string sample = (string)Data.Rows[0]["Date"];
                DateTime res;
                bool correctFormat = DateTime.TryParseExact(sample, DateFormatTextBox.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out res);
                DateFormatTextBox.BorderBrush = correctFormat ? Brushes.Green : Brushes.Red;
            }
            else
            {
                DateFormatTextBox.BorderBrush = Brushes.Gray;
            }

            if (Data.Columns.Contains("Time"))
            {
                string sample = (string)Data.Rows[0]["Time"];
                DateTime res;
                bool correctFormat = DateTime.TryParseExact(sample, TimeFormatTextBox.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out res);
                TimeFormatTextBox.BorderBrush = correctFormat ? Brushes.Green : Brushes.Red;
            }
            else
            {
                TimeFormatTextBox.BorderBrush = Brushes.Gray;
            }

            if (Data.Columns.Contains("DateTime"))
            {
                string sample = (string)Data.Rows[0]["DateTime"];
                DateTime res;
                bool correctFormat = DateTime.TryParseExact(sample, DateTimeFormatTextBox.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out res);
                DateTimeFormatTextBox.BorderBrush = correctFormat ? Brushes.Green : Brushes.Red;
            }
            else
            {
                DateTimeFormatTextBox.BorderBrush = Brushes.Gray;
            }

        }

        private void DateFormatTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            DoFormatColoring();
            
        }

        private void SetColumnType_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var header = (DataGridColumnHeader)((ContextMenu)((MenuItem)routedEventArgs.Source).Parent).PlacementTarget;
            string newName = (string)((MenuItem)routedEventArgs.Source).Header;
            string oldName = (string)header.Content;
            if (Data.Columns.Contains(newName)) return;

            Data.Columns[oldName].ColumnName = newName;
            header.Content = newName;

            //the column is still bound to the old name, so we have to re-bind it
            var gridColumn = (DataGridTextColumn)TheDataGrid.Columns.FirstOrDefault(x => (string) x.Header == oldName);
            if (gridColumn != null)
            {
                gridColumn.Binding = new Binding(newName);
                gridColumn.Header = newName;
            }

            DoFormatColoring();
        }

        private OHLCBar ParseLine(string[] items, string[] columns, decimal priceMultiplier, int volumeMultiplier)
        {
            var bar = new OHLCBar();
            TimeSpan? closingTime = null;
            bool success;

            for (int i = 0; i < items.Length; i++)
            {
                switch (columns[i])
                {
                    case "Date":
                        DateTime tmpDate;
                        success = DateTime.TryParseExact(
                            items[i], DateFormatTextBox.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmpDate);
                         if (!success)
                         {
                             throw new Exception("Incorrect date format.");
                         }
                         else
                         {
                             bar.DT = new DateTime(tmpDate.Ticks);
                         }
                        break;

                    case "DateTime":
                        DateTime tmpDT;
                        success = DateTime.TryParseExact(
                            items[i], DateTimeFormatTextBox.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmpDT);
                         if (!success)
                         {
                             throw new Exception("Incorrect datetime format.");
                         }
                         else
                         {
                             bar.DT = new DateTime(tmpDT.Ticks);
                         }
                        break;

                    case "Time":
                        DateTime tmpTS;
                        success = DateTime.TryParseExact(
                            items[i], TimeFormatTextBox.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmpTS);
                         if (!success)
                         {
                             throw new Exception("Incorrect time format.");
                         }
                         else
                         {
                             closingTime = TimeSpan.FromSeconds(tmpTS.TimeOfDay.TotalSeconds);
                         }
                        break;

                    case "Open":
                        bar.Open = priceMultiplier * decimal.Parse(items[i]);
                        break;

                    case "High":
                        bar.High = priceMultiplier * decimal.Parse(items[i]);
                        break;

                    case "Low":
                        bar.Low = priceMultiplier * decimal.Parse(items[i]);
                        break;

                    case "Close":
                        bar.Close = priceMultiplier * decimal.Parse(items[i]);
                        break;

                    case "AdjClose":
                        bar.AdjClose = priceMultiplier * decimal.Parse(items[i]);
                        break;

                    case "Volume":
                        bar.Volume = volumeMultiplier * long.Parse(items[i]);
                        break;

                    case "OpenInterest":
                        bar.OpenInterest = int.Parse(items[i]);
                        break;

                    case "Dividends":
                        bar.Dividend = decimal.Parse(items[i]);
                        break;

                    case "Splits":
                        bar.Split = decimal.Parse(items[i]);
                        break;
                }
            }

            //do the time addition
            if (closingTime != null)
            {
                bar.DT += closingTime.Value;
            }

            return bar;
        }

        private void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //check that we've got the relevant data needed
            if (!Data.Columns.Contains("Date") && !Data.Columns.Contains("DateTime"))
            {
                MessageBox.Show("Must have a date column.");
                return;
            }

            if ((BarSize)FrequencyComboBox.SelectedItem < BarSize.OneDay && !Data.Columns.Contains("DateTime") && !Data.Columns.Contains("Time"))
            {
                MessageBox.Show("Must have time column at this frequency");
                return;
            }

            if (!Data.Columns.Contains("Open") ||
                !Data.Columns.Contains("High") ||
                !Data.Columns.Contains("Low")  ||
                !Data.Columns.Contains("Close"))
            {
                MessageBox.Show("Must have all OHLC columns.");
                return;
            }

            //make sure the timezone is set, and get it
            if (string.IsNullOrEmpty(_instrument.Exchange.Timezone))
            {
                MessageBox.Show("Instrument's exchange has no set timezone, can't import.");
                return;
            }
            
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(_instrument.Exchange.Timezone);


            //get the multipliers
            decimal priceMultiplier;
            int volumeMultiplier;
            bool parseWorked = decimal.TryParse(PriceMultiplier.Text, out priceMultiplier);
            if (!parseWorked) priceMultiplier = 1;
            parseWorked = int.TryParse(VolumeMultiplier.Text, out volumeMultiplier);
            if (!parseWorked) volumeMultiplier = 1;

            //lines to skip
            int toSkip;
            parseWorked = int.TryParse(StartingLine.Text, out toSkip);
            if(!parseWorked) toSkip = 1;

            //get the frequency
            var frequency = (BarSize)FrequencyComboBox.SelectedItem;

            //separator
            char[] separator = DelimiterBox.Text.ToCharArray();


            List<OHLCBar> bars = new List<OHLCBar>();

            string[] columns = new string[Data.Columns.Count];
            for (int i = 0; i < Data.Columns.Count; i++)
            {
            	columns[i] = Data.Columns[i].ColumnName;
            }

            //determining time: if the freq is >= one day, then the time is simply the session end for this day
            Dictionary<int, TimeSpan> sessionEndTimes = new Dictionary<int, TimeSpan>();


            //1 day and up: we can load it all in one go with no trouble, also may require adjustment
            bool periodicSaving = frequency < BarSize.OneDay;
            OHLCBar bar;
            var barsCount = 0;
            using (StreamReader sr = new StreamReader(FilePathTextBox.Text))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    barsCount++;
                    if (barsCount < toSkip) continue;

                    try
                    {
                        bar = ParseLine(line.Split(separator), columns, priceMultiplier, volumeMultiplier);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Importing error: " + ex.Message);
                        return;
                    }

                    //only add the bar if it falls within the specified date range
                    if (bar.DT >= MinDT.Value && bar.DT <= MaxDT.Value)
                    {
                        bars.Add(bar);
                    }

                    //with 30 bars, we make a check to ensure that the user has entered the correct frequency
                    if (bars.Count == 30)
                    {
                        //the reason we have to use a bunch of bars and look for the most frequent timespan between them
                        //is that session breaks, daily breaks, weekends, etc. can have different timespans despite the
                        //correct frequency being chosen
                        List<int> secDiffs = new List<int>();
                        for (int i = 1; i < bars.Count; i++)
                        {
                            secDiffs.Add((int)Math.Round((bars[i].DT - bars[i - 1].DT).TotalSeconds));
                        }

                        int mostFrequent = secDiffs.MostFrequent();
                        if ((int)Math.Round(frequency.ToTimeSpan().TotalSeconds) != mostFrequent)
                        {
                            MessageBox.Show("You appear to have selected the wrong frequency.");
                            return;
                        }
                    }

                    if (periodicSaving && bars.Count > 1000)
                    {
                        //convert to exchange timezone
                        ConvertTimeZone(bars, tzInfo);

                        //low frequencies, < 1 day. No adjustment required and inserting data at intervals instead of all at once
                        using (var storage = DataStorageFactory.Get())
                        {
                            try
                            {
                                storage.AddData(bars, _instrument, frequency, OverwriteCheckbox.IsChecked.HasValue && OverwriteCheckbox.IsChecked.Value, false);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error: " + ex.Message);
                            }
                        }
                        bars.Clear();
                    }
                }
            }

            if (bars.Count == 0) return;

            //convert to exchange timezone
            ConvertTimeZone(bars, tzInfo);


            //if only the date column is set, we need to get the session info and generate the closing time ourselves
            if (frequency >= BarSize.OneDay && !Data.Columns.Contains("Time") && !Data.Columns.Contains("DateTime"))
            {
                //get the closing time for every day of the week
                var dotwValues = MyUtils.GetEnumValues<DayOfTheWeek>();

                foreach (DayOfTheWeek d in dotwValues)
                {
                    if (_instrument.Sessions.Any(x => x.ClosingDay == d && x.IsSessionEnd))
                    {
                        var endTime = _instrument.Sessions.First(x => x.ClosingDay == d && x.IsSessionEnd).ClosingTime;
                        sessionEndTimes.Add((int)d, endTime);
                    }
                    else
                    {
                        sessionEndTimes.Add((int)d, TimeSpan.FromSeconds(0));
                    }
                }

                for (int i = 0; i < bars.Count; i++)
                {
                    int dayOfWeek = bars[i].DT.DayOfWeek.ToInt();
                    bars[i].DT = bars[i].DT.Date + sessionEndTimes[dayOfWeek];
                }
            }

            //if there are no dividends/splits, but there IS an adjclose column, use that to adjust data right here
            //if there are divs/splits, adjustment will be done by the local storage
            if (frequency >= BarSize.OneDay && !Data.Columns.Contains("Dividends") && !Data.Columns.Contains("Splits") && Data.Columns.Contains("AdjClose"))
            {
                //if we have an adjusted close to work off of, we just use the ratio to get the OHL
                for (int i = 0; i < bars.Count; i++)
                {
                    if (bars[i].AdjClose == null) continue;

                    decimal ratio = bars[i].AdjClose.Value / bars[i].Close;
                    bars[i].AdjOpen = bars[i].Open * ratio;
                    bars[i].AdjHigh = bars[i].High * ratio;
                    bars[i].AdjLow = bars[i].Low * ratio;
                }
            }

            
            //sort by date
            if(frequency >= BarSize.OneDay)
                bars.Sort((x, y) => x.DT.CompareTo(y.DT));

            //try to import
            using (var storage = DataStorageFactory.Get())
            {
                try
                {
                    storage.AddData(bars, 
                        _instrument, 
                        frequency, 
                        OverwriteCheckbox.IsChecked.HasValue && OverwriteCheckbox.IsChecked.Value,
                        frequency >= BarSize.OneDay);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            sw.Stop();
            MessageBox.Show(string.Format("Imported {0} bars in {1} ms.", barsCount, sw.ElapsedMilliseconds));
        }

        private void ConvertTimeZone(List<OHLCBar> bars, TimeZoneInfo tzInfo)
        {
            //time zone conversion
            if (TimezoneComboBox.Text == "GMT" && (Data.Columns.Contains("Time") || Data.Columns.Contains("DateTime")))
            {
                for (int i = 0; i < bars.Count; i++)
                {
                    bars[i].DT = TimeZoneInfo.ConvertTimeFromUtc(bars[i].DT, tzInfo);
                }
            }
            else if (TimezoneComboBox.Text == "Local" && (Data.Columns.Contains("Time") || Data.Columns.Contains("DateTime")))
            {
                for (int i = 0; i < bars.Count; i++)
                {
                    bars[i].DT = TimeZoneInfo.ConvertTime(bars[i].DT, TimeZoneInfo.Local, tzInfo);
                }
            }
        }

        private void StartingLine_KeyUp(object sender, KeyEventArgs e)
        {
            DoParse();
        }
    }
}
