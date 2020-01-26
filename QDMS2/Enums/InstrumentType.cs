// -----------------------------------------------------------------------
// <copyright file="InstrumentType.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    /// <summary>
    /// Order type.
    /// </summary>
    [Serializable]
    public enum InstrumentType : int
    {
        /// <summary>
        /// Stock
        /// </summary>
        [Description("STK")]
        Stock = 0,
        /// <summary>
        /// Option
        /// </summary>
        [Description("OPT")]
        Option = 1,
        /// <summary>
        /// Future
        /// </summary>
        [Description("FUT")]
        Future = 2,
        /// <summary>
        /// Indice
        /// </summary>
        [Description("IND")]
        Index = 3,
        /// <summary>
        /// FOP = options on futures
        /// </summary>
        [Description("FOP")]
        FutureOption = 4,
        /// <summary>
        /// Cash
        /// </summary>
        [Description("CASH")]
        Cash = 5,
        /// <summary>
        /// For Combination Orders - must use combo leg details
        /// </summary>
        [Description("BAG")]
        Bag = 6,
        /// <summary>
        /// Bond
        /// </summary>
        [Description("BOND")]
        Bond = 7,
        /// <summary>
        /// Warrant
        /// </summary>
        [Description("WAR")]
        Warrant = 8,
        /// <summary>
        /// Commodity
        /// </summary>
        [Description("CMDTY")]
        Commodity = 9,
        /// <summary>
        /// Bill
        /// </summary>
        [Description("BILL")]
        Bill = 10,
        /// <summary>
        /// CFD
        /// </summary>
        [Description("CFD")]
        CFD = 11,
        /// <summary>
        /// Undefined Security Type
        /// </summary>
        [Description("")]
        Undefined = 12,
        /// <summary>
        /// Backtest result
        /// </summary>
        [Description("Backtest")]
        Backtest = 13,
        /// <summary>
        /// Cryptocurrency
        /// </summary>
        [Description("Crypto")]
        CryptoCurrency = 14,
        /// <summary>
        /// Fund
        /// </summary>
        [Description("FUND")]
        Fund = 15
    }

}

