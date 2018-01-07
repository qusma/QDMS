// -----------------------------------------------------------------------
// <copyright file="AggTrade.cs" company="">
// Copyright 2018 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;

namespace QDMS.Server.DataSources.Binance.Model
{
    /// <summary>
    ///     Aggregated trades
    /// </summary>
    public class AggTrade
    {
        [JsonProperty("e")]
        public string EventType { get; set; }

        [JsonProperty("E")]
        public long EventTime { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; }

        [JsonProperty("a")]
        public long AggregatedTradeId { get; set; }

        [JsonProperty("p")]
        public string Price { get; set; }

        [JsonProperty("q")]
        public string Quantity { get; set; }

        [JsonProperty("f")]
        public long FirstBreakdownTradeId { get; set; }

        [JsonProperty("l")]
        public long LastBreakdownTradeId { get; set; }

        [JsonProperty("T")]
        public long TradeTime { get; set; }

        [JsonProperty("m")]
        public bool BuyerIsMaker { get; set; }

        /// <summary>
        /// Can be ignored
        /// </summary>
        [JsonProperty("M")]
        public bool M { get; set; }
    }
}