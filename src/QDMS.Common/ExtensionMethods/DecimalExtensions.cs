// -----------------------------------------------------------------------
// <copyright file="DecimalExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    /// <summary>
    /// 
    /// </summary>
    public static class DecimalExtensions
    {
        /// <summary>
        /// Returns the number of decimal places, ignoring trailing zeros.
        /// </summary>
        public static int CountDecimalPlaces(this decimal value)
        {
            return Math.Max(0, value.ToString("0.#########################").Length - Math.Truncate(value).ToString().Length - 1);
        }
    }
}
