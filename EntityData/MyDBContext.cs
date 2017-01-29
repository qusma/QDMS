// -----------------------------------------------------------------------
// <copyright file="MyDBContext.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using EntityData.Migrations;
using QDMS;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Annotations;
using System.Threading;
using System.Threading.Tasks;

namespace EntityData
{
    public partial class MyDBContext : DbContext, IMyDbContext
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
        public DbSet<DataUpdateJobSettings> DataUpdateJobs { get; set; }
        public DbSet<EconomicRelease> EconomicReleases { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<Dividend> Dividends { get; set; }

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

            modelBuilder.Entity<Dividend>().Property(x => x.Amount).HasPrecision(16,8);

            // Instrument

            string uniqueIndex = "IX_Unique";

            modelBuilder.Entity<Instrument>().Property(t => t.Symbol)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute(uniqueIndex)
                        {
                            IsUnique = true,
                            Order = 1
                        }
                    )
                );

            modelBuilder.Entity<Instrument>().Property(t => t.Type)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute(uniqueIndex)
                        {
                            IsUnique = true,
                            Order = 2
                        }
                    )
                );

            modelBuilder.Entity<Instrument>().Property(t => t.DatasourceID)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute(uniqueIndex)
                        {
                            IsUnique = true,
                            Order = 3
                        }
                    )
                );

            modelBuilder.Entity<Instrument>().Property(t => t.ExchangeID)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute(uniqueIndex)
                        {
                            IsUnique = true,
                            Order = 4
                        }
                    )
                );

            modelBuilder.Entity<Instrument>().Property(t => t.Expiration)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute(uniqueIndex)
                        {
                            IsUnique = true,
                            Order = 5
                        }
                    )
                );

            modelBuilder.Entity<Instrument>().Property(t => t.Strike)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute(uniqueIndex)
                        {
                            IsUnique = true,
                            Order = 6
                        }
                    )
                );

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<MyDBContext, MyDbContextConfiguration>());
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

    public interface IMyDbContext
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

        DbChangeTracker ChangeTracker { get; }
        DbContextConfiguration Configuration { get; }
        Database Database { get; }

        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        DbSet Set(Type entityType);

        DbEntityEntry Entry(object entity);
        DbEntityEntry<T> Entry<T>(T entity) where T : class;
        int SaveChanges();
        Task<int> SaveChangesAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        void Dispose();
        void SetEntryState(object entity, EntityState state);
        void UpdateEntryValues(object entity, object newValues);
    }
}