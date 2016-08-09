// -----------------------------------------------------------------------
// <copyright file="DataDBContext.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity;
using EntityData.Migrations;
using EntityData.Migrations.DataDBContextNS;
using QDMS;

namespace EntityData
{
    public class DataDBContext : DbContext
    {
        public DataDBContext()
            : base("Name=qdmsDataEntities")
        {
        }

        public DataDBContext(string connectionString)
        {
            Database.Connection.ConnectionString = connectionString;
        }
 
        public DbSet<OHLCBar> Data { get; set; }
        public DbSet<StoredDataInfo> StoredDataInfo { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataDBContext, DataDBContextConfiguration>());

            modelBuilder.Entity<OHLCBar>().ToTable("data");
            modelBuilder.Entity<StoredDataInfo>().ToTable("instrumentinfo");


            modelBuilder.Entity<OHLCBar>().Property(x => x.DT).HasPrecision(3);
            modelBuilder.Entity<OHLCBar>().Property(x => x.DTOpen).HasPrecision(3);
            modelBuilder.Entity<OHLCBar>().Property(x => x.Open).HasPrecision(16, 8);
            modelBuilder.Entity<OHLCBar>().Property(x => x.High).HasPrecision(16, 8);
            modelBuilder.Entity<OHLCBar>().Property(x => x.Low).HasPrecision(16, 8);
            modelBuilder.Entity<OHLCBar>().Property(x => x.Close).HasPrecision(16, 8);
            modelBuilder.Entity<OHLCBar>().Property(x => x.AdjOpen).HasPrecision(16, 8);
            modelBuilder.Entity<OHLCBar>().Property(x => x.AdjHigh).HasPrecision(16, 8);
            modelBuilder.Entity<OHLCBar>().Property(x => x.AdjLow).HasPrecision(16, 8);
            modelBuilder.Entity<OHLCBar>().Property(x => x.AdjClose).HasPrecision(16, 8);
            modelBuilder.Entity<OHLCBar>().Property(x => x.Dividend).HasPrecision(16, 8);
            modelBuilder.Entity<OHLCBar>().Property(x => x.Split).HasPrecision(16, 8);
            modelBuilder.Entity<OHLCBar>().HasKey(x => new { x.DT, x.InstrumentID, x.Frequency });

            modelBuilder.Entity<StoredDataInfo>().HasKey(x => new { x.InstrumentID, x.Frequency });
            modelBuilder.Entity<StoredDataInfo>().Property(x => x.EarliestDate).HasPrecision(3);
            modelBuilder.Entity<StoredDataInfo>().Property(x => x.LatestDate).HasPrecision(3);
        }
    }
}
