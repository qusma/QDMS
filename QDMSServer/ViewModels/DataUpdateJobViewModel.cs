// -----------------------------------------------------------------------
// <copyright file="DataUpdateJobViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using QDMS;
using Quartz;
using ReactiveUI;

namespace QDMSServer.ViewModels
{
    public class DataUpdateJobViewModel : JobViewModelBase<DataUpdateJobDetails>
    {
        /// <summary>
        /// For design-time purposes only
        /// </summary>
        [Obsolete]
        public DataUpdateJobViewModel() : base() { }

        public DataUpdateJobViewModel(DataUpdateJobDetails job, IScheduler scheduler) : base(job, scheduler)
        {
            this.WhenAnyValue(x => x.UseTag, x => x.Instrument, x => x.Tag)
                .Subscribe(x => ValidateSettings());
        }


        private void ValidateSettings()
        {
            ValidationError = "";

            if (Job.UseTag && Job.Tag == null)
            {
                ValidationError = "You must select a tag.";
            }
            else if (Job.Instrument == null)
            {
                ValidationError = "You must select an instrument";
            }
        }

        public Instrument Instrument
        {
            get { return Job.Instrument; }
            set
            {
                Job.Instrument = value;
                Job.InstrumentID = value?.ID;
                this.RaisePropertyChanged();
            }
        }

        public Tag Tag
        {
            get { return Job.Tag; }
            set
            {
                Job.Tag = value;
                Job.TagID = value?.ID;
                this.RaisePropertyChanged();
            }
        }

        public bool UseTag
        {
            get { return Job.UseTag; }
            set
            {
                Job.UseTag = value;
                this.RaisePropertyChanged();
            }
        }
    }
}