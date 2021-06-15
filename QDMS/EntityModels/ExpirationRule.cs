// -----------------------------------------------------------------------
// <copyright file="ExpirationRule.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// The idea behind the way expiration rules work is to first find
// the "reference day", and then use an offset from that day to find
// the actual expiration date.

using Newtonsoft.Json;
using ProtoBuf;
using System;

namespace QDMS
{
    /// <summary>
    /// This class holds a set of rules that collectively can be used to deduce the expiration date of a futures or options contract.
    /// </summary>
    [ProtoContract]
    public class ExpirationRule
    {
        private bool _referenceUsesDays;
        private bool _referenceDayIsLastBusinessDayOfMonth;

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
        [Obsolete("Use ReferenceDayType instead")]
        [JsonIgnore]
        public bool ReferenceUsesDays
        {
            get { return _referenceUsesDays; }
            set { _referenceUsesDays = value; }
        }

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

        /// <summary>
        /// If this is true, the reference day is set to the last business day of the relevant month.
        /// </summary>
        [ProtoMember(10)]
        [Obsolete("Use ReferenceDayType instead")]
        [JsonIgnore]
        public bool ReferenceDayIsLastBusinessDayOfMonth
        {
            get { return _referenceDayIsLastBusinessDayOfMonth; }
            set { _referenceDayIsLastBusinessDayOfMonth = value; }
        }

        /// <summary>
        /// If this is true, if the reference day happens to be a holiday, it is moved back until it falls on a business day.
        /// </summary>
        [ProtoMember(11)]
        public bool ReferenceDayMustBeBusinessDay { get; set; }

        /// <summary>
        /// Rule for setting the reference day
        /// </summary>
        [ProtoIgnore]
        public ReferenceDayType ReferenceDayType
        {
            get
            {
                if (_referenceDayIsLastBusinessDayOfMonth) return ReferenceDayType.LastDayOfMonth;
                if (_referenceUsesDays) return ReferenceDayType.CalendarDays;
                return ReferenceDayType.WeekDays;
            }
            set
            {
                _referenceDayIsLastBusinessDayOfMonth = value == ReferenceDayType.LastDayOfMonth;
                _referenceUsesDays = value == ReferenceDayType.CalendarDays;
            }
        }
    }
}
