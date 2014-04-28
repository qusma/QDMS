// -----------------------------------------------------------------------
// <copyright file="ISession.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public interface ISession : ICloneable
    {
        int ID { get; set; }

        TimeSpan OpeningTime { get; set; }
        TimeSpan ClosingTime { get; set; }

        double OpeningAsSeconds { get; set; }

        double ClosingAsSeconds { get; set; }

        bool IsSessionEnd { get; set; }

        DayOfTheWeek OpeningDay { get; set; }

        DayOfTheWeek ClosingDay { get; set; }
    }
}
