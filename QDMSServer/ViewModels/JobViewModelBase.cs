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
using QDMSClient;

namespace QDMSServer.ViewModels
{
    /// <summary>
    /// ViewModels for jobs derive from this one
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class JobViewModelBase<T> : ValidatingViewModelBase<T>, IJobViewModel where T : class, IJobSettings
    {
        /// <summary>
        /// For design-time purposes only
        /// </summary>
        [Obsolete]
        protected JobViewModelBase() : base(null, null) { }

        protected JobViewModelBase(T job, AbstractValidator<T> validator, IDataClient client, IDialogCoordinator dialogCoordinator, object dialogContext) : base(job, validator)
        {
            PreChangeName = job.Name;
            //Note the use of job vs Job below. The parameter has the type T which is necessary for the  client stuff to work
            Job = job;

            //Save job
            var saveCanExecute = this
                .WhenAnyValue(x => x.HasErrors)
                .Select(x => x == false);

            Save = ReactiveCommand.CreateFromTask(async _ =>
            {
                //First delete the existing job (if there is an existing job), with the old name
                string tmpName = Name;
                Name = PreChangeName;
                await client.DeleteJob(job).ConfigureAwait(true); //failure is OK here, it might not exist on the server if it's newly added
                Name = tmpName;
                
                //then add it with the new values
                var result = await client.AddJob(job).ConfigureAwait(true);
                if (await result.DisplayErrors(dialogContext, dialogCoordinator).ConfigureAwait(true)) return;
                PreChangeName = Name;
            },
            saveCanExecute);

            //this is here because we need to know the job type
            Delete = ReactiveCommand.CreateFromTask(async _ => (ApiResponse) await client.DeleteJob(job).ConfigureAwait(false));
        }

        public IJobSettings Job { get; }

        public JobKey JobKey => new JobKey(Name, JobTypes.GetJobType(Model));

        public string Name
        {
            get => Model.Name;
            set { Model.Name = value; this.RaisePropertyChanged(); }
        }

        /// <summary>
        /// When changing the name, we need to keep track of the previous one as well to unschedule the previous job
        /// </summary>
        public string PreChangeName { get; set; }
        public ReactiveCommand<Unit, Unit> Save { get; }
        public ReactiveCommand<Unit, ApiResponse> Delete { get; }
        public TimeSpan Time
        {
            get => Model.Time;
            set { Model.Time = value; this.RaisePropertyChanged(); }
        }

        public bool WeekDaysOnly
        {
            get => Model.WeekDaysOnly;
            set { Model.WeekDaysOnly = value; this.RaisePropertyChanged(); }
        }
    }
}