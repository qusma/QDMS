// -----------------------------------------------------------------------
// <copyright file="SchedulerViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMSClient;
using ReactiveUI;

namespace QDMSServer.ViewModels
{
    public class SchedulerViewModel : ReactiveObject
    {
        private readonly IDataClient _client;

        /// <summary>
        /// WARNING: used only for design-time purposes, do not use this!
        /// </summary>
        [Obsolete]
        public SchedulerViewModel()
        {
        }

        public SchedulerViewModel(IDataClient client, IDialogCoordinator dialogCoordinator)
        {
            _client = client;
            DialogCoordinator = dialogCoordinator;

            Jobs = new ReactiveList<IJobViewModel>();
            Tags = new ReactiveList<Tag>();
            Instruments = new ReactiveList<Instrument>();
            EconomicReleaseDataSources = new ReactiveList<string>();
            DividendDataSources = new ReactiveList<string>();

            CreateCommands();
        }

        private void CreateCommands()
        {
            Add = ReactiveCommand.Create(ExecuteAdd);

            //get existing jobs and tags/instruments
            Load = ReactiveCommand.CreateFromTask(async _ =>
            {
                var dataUpdateJobs = _client.GetaDataUpdateJobs();
                var econReleaseUpdateJobs = _client.GetEconomicReleaseUpdateJobs();
                var dividendUpdateJobs = _client.GetDividendUpdateJobs();
                var tags = _client.GetTags();
                var instruments = _client.GetInstruments();
                var econReleaseSources = _client.GetEconomicReleaseDataSources();
                var dividendSources = _client.GetDividendDataSources();

                await Task.WhenAll(dataUpdateJobs, econReleaseUpdateJobs, dividendUpdateJobs,
                    tags, instruments, econReleaseSources, dividendSources).ConfigureAwait(false);

                var responses = new ApiResponse[] { dataUpdateJobs.Result, econReleaseUpdateJobs.Result, dividendUpdateJobs.Result, tags.Result, instruments.Result, econReleaseSources.Result, dividendSources.Result };
                if (await responses.DisplayErrors(this, DialogCoordinator).ConfigureAwait(true)) return null;

                Tags.AddRange(tags.Result.Result);
                Instruments.AddRange(instruments.Result.Result);
                EconomicReleaseDataSources.AddRange(econReleaseSources.Result.Result);
                DividendDataSources.AddRange(dividendSources.Result.Result);

                var jobs = new List<IJobSettings>();
                jobs.AddRange(dataUpdateJobs.Result.Result);
                jobs.AddRange(econReleaseUpdateJobs.Result.Result);
                jobs.AddRange(dividendUpdateJobs.Result.Result);

                return jobs;
            });
            Load.Subscribe(jobs =>
            {
                if (jobs == null) return;

                foreach (var job in jobs)
                {
                    Jobs.Add(GetJobViewModel(job));
                }
            });

            //Delete job
            var deleteCanExecute = this
                .WhenAnyValue(x => x.SelectedJob)
                .Select(x => x != null && !string.IsNullOrEmpty(x.PreChangeName));

            Delete = ReactiveCommand.CreateFromTask(async _ =>
            {
                //Give a dialog to confirm the deletion
                MessageDialogResult dialogResult = await DialogCoordinator.ShowMessageAsync(this,
                    "Delete Job",
                    string.Format("Are you sure you want to delete {0}?", SelectedJob.Name),
                    MessageDialogStyle.AffirmativeAndNegative);

                if (dialogResult != MessageDialogResult.Affirmative) return;

                //If the name has changed but hasn't been saved, we change it back to be in sync with the server
                SelectedJob.Name = SelectedJob.PreChangeName;

                //Request deletion
                var response = await SelectedJob.Delete.Execute();
                if (await response.DisplayErrors(this, DialogCoordinator)) return;

                //if it was successful, remove the VM from the list
                Jobs.Remove(SelectedJob);
                SelectedJob = null;
            },
            deleteCanExecute);
        }

        private IJobViewModel GetJobViewModel(IJobSettings job)
        {
            if (job is DataUpdateJobSettings)
            {
                return new DataUpdateJobViewModel((DataUpdateJobSettings)job, _client, DialogCoordinator, this);
            }
            if (job is EconomicReleaseUpdateJobSettings)
            {
                return new EconomicReleaseUpdateJobViewModel((EconomicReleaseUpdateJobSettings)job, _client, DialogCoordinator, this);
            }
            if (job is DividendUpdateJobSettings)
            {
                return new DividendUpdateJobViewModel((DividendUpdateJobSettings)job, _client, DialogCoordinator, this);
            }

            throw new NotImplementedException();
        }

        private void ExecuteAdd()
        {
            string selectedJob;

            //Present a dialog for the user to select the type of job they want to add
            var dialog = new CustomDialog { Title = "Add New Job" };
            var panel = new StackPanel();
            panel.Children.Add(new RadioButton { Content = "Data Update", Margin = new Thickness(5), IsChecked = true });
            panel.Children.Add(new RadioButton { Content = "Economic Release Update", Margin = new Thickness(5), IsChecked = false });
            panel.Children.Add(new RadioButton { Content = "Dividend Update", Margin = new Thickness(5), IsChecked = false });

            var addBtn = new Button { Content = "Add" };
            addBtn.Click += (s, e) =>
            {
                DialogCoordinator.HideMetroDialogAsync(this, dialog);
                selectedJob = (string)panel.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked.HasValue && r.IsChecked.Value)?.Content;
                AddJob(selectedJob);
            };
            panel.Children.Add(addBtn);
            dialog.Content = panel;

            DialogCoordinator.ShowMetroDialogAsync(this, dialog);
        }

        private void AddJob(string selectedJob)
        {
            IJobSettings job = null;
            //create the jobdetails and add
            if (selectedJob == "Data Update")
            {
                job = new DataUpdateJobSettings { Name = GetJobName("DataUpdateJob"), UseTag = true, Frequency = BarSize.OneDay, Time = new TimeSpan(8, 0, 0), WeekDaysOnly = true };
            }
            else if (selectedJob == "Economic Release Update")
            {
                job = new EconomicReleaseUpdateJobSettings { Name = GetJobName("EconomicReleaseUpdateJob"), BusinessDaysBack = 1, BusinessDaysAhead = 7, DataSource = "FXStreet" };
            }
            else if (selectedJob == "Dividend Update")
            {
                job = new DividendUpdateJobSettings { Name = GetJobName("DividendUpdateJob"), BusinessDaysBack = 0, BusinessDaysAhead = 3, DataSource = "Nasdaq" };
            }

            var jobVm = GetJobViewModel(job);
            Jobs.Add(jobVm);
            SelectedJob = jobVm;
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

        public ReactiveCommand<Unit, Unit> Add { get; set; }

        public ReactiveCommand<Unit, Unit> Update { get; set; }

        public ReactiveCommand<Unit, List<IJobSettings>> Load { get; private set; }

        public ReactiveCommand<Unit, Unit> Delete { get; private set; }

        public IDialogCoordinator DialogCoordinator { get; set; }

        public ReactiveList<Instrument> Instruments { get; set; }

        public ReactiveList<IJobViewModel> Jobs { get; }

        public ReactiveList<string> EconomicReleaseDataSources { get; set; }

        public ReactiveList<string> DividendDataSources { get; set; }

        public IJobViewModel SelectedJob
        {
            get => _selectedJob;
            set => this.RaiseAndSetIfChanged(ref _selectedJob, value);
        }

        public ReactiveList<Tag> Tags { get; set; }

        private IJobViewModel _selectedJob;
    }
}