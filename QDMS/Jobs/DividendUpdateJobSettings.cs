// -----------------------------------------------------------------------
// <copyright file="DividendUpdateJobSettings.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public class DividendUpdateJobSettings : IJobSettings
    {
        public int BusinessDaysAhead { get; set; }
        public int BusinessDaysBack { get; set; }
        public string DataSource { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// Tag.
        /// </summary>
        public virtual Tag Tag { get; set; }

        /// <summary>
        /// If UseTag = true, instruments having this tag are updated.
        /// </summary>
        public int? TagID { get; set; }

        public TimeSpan Time { get; set; }
        public bool UseTag { get; set; }
        public bool WeekDaysOnly { get; set; }
    }
}