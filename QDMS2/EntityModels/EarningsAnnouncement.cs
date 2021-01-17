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
    /// <summary>
    /// Holds data for a specific earnings announcement
    /// </summary>
    public class EarningsAnnouncement
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Average estimation of the earnings per share before release
        /// </summary>
        public decimal? Forecast { get; set; }

        /// <summary>
        /// The actual reported value of earnings per share
        /// </summary>
        public decimal? EarningsPerShare { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(25)]
        public string Symbol { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(100)]
        public string CompanyName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// When the earnings are released
        /// </summary>
        public EarningsCallTime EarningsCallTime { get; set; }

        /// <summary>
        /// If EarningsCallTime is set to SpecificTime, this property will contain the precise time
        /// </summary>
        public DateTime? EarningsTime { get; set; }
        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Symbol} {EarningsPerShare} On: {Date}";
        }
    }
}
