// -----------------------------------------------------------------------
// <copyright file="DesignTimeFactories.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EntityData
{
    // Used by "dotnet ef migrations add"; the connection string is never opened.
    internal class MyDBContextDesignFactory : IDesignTimeDbContextFactory<MyDBContext>
    {
        public MyDBContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<MyDBContext>();
            DbSetup.ConfigureMetadataDb(options, "Host=localhost;Database=qdms");
            return new MyDBContext(options.Options);
        }
    }

    internal class DataDBContextDesignFactory : IDesignTimeDbContextFactory<DataDBContext>
    {
        public DataDBContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<DataDBContext>();
            DbSetup.ConfigureDataDb(options, "Host=localhost;Database=qdms");
            return new DataDBContext(options.Options);
        }
    }
}
