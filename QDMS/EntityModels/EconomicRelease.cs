// -----------------------------------------------------------------------
// <copyright file="EconomicRelease.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    [Serializable]
    public class EconomicRelease
    {
        [ProtoMember(9)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ProtoMember(1)]
        [MaxLength(100)]
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// ISO 2-letter country code
        /// </summary>
        [ProtoMember(2)]
        [MaxLength(2)]
        [Required]
        public string Country { get; set; }

        /// <summary>
        /// ISO 3-letter currency code
        /// </summary>
        [ProtoMember(3)]
        [MaxLength(3)]
        public string Currency { get; set; }

        /// <summary>
        /// Date and time in UTC
        /// </summary>
        [ProtoMember(4)]
        public DateTime DateTime { get; set; }

        [ProtoMember(5)]
        public double? Expected { get; set; }

        [ProtoMember(6)]
        public double? Previous { get; set; }

        [ProtoMember(7)]
        public double? Actual { get; set; }

        [ProtoMember(8)]
        public Importance Importance { get; set; }

        public EconomicRelease()
        {
        }

        public EconomicRelease(string name, string country, string currency, DateTime dateTime, Importance importance, double? expected, double? previous, double? actual)
        {
            Name = name;
            Country = country;
            Currency = currency;
            DateTime = dateTime;
            Importance = importance;
            Expected = expected;
            Previous = previous;
            Actual = actual;
        }

        public override string ToString()
        {
            return $"{Name} ({Country}/{Currency}) at {DateTime}. Exp: {Expected} Prev: {Previous} Act: {Actual}";
        }
    }
}
