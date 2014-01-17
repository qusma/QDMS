// -----------------------------------------------------------------------
// <copyright file="SessionTemplate.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace QDMS
{

    public class SessionTemplate : ICloneable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }

        public virtual ICollection<TemplateSession> Sessions { get; set; }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return new SessionTemplate { ID = ID, Name = Name, Sessions = Sessions == null ? null : Sessions.Select(x => (TemplateSession)x.Clone()).ToList() };
        }
    }
}
