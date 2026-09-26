// -----------------------------------------------------------------------
// <copyright file="JobsRepository.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Newtonsoft.Json;
using NLog;
using QDMS.Server.Jobs;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QDMS.Server.Repositories
{
    /// <summary>
    /// Update jobs are stored in Quartz: the job settings are serialized to JSON in the job's data map,
    /// and the job group identifies the type (see <see cref="JobTypes"/>).
    /// </summary>
    public class JobsRepository : IJobsRepository
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IMyDbContext _context;
        private readonly IScheduler _scheduler;

        public JobsRepository(IMyDbContext context, IScheduler scheduler)
        {
            _context = context;
            _scheduler = scheduler;
        }

        public async Task<DataUpdateJobSettings> GetDataUpdateJob(string name)
        {
            var details = await _scheduler.GetJobDetail(new JobKey(name, JobTypes.DataUpdate)).ConfigureAwait(false);
            if (details == null) return null;

            var job = Deserialize<DataUpdateJobSettings>(details);
            if (job != null) LoadReferences(job);
            return job;
        }

        public async Task<List<DataUpdateJobSettings>> GetDataUpdateJobs()
        {
            var jobs = await GetJobs<DataUpdateJobSettings>(JobTypes.DataUpdate).ConfigureAwait(false);
            jobs.ForEach(LoadReferences);
            return jobs;
        }

        public Task<List<EconomicReleaseUpdateJobSettings>> GetEconomicReleaseUpdateJobs() =>
            GetJobs<EconomicReleaseUpdateJobSettings>(JobTypes.EconomicRelease);

        public async Task<List<DividendUpdateJobSettings>> GetDividendUpdateJobs()
        {
            var jobs = await GetJobs<DividendUpdateJobSettings>(JobTypes.DividendUpdate).ConfigureAwait(false);
            foreach (var job in jobs.Where(x => x.TagID.HasValue))
            {
                job.Tag = _context.Tags.FirstOrDefault(x => x.ID == job.TagID.Value);
            }

            return jobs;
        }

        public async Task<List<EarningsUpdateJobSettings>> GetEarningsUpdateJobs()
        {
            var jobs = await GetJobs<EarningsUpdateJobSettings>(JobTypes.EarningsUpdate).ConfigureAwait(false);
            foreach (var job in jobs.Where(x => x.TagID.HasValue))
            {
                job.Tag = _context.Tags.FirstOrDefault(x => x.ID == job.TagID.Value);
            }

            return jobs;
        }

        public async Task<IJobDetail> GetJobDetails(string name, string type)
        {
            return await _scheduler.GetJobDetail(new JobKey(name, type)).ConfigureAwait(false);
        }

        /// <exception cref="ArgumentNullException"><paramref name="job"/> is <see langword="null"/></exception>
        public async Task ScheduleJob(IJobSettings job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            _logger.Info($"Scheduling job {job.Name}");

            IJobDetail quartzJob = JobBuilder
                .Create<DataUpdateJob>() //the actual job type is determined by the JobFactory from the group
                .WithIdentity(job.Name, JobTypes.GetJobType(job))
                .UsingJobData("settings", JsonConvert.SerializeObject(job))
                .Build();
            try
            {
                await _scheduler.ScheduleJob(quartzJob, CreateTrigger(job)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Quartz Error scheduling job: " + ex.Message);
                throw;
            }
        }

        public async Task DeleteJob(IJobSettings job)
        {
            _logger.Info($"Deleting job {JobTypes.GetJobType(job)}.{job.Name}");
            await _scheduler.DeleteJob(new JobKey(job.Name, JobTypes.GetJobType(job))).ConfigureAwait(false);
        }

        /// <summary>
        /// Fire a job now, outside of its schedule.
        /// </summary>
        public async Task<bool> TriggerJob(string name, string type)
        {
            var key = new JobKey(name, type);
            if (!await _scheduler.Exists(key).ConfigureAwait(false)) return false;

            await _scheduler.TriggerJob(key).ConfigureAwait(false);
            return true;
        }

        private async Task<List<T>> GetJobs<T>(string group) where T : class
        {
            var result = new List<T>();
            var jobKeys = await _scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group)).ConfigureAwait(false);
            foreach (var key in jobKeys)
            {
                var details = await _scheduler.GetJobDetail(key).ConfigureAwait(false);
                var job = details == null ? null : Deserialize<T>(details);
                if (job != null) result.Add(job);
            }

            return result;
        }

        private T Deserialize<T>(IJobDetail details) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(details.JobDataMap.GetString("settings"));
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Failed to deserialize {typeof(T).Name} {details.Key}");
                return null;
            }
        }

        private void LoadReferences(DataUpdateJobSettings job)
        {
            if (job.InstrumentID.HasValue)
            {
                job.Instrument = _context.Instruments.FirstOrDefault(x => x.ID == job.InstrumentID.Value);
            }
            if (job.TagID.HasValue)
            {
                job.Tag = _context.Tags.FirstOrDefault(x => x.ID == job.TagID.Value);
            }
        }

        private static ITrigger CreateTrigger(IJobSettings jobSettings)
        {
            return TriggerBuilder
                .Create()
                .WithSchedule(GetScheduleBuilder(jobSettings))
                .WithIdentity(jobSettings.Name, JobTypes.GetJobType(jobSettings))
                .Build();
        }

        private static DailyTimeIntervalScheduleBuilder GetScheduleBuilder(IJobSettings jobSettings)
        {
            var builder = DailyTimeIntervalScheduleBuilder.Create();

            builder = jobSettings.WeekDaysOnly
                ? builder.OnMondayThroughFriday()
                : builder.OnEveryDay();

            return builder
                .StartingDailyAt(new TimeOnly(jobSettings.Time.Hours, jobSettings.Time.Minutes))
                .EndingDailyAfterCount(1);
        }
    }

    public interface IJobsRepository
    {
        Task<List<DataUpdateJobSettings>> GetDataUpdateJobs();

        Task<DataUpdateJobSettings> GetDataUpdateJob(string name);

        Task<List<EconomicReleaseUpdateJobSettings>> GetEconomicReleaseUpdateJobs();

        Task<List<DividendUpdateJobSettings>> GetDividendUpdateJobs();

        Task<List<EarningsUpdateJobSettings>> GetEarningsUpdateJobs();

        Task DeleteJob(IJobSettings job);

        Task ScheduleJob(IJobSettings job);

        Task<bool> TriggerJob(string name, string type);

        Task<IJobDetail> GetJobDetails(string name, string type);
    }
}
