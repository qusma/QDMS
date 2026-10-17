// -----------------------------------------------------------------------
// <copyright file="JobEndpoints.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QDMS.Server.Jobs;
using QDMS.Server.Repositories;
using System.Net;
using System.Threading.Tasks;

namespace QDMS.Server.Http
{
    public static class JobEndpoints
    {
        public static void MapJobEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/jobs");

            group.MapGet("/dataupdatejobs", async (IJobsRepository repo) => HttpApi.Json(await repo.GetDataUpdateJobs().ConfigureAwait(false)));
            group.MapPost("/dataupdatejobs", (HttpContext http, IJobsRepository repo) => AddJob<DataUpdateJobSettings>(http, repo));
            group.MapDelete("/dataupdatejobs", (HttpContext http, IJobsRepository repo) => DeleteJob<DataUpdateJobSettings>(http, repo));

            group.MapGet("/economicreleaseupdatejobs", async (IJobsRepository repo) => HttpApi.Json(await repo.GetEconomicReleaseUpdateJobs().ConfigureAwait(false)));
            group.MapPost("/economicreleaseupdatejobs", (HttpContext http, IJobsRepository repo) => AddJob<EconomicReleaseUpdateJobSettings>(http, repo));
            group.MapDelete("/economicreleaseupdatejobs", (HttpContext http, IJobsRepository repo) => DeleteJob<EconomicReleaseUpdateJobSettings>(http, repo));

            group.MapGet("/dividendupdatejobs", async (IJobsRepository repo) => HttpApi.Json(await repo.GetDividendUpdateJobs().ConfigureAwait(false)));
            group.MapPost("/dividendupdatejobs", (HttpContext http, IJobsRepository repo) => AddJob<DividendUpdateJobSettings>(http, repo));
            group.MapDelete("/dividendupdatejobs", (HttpContext http, IJobsRepository repo) => DeleteJob<DividendUpdateJobSettings>(http, repo));

            group.MapGet("/earningsupdatejobs", async (IJobsRepository repo) => HttpApi.Json(await repo.GetEarningsUpdateJobs().ConfigureAwait(false)));
            group.MapPost("/earningsupdatejobs", (HttpContext http, IJobsRepository repo) => AddJob<EarningsUpdateJobSettings>(http, repo));
            group.MapDelete("/earningsupdatejobs", (HttpContext http, IJobsRepository repo) => DeleteJob<EarningsUpdateJobSettings>(http, repo));

            //new: run a job immediately. {type} is one of the JobTypes values, e.g. DataUpdate
            group.MapPost("/{type}/{name}/run", async (string type, string name, IJobsRepository repo) =>
                await repo.TriggerJob(name, type).ConfigureAwait(false)
                    ? Results.Ok()
                    : HttpApi.NotFound());
        }

        private static async Task<IResult> AddJob<T>(HttpContext http, IJobsRepository repo) where T : IJobSettings
        {
            var (jobSettings, failure) = await http.ReadAndValidateBody<T>().ConfigureAwait(false);
            if (failure != null) return failure;

            //make sure name doesn't already exist
            var existingJob = await repo.GetJobDetails(jobSettings.Name, JobTypes.GetJobType(jobSettings)).ConfigureAwait(false);
            if (existingJob != null)
            {
                return HttpApi.Error(HttpStatusCode.Conflict, "A job with this name already exists");
            }

            await repo.ScheduleJob(jobSettings).ConfigureAwait(false);

            return HttpApi.Json(jobSettings);
        }

        private static async Task<IResult> DeleteJob<T>(HttpContext http, IJobsRepository repo) where T : IJobSettings
        {
            T jobSettings = await http.Request.ReadBody<T>().ConfigureAwait(false);
            if (jobSettings == null) return HttpApi.ValidationFailure(new[] { "Missing job" });

            //Make sure it exists
            var existingJob = await repo.GetJobDetails(jobSettings.Name, JobTypes.GetJobType(jobSettings)).ConfigureAwait(false);
            if (existingJob == null) return HttpApi.NotFound();

            await repo.DeleteJob(jobSettings).ConfigureAwait(false);

            return HttpApi.Json(jobSettings);
        }
    }
}
