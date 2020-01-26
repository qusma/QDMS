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
        /// 
        /// </summary>
        [ProtoMember(1)]
        public decimal Open { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(2)]
        public decimal High { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(3)]
        public decimal Low { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(4)]
        public decimal Close { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(5)]
        public decimal? AdjOpen { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(6)]
        public decimal? AdjHigh { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(7)]
        public decimal? AdjLow { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(8)]
        public decimal? AdjClose { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [NotMapped]
        public LocalDateTime Date => new LocalDateTime(DT.Year, DT.Month, DT.Day, DT.Hour, DT.Minute, DT.Second, DT.Millisecond);

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(9)]
        [NotMapped]
        public long LongDate
        {
            get => DT.Ticks;
            set => DT = DateTime.FromBinary(value);
        }

        /// <summary>
        /// 
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
        /// Date/Time of the bar open.
        /// </summary>
        public DateTime? DTOpen { get; set; }

        /// <summary>
        /// Date/Time of the bar close.
        /// </summary>
        public DateTime DT { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(10)]
        public long? Volume { get; set; }

        /// <summary>
        /// Total number of outstanding contracts for this instrument
        /// </summary>
        [ProtoMember(11)]
        public int? OpenInterest { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(12)]
        public int InstrumentID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(13)]
        public decimal? Dividend { get; set; }

        /// <summary>
        /// Stock splits
        /// </summary>
        [ProtoMember(14)]
        public decimal? Split { get; set; }

        /// <summary>
        /// 
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
