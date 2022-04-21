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

    /// <summary>
    /// A template from which instruments can inherit their trading sessions
    /// </summary>
    public class SessionTemplate : ICloneable, IEntity
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
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
