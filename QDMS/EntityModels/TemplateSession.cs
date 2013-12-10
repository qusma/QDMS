// -----------------------------------------------------------------------
// <copyright file="TemplateSession.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProtoBuf;

namespace QDMS
{
    public class TemplateSession : ICloneable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ProtoMember(1)]
        public int ID { get; set; }

        public TimeSpan OpeningTime { get; set; }

        public TimeSpan ClosingTime { get; set; }

        [ProtoMember(2)]
        public int TemplateID { get; set; }

        public virtual SessionTemplate Template { get; set; }

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
    }
}
