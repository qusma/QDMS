// -----------------------------------------------------------------------
// <copyright file="StoredDataInfo.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
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
        /// Instrument id
        /// </summary>
        [ProtoMember(1)]
        public int InstrumentID { get; set; }

        /// <summary>
        /// The data frequency
        /// </summary>
        [ProtoMember(2)]
        public BarSize Frequency { get; set; }

        /// <summary>
        /// Starting date for data
        /// </summary>
        public DateTime EarliestDate { get; set; }

        /// <summary>
        /// Ending date for data
        /// </summary>
        public DateTime LatestDate { get; set; }

        /// <summary>
        /// Date as long
        /// </summary>
        [ProtoMember(3)]
        [NotMapped]
        [JsonIgnore]
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
        /// Date as long
        /// </summary>
        [ProtoMember(4)]
        [NotMapped]
        [JsonIgnore]
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
