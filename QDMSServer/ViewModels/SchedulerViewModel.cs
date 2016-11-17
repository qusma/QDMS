// -----------------------------------------------------------------------
// <copyright file="SchedulerViewModel.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using NLog;
using QDMS;
using QDMS.Server.Jobs;
using QDMS.Server.Jobs.JobDetails;
using Quartz;
using Quartz.Impl.Matchers;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows;
using System.Windows.Controls;
using QDMS.Server;

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
            EconomicReleaseDataSources = new ObservableCollection<string> { "FXStreet" }; //we'll be grabbing this through the api in the future

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
                        var jd = JsonConvert.DeserializeObject<DataUpdateJobSettings>((string)jobDetails.JobDataMap["settings"]);
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
                else if (job.Group == JobTypes.EconomicRelease)
                {
                    try
                    {
                        var jd = JsonConvert.DeserializeObject<EconomicReleaseUpdateJobSettings>((string)jobDetails.JobDataMap["settings"]);

                        Jobs.Add(new EconomicReleaseUpdateJobViewModel(jd, _scheduler));
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Failed to deserialize economic release update job");
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

                var im = new InstrumentRepository(context);
                Instruments.AddRange(im.FindInstruments().Result);
            }
        }

        public void Shutdown()
        {
            _scheduler.Shutdown(true);
        }

        private void CreateCommands()
        {
            Delete = ReactiveCommand.Create<DataUpdateJobViewModel>(
                ExecuteDelete, 
                this.WhenAny(x => x.SelectedJob, x => x.Value != null));
            Delete.ThrownExceptions.Subscribe(e => _logger.Log(LogLevel.Warn, e, "Scheduler job deletion error"));

            Add = ReactiveCommand.Create(ExecuteAdd);
        }

        private void ExecuteAdd()
        {
            string selectedJob = "";

            //Present a dialog for the user to select the type of job they want to add
            var dialog = new CustomDialog { Title = "Add New Job" };
            var panel = new StackPanel();
            panel.Children.Add(new RadioButton { Content = "Data Update", Margin = new Thickness(5), IsChecked = true });
            panel.Children.Add(new RadioButton { Content = "Economic Release Update", Margin = new Thickness(5), IsChecked = false });

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
                var job = new DataUpdateJobSettings { Name = GetJobName("DataUpdateJob"), UseTag = true, Frequency = BarSize.OneDay, Time = new TimeSpan(8, 0, 0), WeekDaysOnly = true };
                var jobVm = new DataUpdateJobViewModel(job, _scheduler);
                Jobs.Add(jobVm);
                SelectedJob = jobVm;
            }
            else if (selectedJob == "Economic Release Update")
            {
                var job = new EconomicReleaseUpdateJobSettings { Name = GetJobName("EconomicReleaseUpdateJob"), BusinessDaysBack = 1, BusinessDaysAhead = 7, DataSource = "FXStreet" };
                var jobVm = new EconomicReleaseUpdateJobViewModel(job, _scheduler);
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

        public ReactiveCommand<Unit, Unit> Add { get; private set; }

        public ReactiveCommand<DataUpdateJobViewModel, Unit> Delete { get; private set; }

        public IDialogCoordinator DialogService { get; set; }

        public ReactiveList<Instrument> Instruments { get; set; }

        public ObservableCollection<IJobViewModel> Jobs { get; }
        public ObservableCollection<string> EconomicReleaseDataSources { get; set; }

        public IJobViewModel SelectedJob
        {
            get { return _selectedJob; }
            set { this.RaiseAndSetIfChanged(ref _selectedJob, value); }
        }

        public ReactiveList<Tag> Tags { get; set; }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IScheduler _scheduler;
        private IJobViewModel _selectedJob;
    }
}