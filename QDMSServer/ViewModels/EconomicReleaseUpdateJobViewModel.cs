// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseUpdateJobViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS.Server.Jobs.JobDetails;
using Quartz;
using ReactiveUI;
using System;

namespace QDMSServer.ViewModels
{
    public class EconomicReleaseUpdateJobViewModel : JobViewModelBase<EconomicReleaseUpdateJobSettings>
    {
        /// <summary>
        /// For design-time purposes only
        /// </summary>
        [Obsolete]
        public EconomicReleaseUpdateJobViewModel() : base() { }

        public EconomicReleaseUpdateJobViewModel(EconomicReleaseUpdateJobSettings job, IScheduler scheduler) : base(job, scheduler)
        {
            this.WhenAnyValue(x => x.DaysBack, x => x.DaysAhead, x => x.DataSource)
                .Subscribe(x => ValidateSettings());
        }

        private void ValidateSettings()
        {
            ValidationError = "";
            if (DaysBack < 0) ValidationError = "Days Back must be greater than or equal to zero";
            if (DaysAhead < 0) ValidationError = "Days Back must be greater than or equal to zero";
            if (string.IsNullOrEmpty(Job.DataSource)) ValidationError = "Datasource cannot be empty";
        }

        public int DaysBack
        {
            get { return Job.BusinessDaysBack; }
            set { Job.BusinessDaysBack = value; this.RaisePropertyChanged(); }
        }

        public int DaysAhead
        {
            get { return Job.BusinessDaysAhead; }
            set { Job.BusinessDaysAhead = value; this.RaisePropertyChanged(); }
        }

        public string DataSource
        {
            get { return Job.DataSource; }
            set { Job.DataSource = value; this.RaisePropertyChanged(); }
        }
    }
}