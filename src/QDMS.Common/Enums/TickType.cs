// -----------------------------------------------------------------------
// <copyright file="TickType.cs" company="">
// Copyright 2018 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QDMS
{
    /// <summary>
    ///     Type of tick event
    /// </summary>
    public enum TickType
    {
        /// <summary>
        ///     Trade
        /// </summary>
        Trade,

        /// <summary>
        ///     Trade at the national best price
        /// </summary>
        TradeNb,

        /// <summary>
        ///     Change in the best bid
        /// </summary>
        QuoteBid,

        /// <summary>
        ///     Change in the national best bid
        /// </summary>
        QuoteBidNb,

        /// <summary>
        ///     Change in the best ask
        /// </summary>
        QuoteAsk,

        /// <summary>
        ///     Change in the national best ask
        /// </summary>
        QuoteAskNb
    }
}