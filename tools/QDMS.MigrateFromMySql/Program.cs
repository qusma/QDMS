// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// One-off migration of an old (EF6/MySQL) QDMS installation into the new PostgreSQL schema.
//
//   dotnet run --project tools/QDMS.MigrateFromMySql -- \
//       --mysql "Server=localhost;Uid=root;Pwd=..." \
//       --postgres "Host=localhost;Database=qdms;Username=qdms;Password=..." \
//       [--postgres-data "<connection string for bars, if separate>"] [--skip-bars] [--jobs-only] [--jobs-file jobs.json] [--quartz-db qdmsquartz]
//
// Reads the "qdms" (metadata), "qdmsdata" (bars) and "qdmsquartz" (update jobs; older installs used "quartz") MySQL databases.
// The Postgres database must be empty: run this before starting QDMS.Host for the first time
// (it applies the migrations itself). IDs are preserved, so QTS workspaces keep pointing at the right instruments.

using EntityData;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var options = ParseArgs(args);
if (!options.TryGetValue("mysql", out var mysqlConn) || !options.TryGetValue("postgres", out var pgConn))
{
    Console.Error.WriteLine("Usage: --mysql <connection string without database> --postgres <connection string> " +
                            "[--postgres-data <connection string>] [--skip-bars] [--jobs-only] [--jobs-file jobs.json] [--quartz-db qdmsquartz]");
    return 1;
}

string pgDataConn = options.GetValueOrDefault("postgres-data", pgConn);
bool skipBars = options.ContainsKey("skip-bars");
bool jobsOnly = options.ContainsKey("jobs-only");
string jobsFile = options.GetValueOrDefault("jobs-file", "jobs.json");
string quartzDb = options.GetValueOrDefault("quartz-db", "qdmsquartz");

// 1. schema
if (!jobsOnly)
{
Console.WriteLine("Applying migrations to the target database...");
using (var ctx = new MyDBContext(new DbContextOptionsBuilder<MyDBContext>().UseNpgsqlSnakeCase(pgConn).Options))
{
    ctx.Database.Migrate();
    if (ctx.Instruments.Any() || ctx.Exchanges.Any() || ctx.Datasources.Any())
    {
        Console.Error.WriteLine("The target database already has data (QDMS.Host seeds it on first start). " +
                                "Point this at an empty database, or drop and recreate it.");
        return 1;
    }
}
using (var ctx = new DataDBContext(new DbContextOptionsBuilder<DataDBContext>().UseNpgsqlData(pgDataConn).Options))
{
    ctx.Database.Migrate();
}

// 2. metadata, in foreign key order. Instruments and continuous futures reference each other,
//    so instruments go in without continuous_future_id, which is filled in afterwards.
var metadataTables = new (string MySql, string Postgres)[]
{
    ("datasources", "datasources"),
    ("exchanges", "exchanges"),
    ("exchangesessions", "exchange_sessions"),
    ("sessiontemplates", "session_templates"),
    ("templatesessions", "template_sessions"),
    ("tags", "tags"),
    ("underlyingsymbols", "underlying_symbols"),
    ("instruments", "instruments"),
    ("continuousfutures", "continuous_futures"),
    ("instrumentsessions", "instrument_sessions"),
    ("tag_map", "tag_map"),
    ("dataupdatejobsettings", "data_update_jobs"),
    ("countries", "countries"),
    ("currencies", "currencies"),
    ("economicreleases", "economic_releases"),
    ("dividends", "dividends"),
    ("earningsannouncements", "earnings_announcements"),
};

using (var my = OpenMySql(mysqlConn, "qdms"))
using (var pg = new NpgsqlConnection(pgConn))
{
    pg.Open();
    using var tx = pg.BeginTransaction();

    foreach (var (myTable, pgTable) in metadataTables)
    {
        if (!MySqlTableExists(my, myTable))
        {
            Console.WriteLine($"  {myTable}: not in the source, skipped");
            continue;
        }

        var exclude = pgTable == "instruments" ? new[] { "continuous_future_id" } : Array.Empty<string>();
        long n = CopyTable(my, myTable, pg, pgTable, exclude);
        Console.WriteLine($"  {myTable} -> {pgTable}: {n} rows");
    }

    // link instruments to their continuous futures
    int linked = 0;
    using (var cmd = new MySqlCommand("SELECT ID, ContinuousFutureID FROM instruments WHERE ContinuousFutureID IS NOT NULL", my))
    using (var reader = cmd.ExecuteReader())
    {
        var links = new List<(int, int)>();
        while (reader.Read()) links.Add((reader.GetInt32(0), reader.GetInt32(1)));
        reader.Close();

        foreach (var (instrumentId, cfId) in links)
        {
            using var update = new NpgsqlCommand("UPDATE instruments SET continuous_future_id = $1 WHERE id = $2", pg, tx);
            update.Parameters.Add(new NpgsqlParameter<int> { TypedValue = cfId });
            update.Parameters.Add(new NpgsqlParameter<int> { TypedValue = instrumentId });
            linked += update.ExecuteNonQuery();
        }
    }
    Console.WriteLine($"  linked {linked} continuous futures");

    ResetIdentitySequences(pg, tx, metadataTables.Select(x => x.Postgres));
    tx.Commit();
}

// 3. bars
if (skipBars)
{
    Console.WriteLine("Skipping bars (--skip-bars)");
}
else
{
    using var my = OpenMySql(mysqlConn, "qdmsdata");
    using var pg = new NpgsqlConnection(pgDataConn);
    pg.Open();
    using var tx = pg.BeginTransaction();

    var sw = Stopwatch.StartNew();
    long bars = CopyTable(my, "data", pg, "bars", Array.Empty<string>(), progressEvery: 1_000_000);
    long infos = CopyTable(my, "instrumentinfo", pg, "instrument_info", Array.Empty<string>());
    tx.Commit();
    Console.WriteLine($"  data -> bars: {bars} rows, instrumentinfo -> instrument_info: {infos} rows ({sw.Elapsed:g})");
}

}

