// -----------------------------------------------------------------------
// <copyright file="MySqlHistoryContext.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// Why this exists: MySql servers with a default charset of UTF8 will give an error
// when EF tries to create the __MigrationHistory table because the key is too long.
// So we use this HistoryContext to limit the max length of the relevant fields.

using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations.History;

namespace EntityData.Migrations
{
    internal class MySqlHistoryContext : HistoryContext
    {
        public MySqlHistoryContext(DbConnection connection, string defaultSchema)
            : base(connection, defaultSchema)
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HistoryRow>().Property(h => h.MigrationId).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<HistoryRow>().Property(h => h.ContextKey).HasMaxLength(200).IsRequired();
        }
    }
}
