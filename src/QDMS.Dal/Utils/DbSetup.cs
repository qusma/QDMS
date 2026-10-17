// -----------------------------------------------------------------------
// <copyright file="DbSetup.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

namespace EntityData
{
    public static class DbSetup
    {
        public static void ConfigureMetadataDb(DbContextOptionsBuilder options, string connectionString) =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();

        public static void ConfigureDataDb(DbContextOptionsBuilder options, string connectionString) =>
            options.UseNpgsql(connectionString, o => o.MigrationsHistoryTable(DataDBContext.MigrationsHistoryTable))
                .UseSnakeCaseNamingConvention();
    }
}
