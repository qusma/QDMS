// -----------------------------------------------------------------------
// <copyright file="ReferenceDayType.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QDMS
{
    /// <summary>
    /// 
    /// </summary>
    public enum ReferenceDayType
    {
        /// <summary>
        /// The reference point is set at a specified number of calendar days of a specified month.
        /// </summary>
        CalendarDays,

        /// <summary>
        /// The reference point is set at a specified number of elapsed days of a specified week.
        /// </summary>
        WeekDays,

        /// <summary>
        /// The reference day is set to the last business day of the relevant month.
        /// </summary>
        LastDayOfMonth
    }
}
