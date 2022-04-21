// -----------------------------------------------------------------------
// <copyright file="EarningsUpdateJobSettings.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    /// <summary>
    /// Settings for the job that automatically updates earnings data
    /// </summary>
    public class EarningsUpdateJobSettings : IJobSettings
    {
        /// <summary>
        /// How many business days ahead should be requested
        /// </summary>
        public int BusinessDaysAhead { get; set; }

        /// <summary>
        /// How many business days back should be requested
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
        /// Use tags to filter which instruments are updated
        /// </summary>
        public bool UseTag { get; set; }


        /// <inheritdoc />
        public bool WeekDaysOnly { get; set; }
    }
}