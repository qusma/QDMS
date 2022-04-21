// -----------------------------------------------------------------------
// <copyright file="ContinuousFutureConfig.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity.ModelConfiguration;
using QDMS;

namespace EntityData
{
    public class ContinuousFutureConfig : EntityTypeConfiguration<ContinuousFuture>
    {
        public ContinuousFutureConfig()
        {
            this.HasRequired(x => x.UnderlyingSymbol)
                .WithMany()
                .HasForeignKey(x => x.UnderlyingSymbolID)
                .WillCascadeOnDelete(true);
        }
    }
}
