// -----------------------------------------------------------------------
// <copyright file="InstrumentExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace QDMS
{
    public static class InstrumentExtensions
    {
        /// <summary>
        /// Gets the "ending" session's closing time, one for each day of the week.
        /// </summary>
        public static Dictionary<int, TimeSpan> SessionEndTimesByDay(this Instrument instrument)
        {
            Dictionary<int, TimeSpan> sessionEndTimes = new Dictionary<int, TimeSpan>();
            if (instrument.Sessions == null) return sessionEndTimes;
            
            var dotwValues = MyUtils.GetEnumValues<DayOfTheWeek>();

            foreach (DayOfTheWeek d in dotwValues)
            {
                if (instrument.Sessions.Any(x => x.ClosingDay == d && x.IsSessionEnd))
                {
                    var endTime = instrument.Sessions.First(x => x.ClosingDay == d && x.IsSessionEnd).ClosingTime;
                    sessionEndTimes.Add((int)d, endTime);
                }
                else
                {
                    sessionEndTimes.Add((int)d, TimeSpan.FromSeconds(0));
                }
            }
            return sessionEndTimes;
        }
    }
}
