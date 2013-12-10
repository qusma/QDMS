// -----------------------------------------------------------------------
// <copyright file="DayType.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    [Serializable]
    public enum DayType : int
    {
        /// <summary>
        /// Calendar Day
        /// </summary>
        [Description("Calendar Day")]
        CalendarDay = 0,
        /// <summary>
        /// Business Day
        /// </summary>
        [Description("Business Day")]
        BusinessDay = 1,
    }
}
