// -----------------------------------------------------------------------
// <copyright file="StoredDataInfo.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations.Schema;
using ProtoBuf;

namespace QDMS
{
    /// <summary>
    /// Holds information on what data is available in local storage
    /// </summary>
    [ProtoContract]
    public class StoredDataInfo
    {
        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(1)]
        public int InstrumentID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(2)]
        public BarSize Frequency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime EarliestDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime LatestDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(3)]
        [NotMapped]
        public long EarliestDateAsLong
        {
            get
            {
                return EarliestDate.Ticks;
            }
            set
            {
                EarliestDate = DateTime.FromBinary(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(4)]
        [NotMapped]
        public long LatestDateAsLong
        {
            get
            {
                return LatestDate.Ticks;
            }
            set
            {
                LatestDate = DateTime.FromBinary(value);
            }
        }
    }
}
