// -----------------------------------------------------------------------
// <copyright file="StatsExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace QDMS
{
    /// <summary>
    /// Statistical utils
    /// </summary>
    public static class StatsExtensions
    {
        /// <summary>
        /// StDev
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double QDMSStandardDeviation(this IEnumerable<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }

        /// <summary>
        /// StDev
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double QDMSStandardDeviation(this IEnumerable<decimal> values)
        {
            decimal avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow((double)(v - avg), 2)));
        }
    }
}
