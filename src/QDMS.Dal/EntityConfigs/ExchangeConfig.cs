// -----------------------------------------------------------------------
// <copyright file="ExchangeConfig.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity.ModelConfiguration;
using QDMS;

namespace EntityData
{
    public class ExchangeConfig : EntityTypeConfiguration<Exchange>
    {
        public ExchangeConfig()
        {
            this.HasMany(x => x.Sessions)
            .WithRequired()
            .HasForeignKey(x => x.ExchangeID)
            .WillCascadeOnDelete(true);
        }
    }
}
