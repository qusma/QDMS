// -----------------------------------------------------------------------
// <copyright file="Exchange.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    [Serializable]
    public class Exchange : ICloneable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ProtoMember(1)]
        public int ID { get; set; }

        [ProtoMember(2)]
        [MaxLength(100)]
        public string Name { get; set; }

        [ProtoMember(3)]
        [MaxLength(255)]
        public string Timezone { get; set; }

        [ProtoMember(4)]
        public virtual ICollection<ExchangeSession> Sessions { get; set; }

        [ProtoMember(5)]
        [MaxLength(255)]
        public string LongName { get; set; }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new Exchange();
            clone.ID = ID;
            clone.Name = Name;
            clone.Timezone = Timezone;
            clone.Sessions = Sessions == null ? null : new List<ExchangeSession>(Sessions.Select(x => (ExchangeSession)x.Clone()));
            clone.LongName = LongName;
            return clone;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} ({2}) TZ: {3}",
                ID,
                Name,
                LongName,
                Timezone);
        }
    }
}
