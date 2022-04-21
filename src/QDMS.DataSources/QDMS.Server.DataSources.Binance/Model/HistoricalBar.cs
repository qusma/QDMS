// -----------------------------------------------------------------------
// <copyright file="HistoricalBar.cs" company="">
// Copyright 2018 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;

namespace QDMS.Server.DataSources.Binance.Model
{
    internal class HistoricalBar
    {
        [JsonProperty(Order = 1)]
        public long OpenTime { get; set; }

        [JsonProperty(Order = 2)]
        public decimal Open { get; set; }

        [JsonProperty(Order = 3)]
        public decimal High { get; set; }

        [JsonProperty(Order = 4)]
        public decimal Low { get; set; }

        [JsonProperty(Order = 5)]
        public decimal Close { get; set; }

        [JsonProperty(Order = 6)]
        public decimal Volume { get; set; }

        [JsonProperty(Order = 7)]
        public long CloseTime { get; set; }

        [JsonProperty(Order = 8)]
        public decimal QuoteAssetVolume { get; set; }

        [JsonProperty(Order = 9)]
        public long NoOfTrades { get; set; }

        [JsonProperty(Order = 10)]
        public decimal TakerBuyBaseAssetVolume { get; set; }

        [JsonProperty(Order = 10)]
        public decimal TakerBuyQuoteAssetVolume { get; set; }

        /// <summary>
        /// Can be ignored
        /// </summary>
        [JsonProperty(Order = 11)]
        public string Ignore { get; set; }

        public OHLCBar ToOHLCBar()
        {
            return new OHLCBar
            {
                DTOpen = MyUtils.TimestampToDateTimeByMillisecond(OpenTime),
                DT = MyUtils.TimestampToDateTimeByMillisecond(CloseTime),
                Open = Open,
                High = High,
                Low = Low,
                Close = Close,
                Volume = (long?)Volume //this is an issue, might have to change volume type?
            };
        }
    }
}