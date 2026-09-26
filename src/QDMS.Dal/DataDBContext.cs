// -----------------------------------------------------------------------
// <copyright file="DataDBContext.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using QDMS;

namespace EntityData
{
    /// <summary>
    /// Bar storage. Only used to define and migrate the schema: reads and writes go through
    /// the Npgsql-based IDataStorage implementation.
    /// </summary>
    public class DataDBContext : DbContext
    {
        public const string MigrationsHistoryTable = "__ef_migrations_history_data";

        public DataDBContext(DbContextOptions<DataDBContext> options) : base(options)
        {
        }

        public DbSet<OHLCBar> Data { get; set; }
        public DbSet<StoredDataInfo> StoredDataInfo { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.UseLocalDateTimes();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OHLCBar>(e =>
            {
                e.ToTable("bars");
                e.HasKey(x => new { x.InstrumentID, x.Frequency, x.DT });
                e.Property(x => x.DT).HasPrecision(3);
                e.Property(x => x.DTOpen).HasPrecision(3);
                e.Property(x => x.Open).HasPrecision(16, 8);
                e.Property(x => x.High).HasPrecision(16, 8);
                e.Property(x => x.Low).HasPrecision(16, 8);
                e.Property(x => x.Close).HasPrecision(16, 8);
                e.Property(x => x.AdjOpen).HasPrecision(16, 8);
                e.Property(x => x.AdjHigh).HasPrecision(16, 8);
                e.Property(x => x.AdjLow).HasPrecision(16, 8);
                e.Property(x => x.AdjClose).HasPrecision(16, 8);
                e.Property(x => x.Dividend).HasPrecision(16, 8);
                e.Property(x => x.Split).HasPrecision(16, 8);
            });

            modelBuilder.Entity<StoredDataInfo>(e =>
            {
                e.ToTable("instrument_info");
                e.HasKey(x => new { x.InstrumentID, x.Frequency });
                e.Property(x => x.EarliestDate).HasPrecision(3);
                e.Property(x => x.LatestDate).HasPrecision(3);
            });
        }
    }
}
