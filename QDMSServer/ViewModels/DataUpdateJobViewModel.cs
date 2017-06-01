// -----------------------------------------------------------------------
// <copyright file="DataUpdateJobViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using QDMS.Server.Validation;
using ReactiveUI;
using System;
using MahApps.Metro.Controls.Dialogs;

namespace QDMSServer.ViewModels
{
    public class DataUpdateJobViewModel : JobViewModelBase<DataUpdateJobSettings>
    {
        /// <summary>
        /// For design-time purposes only
        /// </summary>
        [Obsolete]
        public DataUpdateJobViewModel() { }

        public DataUpdateJobViewModel(DataUpdateJobSettings job, IDataClient client, IDialogCoordinator dialogCoordinator, object dialogContext) 
            : base(job, new DataUpdateJobSettingsValidator(), client, dialogCoordinator, dialogContext)
        {
        }

        public Instrument Instrument
        {
            get => Model.Instrument;
            set
            {
                Model.Instrument = value;
                Model.InstrumentID = value?.ID;
                this.RaisePropertyChanged();
            }
        }

        public Tag Tag
        {
            get => Model.Tag;
            set
            {
                Model.Tag = value;
                Model.TagID = value?.ID;
                this.RaisePropertyChanged();
            }
        }

        public bool UseTag
        {
            get => Model.UseTag;
            set
            {
                Model.UseTag = value;
                this.RaisePropertyChanged();
            }
        }
    }
}