// 4. update jobs. Old QDMS kept them in Quartz.NET 2 tables, with the job settings as a JSON string
//    inside a binary-serialized job data map. Pull the JSON out and save it; post each one to the new
//    server's /jobs/<type> endpoint (see the jobs file) to reschedule them.
try
{
    using var my = OpenMySql(mysqlConn, quartzDb);
    using var cmd = new MySqlCommand("SELECT JOB_NAME, JOB_GROUP, JOB_DATA FROM qrtz_job_details", my);
    using var reader = cmd.ExecuteReader();
    var jobs = new List<object>();
    while (reader.Read())
    {
        string name = reader.GetString(0);
        string group = reader.GetString(1);
        string json = reader.IsDBNull(2) ? null : ExtractJson((byte[])reader.GetValue(2));
        jobs.Add(new { Name = name, Type = group, Endpoint = JobEndpoint(group), Settings = json });
        Console.WriteLine($"  job {group}/{name}: {(json == null ? "settings not found" : "ok")}");
    }

    File.WriteAllText(jobsFile, JsonSerializer.Serialize(jobs, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Wrote {jobs.Count} jobs to {jobsFile}");
}
catch (MySqlException ex)
{
    Console.WriteLine($"Could not read Quartz jobs ({ex.Message}); recreate update jobs by hand.");
}

Console.WriteLine("Done.");
return 0;

static Dictionary<string, string> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string>();
    for (int i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--")) continue;
        string key = args[i][2..];
        bool hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--");
        result[key] = hasValue ? args[++i] : "true";
    }

    return result;
}

static MySqlConnection OpenMySql(string connectionString, string database)
{
    var builder = new MySqlConnectionStringBuilder(connectionString)
    {
        Database = database,
        ConvertZeroDateTime = true,
        AllowZeroDateTime = false,
    };
    var conn = new MySqlConnection(builder.ConnectionString);
    conn.Open();
    return conn;
}

static bool MySqlTableExists(MySqlConnection my, string table)
{
    using var cmd = new MySqlCommand(
        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND LOWER(table_name) = @t", my);
    cmd.Parameters.AddWithValue("@t", table);
    return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
}

// Copies every column that exists on both sides, matching names case- and underscore-insensitively
// (InstrumentID -> instrument_id, DTOpen -> dt_open).
static long CopyTable(MySqlConnection my, string myTable, NpgsqlConnection pg, string pgTable, string[] exclude, long progressEvery = 0)
{
    static string Key(string name) => name.Replace("_", "").ToLowerInvariant();

    var pgColumns = new List<(string Name, string Type)>();
    using (var cmd = new NpgsqlCommand(
        "SELECT column_name, data_type FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = $1 ORDER BY ordinal_position", pg))
    {
        cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = pgTable });
        using var r = cmd.ExecuteReader();
        while (r.Read()) pgColumns.Add((r.GetString(0), r.GetString(1)));
    }

    var myColumns = new List<string>();
    using (var cmd = new MySqlCommand(
        "SELECT column_name FROM information_schema.columns WHERE table_schema = DATABASE() AND LOWER(table_name) = @t ORDER BY ordinal_position", my))
    {
        cmd.Parameters.AddWithValue("@t", myTable);
        using var r = cmd.ExecuteReader();
        while (r.Read()) myColumns.Add(r.GetString(0));
    }

    var columns = pgColumns
        .Where(p => !exclude.Contains(p.Name))
        .Select(p => (Pg: p, My: myColumns.FirstOrDefault(m => Key(m) == Key(p.Name))))
        .Where(x => x.My != null)
        .ToList();

    var unmapped = myColumns.Where(m => !columns.Any(c => c.My == m)).ToList();
    if (unmapped.Any()) Console.WriteLine($"    {myTable}: not copied: {string.Join(", ", unmapped)}");

    string select = $"SELECT {string.Join(", ", columns.Select(c => "`" + c.My + "`"))} FROM `{myTable}`";
    string copy = $"COPY {pgTable} ({string.Join(", ", columns.Select(c => c.Pg.Name))}) FROM STDIN (FORMAT BINARY)";

    long rows = 0;
    using (var cmd = new MySqlCommand(select, my) { CommandTimeout = 0 })
    using (var reader = cmd.ExecuteReader())
    using (var writer = pg.BeginBinaryImport(copy))
    {
        while (reader.Read())
        {
            writer.StartRow();
            for (int i = 0; i < columns.Count; i++)
            {
                if (reader.IsDBNull(i))
                {
                    writer.WriteNull();
                    continue;
                }

                string type = columns[i].Pg.Type;
                object value = reader.GetValue(i);
                switch (type)
                {
                    case "integer": writer.Write(Convert.ToInt32(value), type); break;
                    case "bigint": writer.Write(Convert.ToInt64(value), type); break;
                    case "boolean": writer.Write(Convert.ToBoolean(value), type); break;
                    case "numeric": writer.Write(Convert.ToDecimal(value), type); break;
                    case "double precision": writer.Write(Convert.ToDouble(value), type); break;
                    case "timestamp without time zone": writer.Write(DateTime.SpecifyKind((DateTime)value, DateTimeKind.Unspecified), type); break;
                    case "interval": writer.Write((TimeSpan)value, type); break;
                    case "bytea": writer.Write((byte[])value, type); break;
                    default: writer.Write(Convert.ToString(value), type); break;
                }
            }

            rows++;
            if (progressEvery > 0 && rows % progressEvery == 0) Console.WriteLine($"    {rows:N0} rows...");
        }

        writer.Complete();
    }

    return rows;
}

