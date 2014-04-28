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
    }
}
