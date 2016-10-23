// -----------------------------------------------------------------------
// <copyright file="DataUpdateJobConfig.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity.ModelConfiguration;
using QDMS;

namespace EntityData
{
    public class DataUpdateJobConfig : EntityTypeConfiguration<DataUpdateJobSettings>
    {
        public DataUpdateJobConfig()
        {
            this.HasOptional(t => t.Instrument)
                .WithMany()
                .HasForeignKey(x => x.InstrumentID)
                .WillCascadeOnDelete(true);

            this.HasOptional(t => t.Tag)
                .WithMany()
                .HasForeignKey(x => x.TagID)
                .WillCascadeOnDelete(true);
        }
    }
}
