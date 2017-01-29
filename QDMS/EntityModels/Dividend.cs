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
    public class Dividend
    {
        public decimal Amount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; }

        public DateTime? DeclarationDate { get; set; }

        [Index]
        public DateTime ExDate { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public DateTime? PaymentDate { get; set; }

        public DateTime? RecordDate { get; set; }

        [Index]
        [MaxLength(20)]
        public string Symbol { get; set; }

        [MaxLength(50)]
        public string Type { get; set; }

        public override string ToString()
        {
            return $"{Symbol} {Amount} ExDate: {ExDate}";
        }
    }
}