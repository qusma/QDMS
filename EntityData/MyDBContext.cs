// -----------------------------------------------------------------------
// <copyright file="MyDBContext.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity;
using EntityData.Migrations;
using QDMS;

namespace EntityData
{
    public partial class MyDBContext : DbContext
    {
        public MyDBContext()
            : base("Name=qdmsEntities")
        {
        }

        public MyDBContext(string connectionString)
        {
            Database.Connection.ConnectionString = connectionString;
        }

        public DbSet<Instrument> Instruments { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Exchange> Exchanges { get; set; }
        public DbSet<Datasource> Datasources { get; set; }
        public DbSet<SessionTemplate> SessionTemplates { get; set; }
        public DbSet<ExchangeSession> ExchangeSessions { get; set; }
        public DbSet<InstrumentSession> InstrumentSessions { get; set; }
        public DbSet<TemplateSession> TemplateSessions { get; set; }
        public DbSet<UnderlyingSymbol> UnderlyingSymbols { get; set; }
        public DbSet<ContinuousFuture> ContinuousFutures { get; set; }
        public DbSet<DataUpdateJobDetails> DataUpdateJobs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new InstrumentConfig());
            modelBuilder.Configurations.Add(new TagConfig());
            modelBuilder.Configurations.Add(new ExchangeConfig());
            modelBuilder.Configurations.Add(new DatasourceConfig());
            modelBuilder.Configurations.Add(new UnderlyingSymbolConfig());
            modelBuilder.Configurations.Add(new ContinuousFutureConfig());
            modelBuilder.Configurations.Add(new DataUpdateJobConfig());

            modelBuilder.Entity<ExchangeSession>().ToTable("exchangesessions");
            modelBuilder.Entity<InstrumentSession>().ToTable("instrumentsessions");
            modelBuilder.Entity<TemplateSession>().ToTable("templatesessions");

            modelBuilder.Entity<Instrument>()
            .HasMany(c => c.Tags)
            .WithMany()             
            .Map(x =>
            {
                x.MapLeftKey("InstrumentID");
                x.MapRightKey("TagID");
                x.ToTable("tag_map");
            });


            modelBuilder.Entity<Instrument>().Property(x => x.Strike).HasPrecision(16, 8);
            modelBuilder.Entity<Instrument>().Property(x => x.MinTick).HasPrecision(16, 8);

            modelBuilder.Entity<ExchangeSession>().Property(x => x.OpeningTime).HasPrecision(0);
            modelBuilder.Entity<ExchangeSession>().Property(x => x.ClosingTime).HasPrecision(0);

            modelBuilder.Entity<InstrumentSession>().Property(x => x.OpeningTime).HasPrecision(0);
            modelBuilder.Entity<InstrumentSession>().Property(x => x.ClosingTime).HasPrecision(0);

            modelBuilder.Entity<TemplateSession>().Property(x => x.OpeningTime).HasPrecision(0);
            modelBuilder.Entity<TemplateSession>().Property(x => x.ClosingTime).HasPrecision(0);

            
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<MyDBContext, MyDbContextConfiguration>());
        }
    }
}
