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

        /// <summary>
        /// Gets the "opening" session's opening time, one for each day of the week.
        /// Session could potentially be on a previous day.
        /// </summary>
        public static Dictionary<int, TimeSpan> SessionStartTimesByDay(this Instrument instrument)
        {
            Dictionary<int, TimeSpan> sessionStartTimes = new Dictionary<int, TimeSpan>();
            if (instrument.Sessions == null) return sessionStartTimes;

            var dotwValues = MyUtils.GetEnumValues<DayOfTheWeek>();

            var sessions = instrument.Sessions.OrderBy(x => x.OpeningTime).ToList();

            foreach (DayOfTheWeek d in dotwValues)
            {
                if (instrument.Sessions.Any(x => x.OpeningDay == d))
                {
                    var startTime = sessions.First(x => x.ClosingDay == d).OpeningTime;
                    sessionStartTimes.Add((int)d, startTime);
                }
                else
                {
                    sessionStartTimes.Add((int)d, TimeSpan.FromSeconds(0));
                }
            }
            return sessionStartTimes;
        }
    }
}
