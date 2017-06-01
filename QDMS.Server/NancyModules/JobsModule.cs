// -----------------------------------------------------------------------
// <copyright file="DatasourceModule.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using QDMS.Server.Brokers;
using QDMS.Server.Jobs;
using QDMS.Server.Nancy;
using QDMS.Server.Repositories;

namespace QDMS.Server.NancyModules
{
    public class JobsModule : NancyModule
    {
        public JobsModule(IJobsRepository repo, IEconomicReleaseBroker erb)
            : base("/jobs")
        {
            this.RequiresAuthentication();

            //Data Update Jobs

            Get["/dataupdatejobs"] = _ => repo.GetDataUpdateJobs();

            Post["/dataupdatejobs"] = _ => AddJob<DataUpdateJobSettings>(repo);

            Delete["/dataupdatejobs"] = _ => DeleteJob<DataUpdateJobSettings>(repo);

            //Economic Release Jobs

            Get["/economicreleaseupdatejobs"] = _ => repo.GetEconomicReleaseUpdateJobs();

            Post["/economicreleaseupdatejobs"] = _ => AddJob<EconomicReleaseUpdateJobSettings>(repo);

            Delete["/economicreleaseupdatejobs"] = _ => DeleteJob<EconomicReleaseUpdateJobSettings>(repo);

            //Dividend Update Jobs

            Get["/dividendupdatejobs"] = _ => repo.GetDividendUpdateJobs();

            Post["/dividendupdatejobs"] = _ => AddJob<DividendUpdateJobSettings>(repo);

            Delete["/dividendupdatejobs"] = _ => DeleteJob<DividendUpdateJobSettings>(repo);
        }

        private dynamic AddJob<T>(IJobsRepository repo) where T : IJobSettings
        {
            T jobSettings = this.BindAndValidate<T>();
            if (ModelValidationResult.IsValid == false)
            {
                return this.ValidationFailure();
            }

            //make sure name doesn't already exist
            var existingJob = repo.GetJobDetails(jobSettings.Name, JobTypes.GetJobType(jobSettings));
            if (existingJob != null)
            {
                return Negotiate
                    .WithModel(new ErrorResponse(
                        HttpStatusCode.Conflict,
                        "A job with this name already exists", ""))
                    .WithStatusCode(HttpStatusCode.Conflict);
            }

            //add it and return
            repo.ScheduleJob(jobSettings);

            return jobSettings;
        }

        private dynamic DeleteJob<T>(IJobsRepository repo) where T: IJobSettings
        {
            T jobSettings = this.Bind<T>();
            if (jobSettings == null)
            {
                return HttpStatusCode.BadRequest;
            }

            //Make sure it exists
            var existingJob = repo.GetJobDetails(jobSettings.Name, JobTypes.GetJobType(jobSettings));
            if (existingJob == null) return HttpStatusCode.NotFound;

            //delete and return
            repo.DeleteJob(jobSettings);

            return jobSettings;
        }
    }
}