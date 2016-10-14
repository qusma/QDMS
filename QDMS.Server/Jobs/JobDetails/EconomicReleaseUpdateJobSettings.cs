// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseUpdateJobDetails.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS.Annotations;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QDMS.Server.Jobs.JobDetails
{
    public class EconomicReleaseUpdateJobSettings : IJobSettings, INotifyPropertyChanged
    {
        /// <summary>
        /// If true, updates will only happen monday through friday.
        /// </summary>
        public bool WeekDaysOnly { get; set; }

        /// <summary>
        /// The time when the job runs.
        /// </summary>
        public TimeSpan Time { get; set; }

        public string Name { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public int BusinessDaysBack { get; set; }
        public int BusinessDaysAhead { get; set; }
        public string DataSource { get; set; }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}