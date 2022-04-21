// -----------------------------------------------------------------------
// <copyright file="EconomicRelease.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using ProtoBuf;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QDMS
{
    /// <summary>
    /// Holds data on a release of economic data, eg GDP figures
    /// </summary>
    [ProtoContract]
    [Serializable]
    public class EconomicRelease
    {
        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(9)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(1)]
        [MaxLength(100)]
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// ISO 3166 2-letter country code
        /// </summary>
        [ProtoMember(2)]
        [MaxLength(2)]
        [Required]
        public string Country { get; set; }

        /// <summary>
        /// ISO 4217 3-letter currency code
        /// </summary>
        [ProtoMember(3)]
        [MaxLength(3)]
        public string Currency { get; set; }

        /// <summary>
        /// Date and time in UTC
        /// </summary>
        [ProtoMember(4)]
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Expected value
        /// </summary>
        [ProtoMember(5)]
        public double? Expected { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(6)]
        public double? Previous { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(7)]
        public double? Actual { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(8)]
        public Importance Importance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public EconomicRelease()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="country"></param>
        /// <param name="currency"></param>
        /// <param name="dateTime"></param>
        /// <param name="importance"></param>
        /// <param name="expected"></param>
        /// <param name="previous"></param>
        /// <param name="actual"></param>
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

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name} ({Country}/{Currency}) at {DateTime}. Exp: {Expected} Prev: {Previous} Act: {Actual}";
        }
    }
}
