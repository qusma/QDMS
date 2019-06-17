// -----------------------------------------------------------------------
// <copyright file="DividendUpdateJobSettings.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    /// <summary>
    /// Settings for the job that automatically updates dividend data
    /// </summary>
    public class DividendUpdateJobSettings : IJobSettings
    {
        /// <summary>
        /// 
        /// </summary>
        public int BusinessDaysAhead { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int BusinessDaysBack { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string DataSource { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }
        /// <summary>
        /// Tag.
        /// </summary>
        public virtual Tag Tag { get; set; }

        /// <summary>
        /// If UseTag = true, instruments having this tag are updated.
        /// </summary>
        public int? TagID { get; set; }

        /// <inheritdoc />
        public TimeSpan Time { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool UseTag { get; set; }

        /// <inheritdoc />
        public bool WeekDaysOnly { get; set; }
    }
}