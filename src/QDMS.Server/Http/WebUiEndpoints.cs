// -----------------------------------------------------------------------
// <copyright file="WebUiEndpoints.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using QDMS.Server.Brokers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace QDMS.Server.Http
{
    /// <summary>
    /// Endpoints used only by the web UI (under /api). QDMSClient doesn't use them, so they're free to change.
    /// </summary>
    public static class WebUiEndpoints
    {
        //services are marked [FromServices] so the endpoints can be mapped even where they aren't registered (the HTTP tests)
        public static void MapWebUiEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api");

            //what's stored, for all instruments at once
            group.MapGet("/storageinfo", async ([FromServices] IDbContextFactory<DataDBContext> factory, CancellationToken token) =>
            {
                using var context = await factory.CreateDbContextAsync(token).ConfigureAwait(false);
                return HttpApi.Json(await context.StoredDataInfo.AsNoTracking().ToListAsync(token).ConfigureAwait(false));
            });

            group.MapGet("/currencies", async ([FromServices] IMyDbContext context, CancellationToken token) =>
                HttpApi.Json(await context.Currencies.OrderBy(x => x.Code).ToListAsync(token).ConfigureAwait(false)));

            //exchanges store Windows time zone IDs, so we list those (converting on Linux)
            group.MapGet("/timezones", () =>
            {
                var zones = new List<TimeZoneDto>();
                foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
                {
                    string id = tz.Id;
                    if (!tz.HasIanaId || TimeZoneInfo.TryConvertIanaIdToWindowsId(tz.Id, out id))
                    {
                        zones.Add(new TimeZoneDto { Id = id, DisplayName = tz.DisplayName });
                    }
                }

                return HttpApi.Json(zones.GroupBy(x => x.Id).Select(x => x.First()).OrderBy(x => x.Id).ToList());
            });

            group.MapGet("/instruments/{id:int}/bars", async (int id, HttpRequest request, [FromServices] IInstrumentSource repo, [FromServices] IDataStorage storage) =>
            {
                var instrument = await GetInstrument(repo, id).ConfigureAwait(false);
                if (instrument == null) return HttpApi.NotFound();

                var query = request.BindQuery<BarsQuery>();
                var bars = storage.GetData(instrument, query.From ?? DateTime.MinValue, query.To ?? DateTime.MaxValue, query.Frequency);
                return HttpApi.Json(bars);
            });

            //edit existing bars; adjusted prices are recomputed if Adjust is set
            group.MapPut("/instruments/{id:int}/bars", async (int id, HttpContext http, [FromServices] IInstrumentSource repo, [FromServices] IDataStorage storage) =>
            {
                var instrument = await GetInstrument(repo, id).ConfigureAwait(false);
                if (instrument == null) return HttpApi.NotFound();

                var query = http.Request.BindQuery<BarsQuery>();
                var bars = await http.Request.ReadBody<List<OHLCBar>>().ConfigureAwait(false);
                storage.UpdateData(bars, instrument, query.Frequency, query.Adjust);
                return HttpApi.Json(new { Count = bars.Count });
            });

            //add bars (e.g. imported from a file)
            group.MapPost("/instruments/{id:int}/bars", async (int id, HttpContext http, [FromServices] IInstrumentSource repo, [FromServices] IDataStorage storage) =>
            {
                var instrument = await GetInstrument(repo, id).ConfigureAwait(false);
                if (instrument == null) return HttpApi.NotFound();

                var query = http.Request.BindQuery<BarsQuery>();
                var bars = await http.Request.ReadBody<List<OHLCBar>>().ConfigureAwait(false);
                if (bars.Count == 0) return HttpApi.ValidationFailure(new[] { "No bars" });

                storage.AddData(bars.OrderBy(x => x.DT).ToList(), instrument, query.Frequency, query.Overwrite, query.Adjust);
                return HttpApi.Json(new { Count = bars.Count });
            });

            //delete the bars in the body, or all bars of this frequency if All is set
            group.MapPost("/instruments/{id:int}/bars/delete", async (int id, HttpContext http, [FromServices] IInstrumentSource repo, [FromServices] IDataStorage storage) =>
            {
                var instrument = await GetInstrument(repo, id).ConfigureAwait(false);
                if (instrument == null) return HttpApi.NotFound();

                var query = http.Request.BindQuery<BarsQuery>();
                if (query.All)
                {
                    storage.DeleteData(instrument, query.Frequency);
                    return HttpApi.Json(new { Count = -1 });
                }

                var bars = await http.Request.ReadBody<List<OHLCBar>>().ConfigureAwait(false);
                storage.DeleteData(instrument, query.Frequency, bars);
                return HttpApi.Json(new { Count = bars.Count });
            });

            //request data through the historical data broker and wait for it
            group.MapPost("/instruments/{id:int}/fetch", async (int id, HttpContext http, [FromServices] IInstrumentSource repo, [FromServices] IHistoricalDataBroker broker) =>
            {
                var instrument = await GetInstrument(repo, id).ConfigureAwait(false);
                if (instrument == null) return HttpApi.NotFound();

                var fetch = await http.Request.ReadBody<FetchRequest>().ConfigureAwait(false);
                return await Fetch(instrument, fetch, broker, http.RequestAborted).ConfigureAwait(false);
            });

            //tail of today's log file
            group.MapGet("/logs", (HttpRequest request) =>
            {
                int lines = int.TryParse(request.Query["lines"], out int n) ? Math.Clamp(n, 1, 5000) : 500;
                var dir = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "logs"));
                var file = dir.Exists ? dir.GetFiles("*.log").OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault() : null;
                if (file == null) return HttpApi.Json(new LogTail { File = null, Lines = new List<string>() });

                //the file is open for writing by NLog
                using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                var queue = new Queue<string>(lines);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (queue.Count == lines) queue.Dequeue();
                    queue.Enqueue(line);
                }

                return HttpApi.Json(new LogTail { File = file.Name, Lines = queue.ToList() });
            });
        }

        private static async Task<Instrument> GetInstrument(IInstrumentSource repo, int id) =>
            (await repo.FindInstruments(x => x.ID == id).ConfigureAwait(false)).FirstOrDefault();

        private static async Task<IResult> Fetch(Instrument instrument, FetchRequest fetch, IHistoricalDataBroker broker, CancellationToken token)
        {
            string requester = "WEBUI-" + Guid.NewGuid().ToString("N");
            var request = new HistoricalDataRequest(
                instrument,
                fetch.Frequency,
                fetch.StartingDate,
                fetch.EndingDate,
                fetch.DataLocation,
                fetch.SaveToStorage,
                fetch.RTHOnly)
            {
                RequesterIdentity = requester
            };

            var done = new TaskCompletionSource<List<OHLCBar>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var errors = new List<string>();

            void OnData(object sender, HistoricalDataEventArgs e)
            {
                if (e.Request.RequesterIdentity == requester) done.TrySetResult(e.Data);
            }

            //errors don't reliably carry our request's ID, so collect the ones that match it and report them on timeout
            void OnError(object sender, ErrorArgs e)
            {
                if (e.RequestID == null || e.RequestID == request.AssignedID || e.RequestID == request.RequestID)
                {
                    lock (errors) errors.Add(e.ErrorMessage);
                }
            }

            broker.HistoricalDataArrived += OnData;
            broker.Error += OnError;
            try
            {
                try
                {
                    broker.RequestHistoricalData(request);
                }
                catch (Exception ex)
                {
                    return HttpApi.Error(HttpStatusCode.BadRequest, ex.Message);
                }

                int timeout = fetch.TimeoutSeconds > 0 ? fetch.TimeoutSeconds : 120;
                var finished = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(timeout), token)).ConfigureAwait(false);
                if (finished != done.Task)
                {
                    string details;
                    lock (errors) details = string.Join("; ", errors);
                    return HttpApi.Json(new ErrorResponse(HttpStatusCode.GatewayTimeout,
                        $"No answer from the data source within {timeout} s", details), StatusCodes.Status504GatewayTimeout);
                }

                List<string> errorList;
                lock (errors) errorList = errors.ToList();
                return HttpApi.Json(new FetchResult { Bars = done.Task.Result, Errors = errorList });
            }
            finally
            {
                broker.HistoricalDataArrived -= OnData;
                broker.Error -= OnError;
            }
        }

        public class BarsQuery
        {
            public BarSize Frequency { get; set; } = BarSize.OneDay;
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            public bool Overwrite { get; set; }
            public bool Adjust { get; set; } = true;
            public bool All { get; set; }
        }

        public class FetchRequest
        {
            public BarSize Frequency { get; set; } = BarSize.OneDay;
            public DateTime StartingDate { get; set; }
            public DateTime EndingDate { get; set; }
            public DataLocation DataLocation { get; set; } = DataLocation.ExternalOnly;
            public bool SaveToStorage { get; set; }
            public bool RTHOnly { get; set; } = true;
            public int TimeoutSeconds { get; set; }
        }

        public class FetchResult
        {
            public List<OHLCBar> Bars { get; set; }
            public List<string> Errors { get; set; }
        }

        public class TimeZoneDto
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
        }

        public class LogTail
        {
            public string File { get; set; }
            public List<string> Lines { get; set; }
        }
    }
}
