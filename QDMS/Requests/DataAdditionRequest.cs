// -----------------------------------------------------------------------
// <copyright file="DataAdditionRequest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ProtoBuf;

namespace QDMS
{
    /// <summary>
    /// A request to add data to the database
    /// </summary>
    [ProtoContract]
    public class DataAdditionRequest
    {
        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(1)]
        public BarSize Frequency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(2)]
        public Instrument Instrument { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(3)]
        public List<OHLCBar> Data { get; set; }

        /// <summary>
        /// If set to true, will overwrite existing data
        /// </summary>
        [ProtoMember(4)]
        public bool Overwrite { get; set; }

        /// <summary>
        /// If set to true, all adjusted values will be re-calculated
        /// </summary>
        public bool AdjustData { get; set; }

        /// <summary>
        /// For serialization use
        /// </summary>
        public DataAdditionRequest()
        {
            Data = new List<OHLCBar>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="instrument"></param>
        /// <param name="data"></param>
        /// <param name="overwrite"></param>
        /// <param name="adjust"></param>
        public DataAdditionRequest(BarSize frequency, Instrument instrument, List<OHLCBar> data, bool overwrite = true, bool adjust = false)
        {
            Data = data;
            Frequency = frequency;
            Instrument = instrument;
            Overwrite = overwrite;
            AdjustData = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (string.Format("{0} bars @ {1}, instrument: {2}. {3} {4}",
                Data.Count,
                Frequency,
                Instrument,
                Overwrite ? "Overwrite" : "",
                AdjustData ? "Adjust" : ""));
        }
    }
}
