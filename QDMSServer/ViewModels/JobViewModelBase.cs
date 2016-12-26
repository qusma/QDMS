// -----------------------------------------------------------------------
// <copyright file="JobViewModelBase.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMS.Server.Jobs;
using Quartz;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace QDMSServer.ViewModels
{
    /// <summary>
    /// ViewModels for jobs derive from this one
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class JobViewModelBase<T> : ValidatingViewModelBase<T>, IJobViewModel where T : class, IJobSettings
    {
        private readonly T _typedJob;

        /// <summary>
        /// For design-time purposes only
        /// </summary>
        [Obsolete]
        protected JobViewModelBase() : base(null, null) { }

        protected JobViewModelBase(T job, AbstractValidator<T> validator, IDataClient client, IDialogCoordinator dialogCoordinator) : base(job, validator)
        {
            PreChangeName = job.Name;
            Job = job;
            _typedJob = job;

            //Save job
            var saveCanExecute = this
                .WhenAnyValue(x => x.HasErrors)
                .Select(x => x == false);

            Save = ReactiveCommand.CreateFromTask(async _ =>
            {
                //First delete the existing job (if there is an existing job), with the old name
                string tmpName = Name;
                Name = PreChangeName;
                await client.DeleteJob(_typedJob); //failure is OK here, it might not exist on the server if it's newly added
                Name = tmpName;

                //then add it with the new values
                var result = await client.AddJob(_typedJob).ConfigureAwait(true);
                if (await result.DisplayErrors(this, dialogCoordinator)) return;

                PreChangeName = Name;
            },
            saveCanExecute);
        }

        public IJobSettings Job { get; }

        public JobKey JobKey => new JobKey(Name, JobTypes.GetJobType(Model));

        public string Name
        {
            get { return Model.Name; }
            set { Model.Name = value; this.RaisePropertyChanged(); }
        }

        /// <summary>
        /// When changing the name, we need to keep track of the previous one as well to unschedule the previous job
        /// </summary>
        public string PreChangeName { get; set; }
        public ReactiveCommand<Unit, Unit> Save { get; }
        public TimeSpan Time
        {
            get { return Model.Time; }
            set { Model.Time = value; this.RaisePropertyChanged(); }
        }

        public bool WeekDaysOnly
        {
            get { return Model.WeekDaysOnly; }
            set { Model.WeekDaysOnly = value; this.RaisePropertyChanged(); }
        }
    }
}