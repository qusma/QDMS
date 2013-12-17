// -----------------------------------------------------------------------
// <copyright file="RealTimeDataRequest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    public class RealTimeDataRequest
    {
        [ProtoMember(1)]
        public BarSize Frequency { get; set; }

        /// <summary>
        /// Regular trading hours data only.
        /// </summary>
        [ProtoMember(2)]
        public bool RTHOnly { get; set; }

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

        public RealTimeDataRequest()
        {
        }

        public RealTimeDataRequest(Instrument instrument, BarSize frequency, bool rthOnly = true, bool savetoLocalStorage = false)
        {
            Instrument = instrument;
            Frequency = frequency;
            RTHOnly = rthOnly;
            SaveToLocalStorage = savetoLocalStorage;
        }
    }
}
