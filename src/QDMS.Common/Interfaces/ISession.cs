// -----------------------------------------------------------------------
// <copyright file="ISession.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    /// <summary>
    /// Interface for sessions
    /// </summary>
    public interface ISession : ICloneable
    {
        /// <summary>
        /// 
        /// </summary>
        int ID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        TimeSpan OpeningTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        TimeSpan ClosingTime { get; set; }

        /// <summary>
        /// 
        /// </summary>
        double OpeningAsSeconds { get; set; }

        /// <summary>
        /// 
        /// </summary>
        double ClosingAsSeconds { get; set; }

        /// <summary>
        /// Is this the final session of the day? (some venues have morning and afternoon sessions)
        /// </summary>
        bool IsSessionEnd { get; set; }

        /// <summary>
        /// 
        /// </summary>
        DayOfTheWeek OpeningDay { get; set; }

        /// <summary>
        /// 
        /// </summary>
        DayOfTheWeek ClosingDay { get; set; }
    }
}
