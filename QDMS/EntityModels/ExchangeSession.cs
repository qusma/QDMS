// -----------------------------------------------------------------------
// <copyright file="ExchangeSession.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using ProtoBuf;

namespace QDMS
{
    /// <summary>
    /// A trading session (opening and closing times) for a particular trading venue
    /// </summary>
    [ProtoContract]
    public class ExchangeSession : ISession, IEntity
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ProtoMember(1)]
        public int ID { get; set; }

        /// <inheritdoc />
        [JsonIgnore]
        public TimeSpan OpeningTime { get; set; }

        /// <inheritdoc />
        [JsonIgnore]
        public TimeSpan ClosingTime { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(2)]
        public int ExchangeID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public virtual Exchange Exchange { get; set; }

        /// <inheritdoc />
        [ProtoMember(3)]
        [NotMapped]
        public double OpeningAsSeconds
        {
            get
            {
                return OpeningTime.TotalSeconds;
            }
            set
            {
                OpeningTime = TimeSpan.FromSeconds(value);
            }
        }

        /// <inheritdoc />
        [ProtoMember(4)]
        [NotMapped]
        public double ClosingAsSeconds
        {
            get
            {
                return ClosingTime.TotalSeconds;
            }
            set
            {
                ClosingTime = TimeSpan.FromSeconds(value);
            }
        }


        /// <inheritdoc />
        [ProtoMember(5)]
        public bool IsSessionEnd { get; set; }

        /// <inheritdoc />
        [ProtoMember(6)]
        public DayOfTheWeek OpeningDay { get; set; }

        /// <inheritdoc />
        [ProtoMember(7)]
        public DayOfTheWeek ClosingDay { get; set; }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return new ExchangeSession
            {
                ID = ID,
                OpeningTime = TimeSpan.FromSeconds(OpeningTime.TotalSeconds),
                ClosingTime = TimeSpan.FromSeconds(ClosingTime.TotalSeconds),
                ExchangeID = ExchangeID,
                IsSessionEnd = IsSessionEnd,
                OpeningDay = OpeningDay,
                ClosingDay = ClosingDay
            };
        }
    }
}
