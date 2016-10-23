// -----------------------------------------------------------------------
// <copyright file="DateTimeExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using NodaTime;
using QLNet;

namespace QDMS
{
    public static class DateTimeExtensions
    {
        public static DateTime ToDateTime(this LocalDate dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day);
        }

        public static DateTime ToDateTime(this LocalDateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
        }

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
