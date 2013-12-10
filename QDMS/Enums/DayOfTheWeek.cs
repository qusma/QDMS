// -----------------------------------------------------------------------
// <copyright file="DayOfTheWeek.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    [Serializable]
    public enum DayOfTheWeek : int
    {
        /// <summary>
        /// Monday
        /// </summary>
        [Description("Monday")]
        Monday = 0,
        /// <summary>
        /// Tuesday
        /// </summary>
        [Description("Tuesday")]
        Tuesday = 1,
        /// <summary>
        /// Wednesday
        /// </summary>
        [Description("Wednesday")]
        Wednesday = 2,
        /// <summary>
        /// Thursday
        /// </summary>
        [Description("Thursday")]
        Thursday = 3,
        /// <summary>
        /// Friday
        /// </summary>
        [Description("Friday")]
        Friday = 4,
        /// <summary>
        /// Saturday
        /// </summary>
        [Description("Saturday")]
        Saturday = 5,
        /// <summary>
        /// Sunday
        /// </summary>
        [Description("Sunday")]
        Sunday = 6,
    }
}
