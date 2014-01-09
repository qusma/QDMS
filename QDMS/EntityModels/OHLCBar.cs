// -----------------------------------------------------------------------
// <copyright file="OHLCBar.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;
using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    public class OHLCBar
    {
        [ProtoMember(1)]
        public decimal Open { get; set; }

        [ProtoMember(2)]
        public decimal High { get; set; }

        [ProtoMember(3)]
        public decimal Low { get; set; }

        [ProtoMember(4)]
        public decimal Close { get; set; }

        [ProtoMember(5)]
        public decimal? AdjOpen { get; set; }

        [ProtoMember(6)]
        public decimal? AdjHigh { get; set; }

        [ProtoMember(7)]
        public decimal? AdjLow { get; set; }

        [ProtoMember(8)]
        public decimal? AdjClose { get; set; }

        [NotMapped]
        public LocalDateTime Date
        {
            get
            {
                return new LocalDateTime(DT.Year, DT.Month, DT.Day, DT.Hour, DT.Minute, DT.Second, DT.Millisecond);
            }
        }

        [ProtoMember(9)]
        [NotMapped]
        public long LongDate
        {
            get
            {
                return DT.Ticks;
            }
            set
            {
                DT = DateTime.FromBinary(value);
            }
        }

        public DateTime DT { get; set; }

        [ProtoMember(10)]
        public long? Volume { get; set; }

        [ProtoMember(11)]
        public int? OpenInterest { get; set; }

        [ProtoMember(12)]
        public int InstrumentID { get; set; }

        [ProtoMember(13)]
        public decimal? Dividend { get; set; }

        [ProtoMember(14)]
        public decimal? Split { get; set; }

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
