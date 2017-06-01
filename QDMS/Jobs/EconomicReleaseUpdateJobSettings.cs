// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseUpdateJobDetails.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public class EconomicReleaseUpdateJobSettings : IJobSettings
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

        public int BusinessDaysBack { get; set; }
        public int BusinessDaysAhead { get; set; }
        public string DataSource { get; set; }
    }
}