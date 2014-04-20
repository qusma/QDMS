// -----------------------------------------------------------------------
// <copyright file="DayOfWeekExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public static class DayOfWeekExtensions
    {
        /// <summary>
        /// Convert day of week to integer, with Monday as 0.
        /// </summary>
        public static int ToInt(this DayOfWeek value)
        {
            switch (value)
            {
                case DayOfWeek.Sunday:
                    return 6;

                case DayOfWeek.Monday:
                    return 0;

                case DayOfWeek.Tuesday:
                    return 1;

                case DayOfWeek.Wednesday:
                    return 2;

                case DayOfWeek.Thursday:
                    return 3;

                case DayOfWeek.Friday:
                    return 4;

                case DayOfWeek.Saturday:
                    return 5;

            }
            return 0;
        }
    }
}
