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
    /// <summary>
    /// 
    /// </summary>
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
            }
            return sessionEndTimes;
        }

        /// <summary>
        /// Gets the "opening" session's opening time, one for each day of the week.
        /// Session could potentially be on a previous day.
        /// </summary>
        /// <returns>
        /// A dictionary with keys corresponding to DayOfTheWeek,
        /// and values of the opening session for that day.
        /// </returns>
        public static Dictionary<int, InstrumentSession> SessionStartTimesByDay(this Instrument instrument)
        {
            var sessionStartTimes = new Dictionary<int, InstrumentSession>();
            if (instrument.Sessions == null) return sessionStartTimes;

            var dotwValues = MyUtils.GetEnumValues<DayOfTheWeek>();

            var sessions = instrument.Sessions.OrderBy(x => x.OpeningTime).ToList();

            foreach (DayOfTheWeek d in dotwValues)
            {
                if (sessions.Any(x => x.ClosingDay == d))
                {
                    //if there's a session starting on a different day,
                    //that's the earliest one no matter the time
                    InstrumentSession prevDaySession = sessions.FirstOrDefault(x => x.ClosingDay == d && x.OpeningDay != d);
                    if(prevDaySession != null)
                    {
                        sessionStartTimes.Add((int)d, prevDaySession);
                    }
                    else
                    {
                        var session = sessions.First(x => x.ClosingDay == d);
                        sessionStartTimes.Add((int)d, session);
                    }
                }
            }
            return sessionStartTimes;
        }
    }
}
