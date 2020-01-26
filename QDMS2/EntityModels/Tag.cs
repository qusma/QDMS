﻿// -----------------------------------------------------------------------
// <copyright file="Tag.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProtoBuf;

namespace QDMS
{
    /// <summary>
    /// Instrument tags used for categorization etc
    /// </summary>
    [ProtoContract]
    public class Tag : ICloneable, IEquatable<Tag>, IEntity
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ProtoMember(1)]
        public int ID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(2)]
        [MaxLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return new Tag { ID = ID, Name = Name };
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Tag other)
        {
            if (ReferenceEquals(other, null)) return false;
            return other.ID == ID && other.Name == Name;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }
}
