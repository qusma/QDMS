// -----------------------------------------------------------------------
// <copyright file="SchedulerViewModel.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using QDMS;
using Quartz;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using QDMS.Server.Jobs;
using Quartz.Impl.Matchers;

namespace QDMSServer.ViewModels
{
    public class SchedulerViewModel : ReactiveObject
    {
        /// <summary>
        /// WARNING: used only for design-time purposes, do not use this!
        /// </summary>
        [Obsolete]
        public SchedulerViewModel()
        {
        }

        public SchedulerViewModel(IScheduler scheduler, IDialogCoordinator dialogService)
        {
            _scheduler = scheduler;
            DialogService = dialogService;

            Jobs = new ObservableCollection<IJobViewModel>();
            Tags = new ReactiveList<Tag>();
            Instruments = new ReactiveList<Instrument>();

            RefreshCollections();
            PopulateJobs();
            
            CreateCommands();
        }

        private void PopulateJobs()
        {
            var dataUpdateJobKeys = _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            foreach (JobKey job in dataUpdateJobKeys)
            {
                IJobDetail jobDetails = _scheduler.GetJobDetail(job);

                if (job.Group == JobTypes.DataUpdate)
                {
                    try
                    {
                        var jd = JsonConvert.DeserializeObject<DataUpdateJobDetails>((string)jobDetails.JobDataMap["settings"]);
                        if (jd.InstrumentID.HasValue)
                        {
                            jd.Instrument = Instruments.FirstOrDefault(x => x.ID == jd.InstrumentID.Value);
                        }
                        if (jd.TagID.HasValue)
                        {
                            jd.Tag = Tags.FirstOrDefault(x => x.ID == jd.TagID.Value);
                        }

                        Jobs.Add(new DataUpdateJobViewModel(jd, _scheduler));
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Failed to deserialize data update job");
                    }
                }
            }
        }

        public void RefreshCollections()
        {
            Tags.Clear();
            Instruments.Clear();

            using (var context = new MyDBContext())
            {
                Tags.AddRange(context.Tags.ToList());

                var im = new InstrumentManager();
                Instruments.AddRange(im.FindInstruments(context));
            }
        }

        public void Shutdown()
        {
            _scheduler.Shutdown(true);
        }

        private void CreateCommands()
        {
            Delete = ReactiveCommand.Create(this.WhenAny(x => x.SelectedJob, x => x.Value != null));
            Delete.Subscribe(x => ExecuteDelete(x as DataUpdateJobViewModel), e => _logger.Log(LogLevel.Warn, e, "Scheduler job deletion error"));

            Add = ReactiveCommand.Create();
            Add.Subscribe(_ => ExecuteAdd());
        }

        private void ExecuteAdd()
        {
            string selectedJob = "";

            //Present a dialog for the user to select the type of job they want to add
            var dialog = new CustomDialog { Title = "Add New Job" };
            var panel = new StackPanel();
            panel.Children.Add(new RadioButton { Content = "Data Update", Margin = new Thickness(5), IsChecked = true });

            var addBtn = new Button { Content = "Add" };
            addBtn.Click += (s, e) =>
            {
                DialogService.HideMetroDialogAsync(this, dialog);
                selectedJob = (string)panel.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked.HasValue && r.IsChecked.Value)?.Content;
                AddJob(selectedJob);
            };
            panel.Children.Add(addBtn);
            dialog.Content = panel;

            DialogService.ShowMetroDialogAsync(this, dialog);
        }

        private void AddJob(string selectedJob)
        {
            //create the jobdetails and add
            if (selectedJob == "Data Update")
            {
                var job = new DataUpdateJobDetails { Name = GetJobName("DataUpdateJob"), UseTag = true, Frequency = BarSize.OneDay, Time = new TimeSpan(8, 0, 0), WeekDaysOnly = true };
                var jobVm = new DataUpdateJobViewModel(job, _scheduler);
                Jobs.Add(jobVm);
                SelectedJob = jobVm;
            }
        }

        /// <summary>
        /// Get job name that doesn't overlap with an existing one
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetJobName(string name)
        {
            string newName = name;
            int counter = 1;
            while (Jobs.Any(x => x.Name == newName))
            {
                newName = name + counter;
                counter++;
            }
            return newName;
        }

        private async void ExecuteDelete(IJobViewModel vm)
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));

            MessageDialogResult dialogResult = await DialogService.ShowMessageAsync(this,
                "Delete Job",
                string.Format("Are you sure you want to delete {0}?", vm.Name),
                MessageDialogStyle.AffirmativeAndNegative);

            if (dialogResult != MessageDialogResult.Affirmative) return;

            vm.DeleteJob();

            Jobs.Remove(vm);

            SelectedJob = null;
        }

        public ReactiveCommand<object> Add { get; private set; }

        public ReactiveCommand<object> Delete { get; private set; }

        public IDialogCoordinator DialogService { get; set; }

        public ReactiveList<Instrument> Instruments { get; set; }

        public ObservableCollection<IJobViewModel> Jobs { get; }

        public DataUpdateJobViewModel SelectedJob
        {
            get { return _selectedJob; }
            set { this.RaiseAndSetIfChanged(ref _selectedJob, value); }
        }

        public ReactiveList<Tag> Tags { get; set; }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IScheduler _scheduler;
        private DataUpdateJobViewModel _selectedJob;
    }
}