// -----------------------------------------------------------------------
// <copyright file="DateTimeExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NodaTime;
using QLNet;
using System;

namespace QDMS
{
    /// <summary>
    /// For internal use
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// LocalDate to DateTime
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this LocalDate dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day);
        }

        /// <summary>
        /// LocalDateTime to DateTime
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this LocalDateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
        }

        /// <summary>
        /// Add business days to datetime
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="days"></param>
        /// <param name="calendar"></param>
        /// <returns></returns>
        public static DateTime AddBusinessDays(this DateTime dt, int days, Calendar calendar = null)
        {
            calendar = calendar ?? MyUtils.GetCalendarFromCountryCode("US");
            int counter = days;
            while (counter != 0)
            {
                dt = dt.AddDays(Math.Sign(counter));
                if (calendar.isBusinessDay(dt))
                {
                    counter -= Math.Sign(counter);
                }
            }
            return dt;
        }
    }
}
