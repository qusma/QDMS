// -----------------------------------------------------------------------
// <copyright file="InstrumentConfig.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity.ModelConfiguration;
using QDMS;

namespace EntityData
{
    public class InstrumentConfig : EntityTypeConfiguration<Instrument>
    {
        public InstrumentConfig()
        {
            this.HasRequired(t => t.Exchange)
                .WithMany()
                .HasForeignKey(x => x.ExchangeID)
                .WillCascadeOnDelete(false);

            this.HasOptional(x => x.PrimaryExchange)
                .WithMany()
                .HasForeignKey(x => x.PrimaryExchangeID)
                .WillCascadeOnDelete(false);

            this.HasRequired(x => x.Datasource)
                .WithMany()
                .HasForeignKey(x => x.DatasourceID)
                .WillCascadeOnDelete(true);

            this.HasMany(x => x.Sessions)
                .WithRequired()
                .HasForeignKey(x => x.InstrumentID)
                .WillCascadeOnDelete(true);

            this.HasOptional(x => x.ContinuousFuture)
                .WithMany()
                .HasForeignKey(x => x.ContinuousFutureID)
                .WillCascadeOnDelete(true);
        }
    } 
 
}
