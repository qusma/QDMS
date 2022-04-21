// -----------------------------------------------------------------------
// <copyright file="Kline.cs" company="">
// Copyright 2018 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;

namespace QDMS.Server.DataSources.Binance.Model
{
    public class Kline
    {
        [JsonProperty("e")]
        public string EventType { get; set; }

        [JsonProperty("E")]
        public long EventTime { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; }

        [JsonProperty("k")]
        public BinanceBar Bar { get; set; }
    }

    public class BinanceBar
    {
        [JsonProperty("t")]
        public long StartTime { get; set; }

        [JsonProperty("T")]
        public long EndTime { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; }

        /// <summary>
        ///     Available intervals: 1m, 3m, 5m, 15m, 30m, 1h, 2h, 4h, 6h, 8h, 12h, 1d, 3d, 1w, 1M
        /// </summary>
        [JsonProperty("i")]
        public string Interval { get; set; }

        [JsonProperty("f")]
        public long FirstTradeId { get; set; }

        [JsonProperty("L")]
        public long LastTradeId { get; set; }

        [JsonProperty("o")]
        public decimal Open { get; set; }

        [JsonProperty("c")]
        public decimal Close { get; set; }

        [JsonProperty("h")]
        public decimal High { get; set; }

        [JsonProperty("l")]
        public decimal Low { get; set; }

        [JsonProperty("v")]
        public decimal Volume { get; set; }

        [JsonProperty("n")]
        public long NumberOfTrades { get; set; }

        [JsonProperty("x")]
        public bool IsFinal { get; set; }

        [JsonProperty("q")]
        public string QuoteVolume { get; set; }

        [JsonProperty("V")]
        public string ActiveBuyVolume { get; set; }

        [JsonProperty("Q")]
        public string QuoteVolumeOfActiveBuy { get; set; }

        /// <summary>
        ///     Can be ignored
        /// </summary>
        [JsonProperty("B")]
        public string B { get; set; }
    }
}