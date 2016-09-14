// -----------------------------------------------------------------------
// <copyright file="DateTimeExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using NodaTime;

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

        public static long ToTimestamp(this DateTime value)
        {
            long epoch = (value.Ticks - 621355968000000000) / 10000000;
            return epoch;
        }
    }
}