static void ResetIdentitySequences(NpgsqlConnection pg, NpgsqlTransaction tx, IEnumerable<string> tables)
{
    foreach (var table in tables)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT column_name FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = $1 AND is_identity = 'YES'", pg, tx);
        cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = table });
        var identity = cmd.ExecuteScalar() as string;
        if (identity == null) continue;

        using var reset = new NpgsqlCommand(
            $"SELECT setval(pg_get_serial_sequence('{table}', '{identity}'), COALESCE((SELECT MAX({identity}) FROM {table}), 0) + 1, false)", pg, tx);
        reset.ExecuteNonQuery();
    }
}

static string ExtractJson(byte[] blob)
{
    // the settings string is stored as UTF-8 inside the serialized map; take the outermost {...}
    string text = Encoding.UTF8.GetString(blob);
    var match = Regex.Match(text, @"\{""(?:[^\x00-\x08]|\s)*\}", RegexOptions.Singleline);
    if (!match.Success) return null;

    string candidate = match.Value;
    for (int end = candidate.Length; end > 1; end = candidate.LastIndexOf('}', end - 2) + 1)
    {
        try
        {
            string json = candidate[..end];
            JsonDocument.Parse(json).Dispose();
            return json;
        }
        catch (JsonException)
        {
        }

        if (end <= 1) break;
    }

    return null;
}

static string JobEndpoint(string group) => group switch
{
    "DataUpdate" => "/jobs/dataupdatejobs",
    "EconomicRelease" => "/jobs/economicreleaseupdatejobs",
    "DividendUpdate" => "/jobs/dividendupdatejobs",
    "EarningsUpdate" => "/jobs/earningsupdatejobs",
    _ => null
};

internal static class DbOptionsExtensions
{
    public static DbContextOptionsBuilder<MyDBContext> UseNpgsqlSnakeCase(this DbContextOptionsBuilder<MyDBContext> builder, string connectionString)
    {
        DbSetup.ConfigureMetadataDb(builder, connectionString);
        return builder;
    }

    public static DbContextOptionsBuilder<DataDBContext> UseNpgsqlData(this DbContextOptionsBuilder<DataDBContext> builder, string connectionString)
    {
        DbSetup.ConfigureDataDb(builder, connectionString);
        return builder;
    }
}
