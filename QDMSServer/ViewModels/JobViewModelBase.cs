// -----------------------------------------------------------------------
// <copyright file="JobViewModelBase.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using QDMS.Server.Jobs;
using Quartz;
using ReactiveUI;
using System;

namespace QDMSServer.ViewModels
{
    public abstract class JobViewModelBase<T> : ReactiveObject, IJobViewModel where T : IJobDetails
    {
        private readonly IScheduler _scheduler;

        /// <summary>
        /// When changing the name, we need to keep track of the previous one as well to unschedule the previous job
        /// </summary>
        private string _preChangeName;

        private string _validationError;

        /// <summary>
        /// For design-time purposes only
        /// </summary>
        [Obsolete]
        protected JobViewModelBase() { }

        protected JobViewModelBase(T job, IScheduler scheduler)
        {
            Job = job;
            _scheduler = scheduler;
            _preChangeName = job.Name;

            Save = ReactiveCommand.Create(this.WhenAny(x => x.ValidationError, x => string.IsNullOrEmpty(x.Value)));
            Save.Subscribe(x => ExecuteSave());
        }

        public T Job { get; }
        public JobKey JobKey => new JobKey(Name, JobTypes.GetJobType(Job));

        public string Name
        {
            get { return Job.Name; }
            set { Job.Name = value; this.RaisePropertyChanged(); }
        }

        public ReactiveCommand<object> Save { get; }

        public string ValidationError
        {
            get { return _validationError; }
            set { this.RaiseAndSetIfChanged(ref _validationError, value); }
        }

        public void DeleteJob()
        {
            _scheduler.DeleteJob(new JobKey(_preChangeName, JobTypes.GetJobType(Job)));
        }

        private void ExecuteSave()
        {
            DeleteJob();

            JobsManager.ScheduleJob(_scheduler, Job);
            _preChangeName = Name;
        }
    }
}