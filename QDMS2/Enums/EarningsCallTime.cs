// -----------------------------------------------------------------------
// <copyright file="EarningsCallTime.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;

namespace QDMS
{
    /// <summary>
    /// The timing of an earnings release
    /// </summary>
    public enum EarningsCallTime
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("Before Market Open")]
        BeforeMarketOpen = 0,
        /// <summary>
        /// 
        /// </summary>
        [Description("After Market Close")]
        AfterMarketClose = 1,
        /// <summary>
        /// 
        /// </summary>
        [Description("Specific time available. See EarningsTime.")]
        SpecificTime = 2,
        /// <summary>
        /// 
        /// </summary>
        [Description("Call time is not available")]
        NotAvailable = 3
    }
}
