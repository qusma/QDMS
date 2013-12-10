// -----------------------------------------------------------------------
// <copyright file="ExpirationRule.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    [Serializable]
    public class ExpirationRule
    {
        /// <summary>
        /// The future expires this many days before the Reference day.
        /// </summary>
        [ProtoMember(1)]
        public int DaysBefore { get; set; }

        /// <summary>
        /// Use calendar days or business days for the calculation.
        /// </summary>
        [ProtoMember(2)]
        public DayType DayType { get; set; }

        /// <summary>
        /// If true, the reference point is set at a specified number of calendar days of a specified month.
        /// If false, we use a set number of elapsed days of the week to set the reference point.
        /// </summary>
        [ProtoMember(5)]
        public bool ReferenceUsesDays { get; set; }

        /// <summary>
        /// If ReferenceUsesDays is true, this sets the day of the month on which the reference day is.
        /// </summary>
        [ProtoMember(6)]
        public int ReferenceDays { get; set; }

        /// <summary>
        /// The month that the reference day is in: previous/current/next.
        /// </summary>
        [ProtoMember(7)]
        public RelativeMonth ReferenceRelativeMonth { get; set; }

        /// <summary>
        /// When using a weekday-based reference day, this sets the number of weeks that must have passed for the reference day to be set.
        /// </summary>
        [ProtoMember(8)]
        public WeekDayCount ReferenceWeekDayCount { get; set; }

        /// <summary>
        /// The weekday of the reference day.
        /// </summary>
        [ProtoMember(9)]
        public DayOfTheWeek ReferenceWeekDay { get; set; }
    }
}
