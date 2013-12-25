// -----------------------------------------------------------------------
// <copyright file="HistoricalDataRequest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    public class HistoricalDataRequest : ICloneable
    {
        /// <summary>
        /// The frequency of the data.
        /// </summary>
        [ProtoMember(1, IsRequired = true)]
        public BarSize Frequency { get; set; }

        /// <summary>
        /// The instrument whose data you want.
        /// </summary>
        [ProtoMember(2, IsRequired = true)]
        public Instrument Instrument { get; set; }

        /// <summary>
        /// Inclusive starting date for the period requested.
        /// </summary>
        public DateTime StartingDate
        {
            get
            {
                return new DateTime(_longStartingDate);
            }
            set
            {
                _longStartingDate = value.Ticks;
            }
        }

        /// <summary>
        /// Inclusive ending date for the period requested.
        /// </summary>
        public DateTime EndingDate
        {
            get
            {
                return new DateTime(_longEndingDate);
            }
            set
            {
                _longEndingDate = value.Ticks;
            }
        }
        
        [ProtoMember(3)]
        private long _longStartingDate;

        [ProtoMember(4)]
        private long _longEndingDate;

        /// <summary>
        /// If this is true, the request will go directly to the instrument's data source.
        /// If it is false, the request will first go to local storage and only request fresh data if the local storage is unable to fulfill the request.
        /// </summary>
        [ProtoMember(5)]
        public bool ForceFreshData { get; set; }

        /// <summary>
        /// If this is true, the request will go directly to local storage and will never query the external data source.
        /// </summary>
        [ProtoMember(6)]
        public bool LocalStorageOnly { get; set; }

        /// <summary>
        /// If this is true, any data received from the external data source will be saved to local storage.
        /// </summary>
        [ProtoMember(7)]
        public bool SaveDataToStorage { get; set; }

        /// <summary>
        /// If this is true, only data from regular trading hours will be returned.
        /// </summary>
        [ProtoMember(8)]
        public bool RTHOnly { get; set; }

        /// <summary>
        /// This value is used on the client side to uniquely identify historical data requests.
        /// </summary>
        [ProtoMember(9)]
        public int RequestID { get; set; }

        /// <summary>
        /// The historical data broker gives the request an ID, which is then used to identify it when the data is returned.
        /// </summary>
        [ProtoMember(10)]
        public int AssignedID { get; set; }

        /// <summary>
        /// This property references the "parent" request's AssignedID
        /// </summary>
        [ProtoMember(11)]
        public int? IsSubrequestFor { get; set; }

        public HistoricalDataRequest()
        {
        }

        public HistoricalDataRequest(Instrument instrument, BarSize frequency, DateTime startingDate, DateTime endingDate, bool forceFreshData = false, bool localStorageOnly = false, bool saveToLocalStorage = true, bool rthOnly = false, int requestID = 0)
        {
            Frequency = frequency;
            Instrument = instrument;
            StartingDate = startingDate;
            EndingDate = endingDate;
            ForceFreshData = forceFreshData;
            LocalStorageOnly = localStorageOnly;
            SaveDataToStorage = saveToLocalStorage;
            RTHOnly = rthOnly;
            RequestID = requestID;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new HistoricalDataRequest(Instrument, Frequency, StartingDate, EndingDate, ForceFreshData, LocalStorageOnly, SaveDataToStorage, RTHOnly, RequestID);
            clone.AssignedID = AssignedID;
            clone.IsSubrequestFor = IsSubrequestFor;
            return clone;
        }
    }
}
