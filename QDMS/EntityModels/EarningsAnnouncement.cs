// -----------------------------------------------------------------------
// <copyright file="EarningsAnnouncement.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QDMS
{
    public class EarningsAnnouncement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Average estimation of the earnings per share before release
        /// </summary>
        public double? Forecast { get; set; }

        /// <summary>
        /// The actual reported value of earnings per share
        /// </summary>
        public double? EarningsPerShare { get; set; }

        [MaxLength(25)]
        public string Symbol { get; set; }

        [MaxLength(100)]
        public string CompanyName { get; set; }

        public DateTime Date { get; set; }

        public EarningsCallTime EarningsCallTime { get; set; }

        /// <summary>
        /// If EarningsCallTime is set to SpecificTime, this property will contain the precise time
        /// </summary>
        public DateTime? EarningsTime { get; set; }
    }
}
