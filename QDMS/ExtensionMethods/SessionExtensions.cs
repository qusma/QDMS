// -----------------------------------------------------------------------
// <copyright file="ISessionExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public static class SessionExtensions
    {
        public static InstrumentSession ToInstrumentSession(this ISession session)
        {
            var result = new InstrumentSession();
            result.OpeningDay = session.OpeningDay;
            result.OpeningTime = TimeSpan.FromSeconds(session.OpeningTime.TotalSeconds);
            result.ClosingDay = session.ClosingDay;
            result.ClosingTime = TimeSpan.FromSeconds(session.ClosingTime.TotalSeconds);
            result.IsSessionEnd = session.IsSessionEnd;
            return result;
        }

        public static bool Overlaps(this ISession session1, ISession session2)
        {
            //session 1 engulfs session 2
            if (session1.OpeningDay < session2.OpeningDay &&
                session1.ClosingDay > session2.ClosingDay &&
                session2.OpeningDay < session2.ClosingDay)
            {
                return true;
            }

            //session 2 engulfs session 1
            if (session2.OpeningDay < session1.OpeningDay &&
                session2.ClosingDay > session1.ClosingDay &&
                session1.OpeningDay < session1.ClosingDay)
            {
                return true;
            }

            //session 1 engulfs session 2, across weeks
            if (session1.OpeningDay > session1.ClosingDay &&
                session1.OpeningDay > session2.OpeningDay &&
                session1.ClosingDay > session2.ClosingDay)
            {
                return true;
            }

            //session 2 engulfs session 1, across weeks
            if (session2.OpeningDay > session2.ClosingDay &&
                session2.OpeningDay > session1.OpeningDay &&
                session2.ClosingDay > session1.ClosingDay)
            {
                return true;
            }

            //partial overlap intraweek, 1 over 2
            if (session1.OpeningDay < session2.ClosingDay &&
                session1.ClosingDay > session2.ClosingDay)
            {
                return true;
            }

            //partial overlap intraweek, 2 over 1
            if (session2.OpeningDay < session1.ClosingDay &&
                session2.ClosingDay > session1.ClosingDay)
            {
                return true;
            }

            //same day, times overlap
            if (session1.OpeningDay == session2.OpeningDay &&
                session1.ClosingDay == session2.ClosingDay)
            {
                if (session1.OpeningTime > session2.OpeningTime && session1.ClosingTime < session2.ClosingTime)
                    return true;
                if (session1.OpeningTime < session2.OpeningTime && session1.ClosingTime > session2.OpeningTime)
                    return true;
                if (session1.OpeningTime < session2.ClosingTime && session1.ClosingTime > session2.ClosingTime)
                    return true;

                if (session2.OpeningTime > session1.OpeningTime && session2.ClosingTime < session1.ClosingTime)
                    return true;
                if (session2.OpeningTime < session1.OpeningTime && session2.ClosingTime > session1.OpeningTime)
                    return true;
                if (session2.OpeningTime < session1.ClosingTime && session2.ClosingTime > session1.ClosingTime)
                    return true;
            }

            return false;
        }
    }
}
