// -----------------------------------------------------------------------
// <copyright file="ModelConventions.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace EntityData
{
    public static class ModelConventions
    {
        /// <summary>
        /// QDMS stores exchange-local times, not instants. Map every DateTime to
        /// "timestamp without time zone" and strip the Kind on write, since Npgsql refuses
        /// to write UTC-kind values to that column type.
        /// </summary>
        public static void UseLocalDateTimes(this ModelConfigurationBuilder builder)
        {
            builder.Properties<DateTime>()
                .HaveColumnType("timestamp without time zone")
                .HaveConversion<UnspecifiedKindConverter>();
            builder.Properties<DateTime?>()
                .HaveColumnType("timestamp without time zone")
                .HaveConversion<NullableUnspecifiedKindConverter>();
        }

        private class UnspecifiedKindConverter : ValueConverter<DateTime, DateTime>
        {
            public UnspecifiedKindConverter()
                : base(x => DateTime.SpecifyKind(x, DateTimeKind.Unspecified), x => x)
            {
            }
        }

        private class NullableUnspecifiedKindConverter : ValueConverter<DateTime?, DateTime?>
        {
            public NullableUnspecifiedKindConverter()
                : base(x => x.HasValue ? DateTime.SpecifyKind(x.Value, DateTimeKind.Unspecified) : x, x => x)
            {
            }
        }
    }
}
