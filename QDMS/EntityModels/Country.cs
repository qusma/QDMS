// -----------------------------------------------------------------------
// <copyright file="Country.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QDMS
{
    public class Country
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(100)]
        [Index("IX_Country", IsUnique = true)]
        public string Name { get; set; }

        /// <summary>
        /// ISO 3166 2-letter country code
        /// </summary>
        [MaxLength(2)]
        public string CountryCode { get; set; }

        /// <summary>
        /// ISO 4217 3-letter currency code
        /// </summary>
        [MaxLength(3)]
        public string CurrencyCode { get; set; }
    }
}