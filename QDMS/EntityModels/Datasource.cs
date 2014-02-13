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
    [Serializable]
    public class Datasource
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
    }
}
