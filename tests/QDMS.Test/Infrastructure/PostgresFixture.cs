// -----------------------------------------------------------------------
// <copyright file="PostgresFixture.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace QDMSTest
{
    /// <summary>
    /// Starts one Postgres container for the whole test run (needs Docker). A template database is migrated once;
    /// each test gets a fresh copy of it via <see cref="CreateDatabase"/>, which is fast.
    /// </summary>
    [SetUpFixture]
    public class PostgresFixture
    {
        private const string TemplateDb = "qdms_template";

        private static PostgreSqlContainer _container;
        private static Task _startup;
        private static readonly object StartupLock = new object();

        /// <summary>
        /// Creates a new, fully migrated database and returns its connection string.
        /// </summary>
        public static async Task<string> CreateDatabase()
        {
            await EnsureStarted().ConfigureAwait(false);

            string name = "test_" + Guid.NewGuid().ToString("N");
            await using (var conn = new NpgsqlConnection(_container.GetConnectionString()))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand($"CREATE DATABASE {name} TEMPLATE {TemplateDb}", conn);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            return ConnectionStringFor(name);
        }

        private static Task EnsureStarted()
        {
            lock (StartupLock)
            {
                return _startup ??= Start();
            }
        }

        private static async Task Start()
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:17-alpine")
                .Build();
            await _container.StartAsync().ConfigureAwait(false);

            await using (var conn = new NpgsqlConnection(_container.GetConnectionString()))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand($"CREATE DATABASE {TemplateDb}", conn);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            string templateCs = ConnectionStringFor(TemplateDb);
            await using (var ctx = CreateMetadataContext(templateCs))
            {
                await ctx.Database.MigrateAsync().ConfigureAwait(false);
            }

            await using (var ctx = CreateDataContext(templateCs))
            {
                await ctx.Database.MigrateAsync().ConfigureAwait(false);
            }

            //a template database can't be copied while anyone is connected to it
            NpgsqlConnection.ClearAllPools();
        }

        public static MyDBContext CreateMetadataContext(string connectionString)
        {
            var options = new DbContextOptionsBuilder<MyDBContext>();
            DbSetup.ConfigureMetadataDb(options, connectionString);
            return new MyDBContext(options.Options);
        }

        public static DataDBContext CreateDataContext(string connectionString)
        {
            var options = new DbContextOptionsBuilder<DataDBContext>();
            DbSetup.ConfigureDataDb(options, connectionString);
            return new DataDBContext(options.Options);
        }

        private static string ConnectionStringFor(string database) =>
            new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = database }.ConnectionString;

        [OneTimeTearDown]
        public async Task TearDown()
        {
            if (_container != null)
            {
                await _container.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
