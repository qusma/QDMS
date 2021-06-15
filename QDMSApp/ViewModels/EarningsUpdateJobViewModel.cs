﻿// -----------------------------------------------------------------------
// <copyright file="EarningsUpdateJobViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMS.Server.Validation;
using ReactiveUI;
using System;

namespace QDMSApp.ViewModels
{
    public class EarningsUpdateJobViewModel : JobViewModelBase<EarningsUpdateJobSettings>
    {
        /// <summary>
        /// For design-time purposes only
        /// </summary>
        [Obsolete]
        public EarningsUpdateJobViewModel() { }

        public EarningsUpdateJobViewModel(EarningsUpdateJobSettings job, IDataClient client, IDialogCoordinator dialogCoordinator, object dialogContext)
            : base(job, new EarningsUpdateJobSettingsValidator(), client, dialogCoordinator, dialogContext)
        {
        }

        public int BusinessDaysBack
        {
            get => Model.BusinessDaysBack;
            set { Model.BusinessDaysBack = value; this.RaisePropertyChanged(); }
        }

        public int BusinessDaysAhead
        {
            get => Model.BusinessDaysAhead;
            set { Model.BusinessDaysAhead = value; this.RaisePropertyChanged(); }
        }

        public string DataSource
        {
            get => Model.DataSource;
            set { Model.DataSource = value; this.RaisePropertyChanged(); }
        }

        public int? TagID
        {
            get => Model.TagID;
            set
            {
                Model.TagID = value;
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