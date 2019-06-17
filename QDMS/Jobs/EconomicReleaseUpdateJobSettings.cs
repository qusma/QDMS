// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseUpdateJobDetails.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    /// <summary>
    /// Settings for the job that automatically updates economic release data
    /// </summary>
    public class EconomicReleaseUpdateJobSettings : IJobSettings
    {
        /// <inheritdoc />
        public bool WeekDaysOnly { get; set; }


        /// <inheritdoc />
        public TimeSpan Time { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <summary>
        /// How many business days back should be requested
        /// </summary>
        public int BusinessDaysBack { get; set; }

        /// <summary>
        /// How many business days ahead should be requested
        /// </summary>
        public int BusinessDaysAhead { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string DataSource { get; set; }
    }
}