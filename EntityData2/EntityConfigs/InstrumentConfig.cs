// -----------------------------------------------------------------------
// <copyright file="InstrumentConfig.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QDMS;

namespace EntityData
{
    public class InstrumentConfig : IEntityTypeConfiguration<Instrument>
    {

        public void Configure(EntityTypeBuilder<Instrument> builder)
        {
            builder.HasOne(t => t.Exchange)
                .WithMany()
                .HasForeignKey(x => x.ExchangeID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.PrimaryExchange)
                .WithMany()
                .HasForeignKey(x => x.PrimaryExchangeID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Datasource)
                .WithMany()
                .HasForeignKey(x => x.DatasourceID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Sessions)
                .HasOne()
                .HasForeignKey(x => x.InstrumentID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ContinuousFuture)
                .WithMany()
                .HasForeignKey(x => x.ContinuousFutureID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    } 
 
}
