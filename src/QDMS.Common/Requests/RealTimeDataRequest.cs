// -----------------------------------------------------------------------
// <copyright file="RealTimeDataRequest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using ProtoBuf;
using System;

namespace QDMS
{
    /// <summary>
    /// Request to start a real time data stream
    /// </summary>
    [ProtoContract]
    public class RealTimeDataRequest : ICloneable
    {
        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(1)]
        public BarSize Frequency { get; set; }

        /// <summary>
        /// Regular trading hours data only.
        /// </summary>
        [ProtoMember(2)]
        public bool RTHOnly { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(3)]
        public Instrument Instrument { get; set; }

        /// <summary>
        /// Save incoming data to local storage.
        /// </summary>
        [ProtoMember(4)]
        public bool SaveToLocalStorage { get; set; }

        /// <summary>
        /// If the data stream for the requested instrument fails, fall back to this one instead.
        /// </summary>
        [ProtoMember(5)]
        public Instrument FallBack { get; set; }

        /// <summary>
        /// This value is used on the client side to uniquely identify real time data requests.
        /// </summary>
        [ProtoMember(6)]
        public int RequestID { get; set; }

        /// <summary>
        /// The real time data broker gives the request an ID, which is then used to identify it when the data is returned.
        /// </summary>
        public int AssignedID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RealTimeDataRequest()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="frequency"></param>
        /// <param name="rthOnly"></param>
        /// <param name="savetoLocalStorage"></param>
        public RealTimeDataRequest(Instrument instrument, BarSize frequency, bool rthOnly = true, bool savetoLocalStorage = false)
        {
            Instrument = instrument;
            Frequency = frequency;
            RTHOnly = rthOnly;
            SaveToLocalStorage = savetoLocalStorage;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new RealTimeDataRequest(Instrument, Frequency, RTHOnly, SaveToLocalStorage);
            clone.FallBack = FallBack;
            clone.RequestID = RequestID;
            clone.AssignedID = AssignedID;
            return clone;
        }
    }
}
