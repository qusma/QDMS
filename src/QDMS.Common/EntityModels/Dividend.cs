// -----------------------------------------------------------------------
// <copyright file="Dividend.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QDMS
{
    /// <summary>
    /// Holds data on one particular dividend payout
    /// </summary>
    public class Dividend
    {
        /// <summary>
        /// 
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(3)]
        public string Currency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? DeclarationDate { get; set; }

        /// <summary>
        /// Cut-off date for receiving the dividend
        /// </summary>
        public DateTime ExDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? RecordDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(20)]
        public string Symbol { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(50)]
        public string Type { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Symbol} {Amount} ExDate: {ExDate}";
        }
    }
}