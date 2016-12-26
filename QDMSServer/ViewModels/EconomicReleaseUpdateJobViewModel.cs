// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseUpdateJobViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using ReactiveUI;
using System;
using MahApps.Metro.Controls.Dialogs;
using QDMS.Server.Validation;

namespace QDMSServer.ViewModels
{
    public class EconomicReleaseUpdateJobViewModel : JobViewModelBase<EconomicReleaseUpdateJobSettings>
    {
        /// <summary>
        /// For design-time purposes only
        /// </summary>
        [Obsolete]
        public EconomicReleaseUpdateJobViewModel() { }

        public EconomicReleaseUpdateJobViewModel(EconomicReleaseUpdateJobSettings job, IDataClient client, IDialogCoordinator dialogCoordinator) 
            : base(job, new EconomicReleaseUpdateJobSettingsValidator(), client, dialogCoordinator)
        {

        }

        public int BusinessDaysBack
        {
            get { return Model.BusinessDaysBack; }
            set { Model.BusinessDaysBack = value; this.RaisePropertyChanged(); }
        }

        public int BusinessDaysAhead
        {
            get { return Model.BusinessDaysAhead; }
            set { Model.BusinessDaysAhead = value; this.RaisePropertyChanged(); }
        }

        public string DataSource
        {
            get { return Model.DataSource; }
            set { Model.DataSource = value; this.RaisePropertyChanged(); }
        }
    }
}