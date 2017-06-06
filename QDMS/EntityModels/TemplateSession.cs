// -----------------------------------------------------------------------
// <copyright file="TemplateSession.cs" company="">
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
    [ProtoContract]
    public class TemplateSession : ISession, IEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ProtoMember(1)]
        public int ID { get; set; }

        [JsonIgnore]
        public TimeSpan OpeningTime { get; set; }

        [JsonIgnore]
        public TimeSpan ClosingTime { get; set; }

        [ProtoMember(2)]
        public int TemplateID { get; set; }

        [JsonIgnore]
        public virtual SessionTemplate Template { get; set; }

        [ProtoMember(3)]
        [NotMapped]
        public double OpeningAsSeconds
        {
            get => OpeningTime.TotalSeconds;
            set => OpeningTime = TimeSpan.FromSeconds(value);
        }

        [ProtoMember(4)]
        [NotMapped]
        public double ClosingAsSeconds
        {
            get => ClosingTime.TotalSeconds;
            set => ClosingTime = TimeSpan.FromSeconds(value);
        }


        [ProtoMember(5)]
        public bool IsSessionEnd { get; set; }

        [ProtoMember(6)]
        public DayOfTheWeek OpeningDay { get; set; }

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
            return new TemplateSession
            {
                ID = ID,
                OpeningTime = TimeSpan.FromSeconds(OpeningTime.TotalSeconds),
                ClosingTime = TimeSpan.FromSeconds(ClosingTime.TotalSeconds),
                TemplateID = TemplateID,
                IsSessionEnd = IsSessionEnd,
                OpeningDay = OpeningDay,
                ClosingDay = ClosingDay
            };
        }

        public override string ToString()
        {
            return $"{OpeningDay} {OpeningTime} - {ClosingDay} {ClosingTime}";
        }
    }
}
