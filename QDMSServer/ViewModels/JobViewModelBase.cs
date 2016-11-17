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
using System.Reactive;
using ReactiveUI.Legacy;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace QDMSServer.ViewModels
{
    /// <summary>
    /// ViewModels for jobs derive from this one
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class JobViewModelBase<T> : ReactiveObject, IJobViewModel where T : IJobSettings
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

            Save = ReactiveCommand.Create(ExecuteSave, this.WhenAny(x => x.ValidationError, x => string.IsNullOrEmpty(x.Value)));
        }

        public T Job { get; }
        public JobKey JobKey => new JobKey(Name, JobTypes.GetJobType(Job));

        public string Name
        {
            get { return Job.Name; }
            set { Job.Name = value; this.RaisePropertyChanged(); }
        }

        public ReactiveCommand<Unit, Unit> Save { get; }

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