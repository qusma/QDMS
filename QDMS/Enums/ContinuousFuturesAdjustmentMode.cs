// -----------------------------------------------------------------------
// <copyright file="ContinuousFuturesAdjustmentMode.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    /// <summary>
    /// How to adjust underlying data in order to form continuous futures
    /// </summary>
    [Serializable]
    public enum ContinuousFuturesAdjustmentMode
    {
        /// <summary>
        /// No Adjustment
        /// </summary>
        [Description("No Adjustment")]
        NoAdjustment = 0,
        /// <summary>
        /// Ratio
        /// </summary>
        [Description("Ratio")]
        Ratio = 1,
        /// <summary>
        /// Difference
        /// </summary>
        [Description("Difference")]
        Difference = 2
    }
}
