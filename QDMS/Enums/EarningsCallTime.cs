// -----------------------------------------------------------------------
// <copyright file="EarningsCallTime.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;

namespace QDMS
{
    public enum EarningsCallTime
    {
        [Description("Before Market Open")]
        BeforeMarketOpen = 0,
        [Description("After Market Close")]
        AfterMarketClose = 1,
        [Description("Specific time available. See EarningsTime.")]
        SpecificTime = 2,
        [Description("Call time is not available")]
        NotAvailable = 3
    }
}
