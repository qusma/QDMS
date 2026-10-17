// -----------------------------------------------------------------------
// <copyright file="MyDBContext.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using QDMS;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EntityData
{
    /// <summary>
    /// Metadata: instruments, exchanges, sessions, tags, jobs, dividends, earnings, economic releases.
    /// Quartz's tables (qrtz_*) live in the same database; Quartz creates them itself.
    /// </summary>
    public class MyDBContext : DbContext, IMyDbContext
    {
        public MyDBContext(DbContextOptions<MyDBContext> options) : base(options)
        {
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
        public DbSet<DataUpdateJobSettings> DataUpdateJobs { get; set; }
        public DbSet<EconomicRelease> EconomicReleases { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<Dividend> Dividends { get; set; }
        public DbSet<EarningsAnnouncement> EarningsAnnouncements { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.UseLocalDateTimes();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Instrument>(e =>
            {
                e.Property(x => x.ID).ValueGeneratedOnAdd();
                e.Property(x => x.Strike).HasPrecision(16, 8);
                e.Property(x => x.MinTick).HasPrecision(16, 8);


                e.HasOne(x => x.Exchange).WithMany().HasForeignKey(x => x.ExchangeID).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.PrimaryExchange).WithMany().HasForeignKey(x => x.PrimaryExchangeID).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Datasource).WithMany().HasForeignKey(x => x.DatasourceID).IsRequired().OnDelete(DeleteBehavior.Cascade);
                e.HasMany(x => x.Sessions).WithOne(x => x.Instrument).HasForeignKey(x => x.InstrumentID).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.ContinuousFuture).WithMany().HasForeignKey(x => x.ContinuousFutureID).OnDelete(DeleteBehavior.Restrict);

                e.HasMany(x => x.Tags).WithMany().UsingEntity(
                    "tag_map",
                    r => r.HasOne(typeof(Tag)).WithMany().HasForeignKey("tag_id").OnDelete(DeleteBehavior.Cascade),
                    l => l.HasOne(typeof(Instrument)).WithMany().HasForeignKey("instrument_id").OnDelete(DeleteBehavior.Cascade));

                //NULLs are distinct, as they were in MySQL: e.g. two instruments with the same symbol and no expiration don't conflict
                e.HasIndex(x => new { x.Symbol, x.Type, x.DatasourceID, x.ExchangeID, x.Expiration, x.Strike })
                    .IsUnique();
            });

            modelBuilder.Entity<ContinuousFuture>(e =>
            {
                e.HasOne(x => x.Instrument).WithMany().HasForeignKey(x => x.InstrumentID).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.UnderlyingSymbol).WithMany().HasForeignKey(x => x.UnderlyingSymbolID).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DataUpdateJobSettings>(e =>
            {
                e.ToTable("data_update_jobs");
                e.HasOne(x => x.Instrument).WithMany().HasForeignKey(x => x.InstrumentID).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagID).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Exchange>()
                .HasMany(x => x.Sessions).WithOne(x => x.Exchange).HasForeignKey(x => x.ExchangeID).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessionTemplate>()
                .HasMany(x => x.Sessions).WithOne(x => x.Template).HasForeignKey(x => x.TemplateID).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UnderlyingSymbol>(e =>
            {
                e.Ignore(x => x.Rule);
                e.HasIndex(x => x.Symbol).IsUnique();
            });

            modelBuilder.Entity<Dividend>(e =>
            {
                e.Property(x => x.Amount).HasPrecision(16, 8);
                e.HasIndex(x => x.ExDate);
                e.HasIndex(x => x.Symbol);
            });

            modelBuilder.Entity<EarningsAnnouncement>(e =>
            {
                e.Property(x => x.EarningsPerShare).HasPrecision(16, 8);
                e.Property(x => x.Forecast).HasPrecision(16, 8);
                e.HasIndex(x => x.Symbol);
                e.HasIndex(x => x.Date);
            });

            modelBuilder.Entity<EconomicRelease>(e =>
            {
                e.HasIndex(x => new { x.Name, x.Country, x.DateTime }).IsUnique();
                e.HasIndex(x => x.Currency);
            });

            modelBuilder.Entity<Currency>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<Country>().HasIndex(x => x.Name).IsUnique();
            modelBuilder.Entity<Datasource>().HasIndex(x => x.Name).IsUnique();
        }

        public void SetEntryState(object entity, EntityState state)
        {
            Entry(entity).State = state;
        }

        public void UpdateEntryValues(object entity, object newValues)
        {
            Entry(entity).CurrentValues.SetValues(newValues);
        }
    }

    public interface IMyDbContext : IDisposable
    {
        DbSet<Instrument> Instruments { get; set; }
        DbSet<Tag> Tags { get; set; }
        DbSet<Exchange> Exchanges { get; set; }
        DbSet<Datasource> Datasources { get; set; }
        DbSet<SessionTemplate> SessionTemplates { get; set; }
        DbSet<ExchangeSession> ExchangeSessions { get; set; }
        DbSet<InstrumentSession> InstrumentSessions { get; set; }
        DbSet<TemplateSession> TemplateSessions { get; set; }
        DbSet<UnderlyingSymbol> UnderlyingSymbols { get; set; }
        DbSet<ContinuousFuture> ContinuousFutures { get; set; }
        DbSet<DataUpdateJobSettings> DataUpdateJobs { get; set; }
        DbSet<EconomicRelease> EconomicReleases { get; set; }
        DbSet<Country> Countries { get; set; }
        DbSet<Currency> Currencies { get; set; }
        DbSet<Dividend> Dividends { get; set; }
        DbSet<EarningsAnnouncement> EarningsAnnouncements { get; set; }

        ChangeTracker ChangeTracker { get; }
        DatabaseFacade Database { get; }

        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        EntityEntry Entry(object entity);
        EntityEntry<T> Entry<T>(T entity) where T : class;
        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        void SetEntryState(object entity, EntityState state);
        void UpdateEntryValues(object entity, object newValues);
    }
}
