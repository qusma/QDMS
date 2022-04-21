// -----------------------------------------------------------------------
// <copyright file="SessionTemplateConfig.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity.ModelConfiguration;
using QDMS;

namespace EntityData
{
    public class SessionTemplateConfig : EntityTypeConfiguration<SessionTemplate>
    {
        public SessionTemplateConfig()
        {
            this.HasMany(x => x.Sessions)
            .WithRequired()
            .HasForeignKey(x => x.TemplateID)
            .WillCascadeOnDelete(true);
        }
    }
}
