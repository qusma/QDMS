// -----------------------------------------------------------------------
// <copyright file="SessionTemplatesWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
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
    public partial class ScheduledJobsWindow : MetroWindow
    {
        public ObservableCollection<DataUpdateJobDetails> Jobs { get; set; }
        public ObservableCollection<Tag> Tags { get; set; }
        public ObservableCollection<Instrument> Instruments { get; set; }

        public ScheduledJobsWindow()
        {
            InitializeComponent();
            DataContext = this;

            Jobs = new ObservableCollection<DataUpdateJobDetails>();
            Tags = new ObservableCollection<Tag>();
            Instruments = new ObservableCollection<Instrument>();

            using (var context = new MyDBContext())
            {
                var jobs = context.DataUpdateJobs.ToList();
                foreach (DataUpdateJobDetails job in jobs)
                {
                    Jobs.Add(job);
                }

                var tags = context.Tags.ToList();
                foreach (Tag tag in tags)
                {
                    Tags.Add(tag);
                }

                var im = new InstrumentManager();

                List<Instrument> instruments = im.FindInstruments(context);
                foreach (Instrument i in instruments)
                {
                    Instruments.Add(i);
                }
            }

        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var job = new DataUpdateJobDetails { Name = "NewJob", UseTag = true, Frequency = BarSize.OneDay, Time = new TimeSpan(8, 0, 0), WeekDaysOnly = true };
            Jobs.Add(job);
            JobsGrid.SelectedItem = job;

            using (var context = new MyDBContext())
            {
                context.DataUpdateJobs.Add(job);
                context.SaveChanges();
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if(JobsGrid.SelectedItems.Count != 1) return;

            var selectedJob = (DataUpdateJobDetails)JobsGrid.SelectedItem;

            var dialogResult = MessageBox.Show(string.Format("Are you sure you want to delete {0}?", selectedJob.Name), "Delete Job", MessageBoxButton.YesNo);
            if (dialogResult != MessageBoxResult.Yes) return;

            using (var context = new MyDBContext())
            {
                var job = context.DataUpdateJobs.FirstOrDefault(x => x.ID == selectedJob.ID);
                if (job == null) return;

                context.DataUpdateJobs.Remove(job);

                context.SaveChanges();
            }

            Jobs.Remove(selectedJob);

            CollectionViewSource.GetDefaultView(JobsGrid.ItemsSource).Refresh();
        }

        private void SaveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (JobsGrid.SelectedItems.Count != 1) return;

            if (FrequencyComboBox.SelectedItem == null)
            {
                MessageBox.Show("You must select a frequency.");
                return;
            }

            using (var context = new MyDBContext())
            {
                var job = (DataUpdateJobDetails)JobsGrid.SelectedItem;

                if (job.UseTag)
                {
                    if (TagsComboBox.SelectedItem == null)
                    {
                        MessageBox.Show("You must select a tag.");
                        return;
                    }

                    job.Instrument = null;
                    job.InstrumentID = null;
                    job.Tag = (Tag)TagsComboBox.SelectedItem;
                    job.TagID = job.Tag.ID;
                }
                else //job is for a specific instrument, not a tag
                {
                    if (InstrumentsComboBox.SelectedItem == null)
                    {
                        MessageBox.Show("You must select an instrument.");
                        return;
                    }

                    job.Instrument = (Instrument)InstrumentsComboBox.SelectedItem;
                    job.InstrumentID = job.Instrument.ID;
                    job.Tag = null;
                    job.TagID = null;
                }

                context.DataUpdateJobs.Attach(job);
                context.Entry(job).State = EntityState.Modified;
                context.SaveChanges();
            }
        }
    }
}
