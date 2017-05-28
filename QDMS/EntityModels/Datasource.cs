// -----------------------------------------------------------------------
// <copyright file="Datasource.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    public class Datasource : IEntity, IEquatable<Datasource>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ProtoMember(1)]
        public int ID { get; set; }

        [ProtoMember(2)]
        [MaxLength(100)]
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(Datasource other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ID == other.ID && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Datasource) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ID * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }
}
