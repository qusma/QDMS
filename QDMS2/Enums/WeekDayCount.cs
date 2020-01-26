// -----------------------------------------------------------------------
// <copyright file="WeekDayCount.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    /// <summary>
    /// For continuous futures
    /// </summary>
    [Serializable]
    public enum WeekDayCount : int
    {
        /// <summary>
        /// 1st
        /// </summary>
        [Description("1st")]
        First = 0,
        /// <summary>
        /// 2nd
        /// </summary>
        [Description("2nd")]
        Second = 1,
        /// <summary>
        /// 3rd
        /// </summary>
        [Description("3rd")]
        Third = 2,
        /// <summary>
        /// 4th
        /// </summary>
        [Description("4th")]
        Fourth = 3,
        /// <summary>
        /// Last
        /// </summary>
        [Description("Last")]
        Last = 4,
    }
}
