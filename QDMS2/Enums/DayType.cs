// -----------------------------------------------------------------------
// <copyright file="DayType.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    /// <summary>
    /// Calendar vs business day
    /// </summary>
    [Serializable]
    public enum DayType : int
    {
        /// <summary>
        /// Calendar Day
        /// </summary>
        [Description("Calendar Day")]
        Calendar = 0,
        /// <summary>
        /// Business Day
        /// </summary>
        [Description("Business Day")]
        Business = 1,
    }
}
