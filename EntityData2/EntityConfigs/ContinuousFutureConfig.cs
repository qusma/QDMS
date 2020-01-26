// -----------------------------------------------------------------------
// <copyright file="ContinuousFutureConfig.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QDMS;

namespace EntityData
{
    public class ContinuousFutureConfig : IEntityTypeConfiguration<ContinuousFuture>
    {
        public void Configure(EntityTypeBuilder<ContinuousFuture> builder)
        {
            builder.HasOne(x => x.UnderlyingSymbol)
                .WithMany()
                .HasForeignKey(x => x.UnderlyingSymbolID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
