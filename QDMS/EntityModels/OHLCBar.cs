// -----------------------------------------------------------------------
// <copyright file="OHLCBar.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;
using ProtoBuf;

namespace QDMS
{
    /// <summary>
    /// An open-high-low-close bar
    /// </summary>
    [ProtoContract]
    public class OHLCBar
    {
        /// <summary>
        /// Opening price
        /// </summary>
        [ProtoMember(1)]
        public decimal Open { get; set; }

        /// <summary>
        /// High price
        /// </summary>
        [ProtoMember(2)]
        public decimal High { get; set; }

        /// <summary>
        /// Low price
        /// </summary>
        [ProtoMember(3)]
        public decimal Low { get; set; }

        /// <summary>
        /// Closing price
        /// </summary>
        [ProtoMember(4)]
        public decimal Close { get; set; }

        /// <summary>
        /// Adjusted opening price
        /// </summary>
        [ProtoMember(5)]
        public decimal? AdjOpen { get; set; }

        /// <summary>
        /// Adjusted high price
        /// </summary>
        [ProtoMember(6)]
        public decimal? AdjHigh { get; set; }

        /// <summary>
        /// Adjusted low price
        /// </summary>
        [ProtoMember(7)]
        public decimal? AdjLow { get; set; }

        /// <summary>
        /// Adjusted closing price
        /// </summary>
        [ProtoMember(8)]
        public decimal? AdjClose { get; set; }

        /// <summary>
        /// Closing datetime
        /// </summary>
        [NotMapped]
        public LocalDateTime Date => new LocalDateTime(DT.Year, DT.Month, DT.Day, DT.Hour, DT.Minute, DT.Second, DT.Millisecond);

        /// <summary>
        /// Closing datetime in ticks
        /// </summary>
        [ProtoMember(9)]
        [NotMapped]
        public long LongDate
        {
            get => DT.Ticks;
            set => DT = DateTime.FromBinary(value);
        }

        /// <summary>
        /// Opening datetime in ticks
        /// </summary>
        [ProtoMember(91)]
        [NotMapped]
        public long? LongOpenDate
        {
            get => DTOpen.HasValue ? DTOpen.Value.Ticks : (long?)null;
            set =>
                DTOpen = value.HasValue
                    ? DateTime.FromBinary(value.Value)
                    : (DateTime?)null;
        }

        /// <summary>
        /// Date/Time of the bar open. Should be in UTC.
        /// </summary>
        public DateTime? DTOpen { get; set; }

        /// <summary>
        /// Date/Time of the bar close. Should be in UTC.
        /// </summary>
        public DateTime DT { get; set; }

        /// <summary>
        /// Volume
        /// </summary>
        [ProtoMember(10)]
        public long? Volume { get; set; }

        /// <summary>
        /// Total number of outstanding contracts for this instrument
        /// </summary>
        [ProtoMember(11)]
        public int? OpenInterest { get; set; }

        /// <summary>
        /// Instrument ID
        /// </summary>
        [ProtoMember(12)]
        public int InstrumentID { get; set; }

        /// <summary>
        /// Dividend on this day
        /// </summary>
        [ProtoMember(13)]
        public decimal? Dividend { get; set; }

        /// <summary>
        /// Stock splits
        /// </summary>
        [ProtoMember(14)]
        public decimal? Split { get; set; }

        /// <summary>
        /// Frequency
        /// </summary>
        public BarSize Frequency { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0} - O: {1} H: {2} L: {3} C: {4} {5}",
                DT.ToString("yyyy-MM-dd HH:mm:ss"),
                Open,
                High,
                Low,
                Close,
                Dividend.HasValue ? "Div: " + Dividend.Value : "");
        }
    }
}
