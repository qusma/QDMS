// -----------------------------------------------------------------------
// <copyright file="JobsRepository.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Newtonsoft.Json;
using NLog;
using QDMS.Server.Jobs;
using QDMSServer;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QDMS.Server.Repositories
{
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

        public DataUpdateJobSettings GetDataUpdateJob(string name)
        {
            var details = _scheduler.GetJobDetail(new JobKey(name, JobTypes.DataUpdate));
            return DeserializeDataUpdateJobSettings(new[] { details }).FirstOrDefault();
        }

        public List<DataUpdateJobSettings> GetDataUpdateJobs()
        {
            var jobKeys = _scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(JobTypes.DataUpdate));
            var jobDetails = jobKeys.Select(x => _scheduler.GetJobDetail(x)).ToList();
            return DeserializeDataUpdateJobSettings(jobDetails);
        }

        private List<DataUpdateJobSettings> DeserializeDataUpdateJobSettings(IEnumerable<IJobDetail> jobDetails)
        {
            var result = new List<DataUpdateJobSettings>();
            foreach (var job in jobDetails)
            {
                try
                {
                    var jd = JsonConvert.DeserializeObject<DataUpdateJobSettings>((string)job.JobDataMap["settings"]);
                    if (jd.InstrumentID.HasValue)
                    {
                        jd.Instrument = _context.Set<Instrument>().FirstOrDefault(x => x.ID == jd.InstrumentID.Value);
                    }
                    if (jd.TagID.HasValue)
                    {
                        jd.Tag = _context.Set<Tag>().FirstOrDefault(x => x.ID == jd.TagID.Value);
                    }

                    result.Add(jd);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to deserialize data update job");
                }
            }

            return result;
        }

        public List<EconomicReleaseUpdateJobSettings> GetEconomicReleaseUpdateJobs()
        {
            var result = new List<EconomicReleaseUpdateJobSettings>();
            var jobKeys = _scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(JobTypes.EconomicRelease));
            foreach (var job in jobKeys)
            {
                IJobDetail jobDetails = _scheduler.GetJobDetail(job);

                try
                {
                    var jd = JsonConvert.DeserializeObject<EconomicReleaseUpdateJobSettings>((string)jobDetails.JobDataMap["settings"]);

                    result.Add(jd);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to deserialize economic release update job");
                }
            }

            return result;
        }

        public List<DividendUpdateJobSettings> GetDividendUpdateJobs()
        {
            var result = new List<DividendUpdateJobSettings>();
            var jobKeys = _scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(JobTypes.DividendUpdate));

            foreach (var job in jobKeys)
            {
                IJobDetail jobDetails = _scheduler.GetJobDetail(job);

                try
                {
                    var jd = JsonConvert.DeserializeObject<DividendUpdateJobSettings>((string)jobDetails.JobDataMap["settings"]);

                    if (jd.TagID.HasValue)
                    {
                        jd.Tag = _context.Set<Tag>().FirstOrDefault(x => x.ID == jd.TagID.Value);
                    }

                    result.Add(jd);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to deserialize dividend update job");
                }
            }

            return result;
        }

        public IJobDetail GetJobDetails(string name, string type)
        {
            return _scheduler.GetJobDetail(new JobKey(name, type));
        }

        /// <summary>
        /// </summary>
        /// <param name="job"></param>
        /// <exception cref="ArgumentNullException"><paramref name="job"/> is <see langword="null"/></exception>
        public void ScheduleJob(IJobSettings job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            _logger.Info($"Scheduling job {job.Name}");

            //convert and add them
            IDictionary map = new Dictionary<string, string> { { "settings", JsonConvert.SerializeObject(job) } };

            IJobDetail quartzJob = JobBuilder
                .Create<DataUpdateJob>()
                .WithIdentity(job.Name, JobTypes.GetJobType(job))
                .UsingJobData(new JobDataMap(map))
                .Build();
            try
            {
                _scheduler.ScheduleJob(quartzJob, CreateTrigger(job));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Quartz Error scheduling job: " + ex.Message);
                throw;
            }
        }

        public void DeleteJob(IJobSettings job)
        {
            _logger.Info($"Deleting job {JobTypes.GetJobType(job)}.{job.Name}");
            _scheduler.DeleteJob(new JobKey(job.Name, JobTypes.GetJobType(job)));
        }

        private static ITrigger CreateTrigger(IJobSettings jobSettings)
        {
            ITrigger trigger = TriggerBuilder
                .Create()
                .WithSchedule(GetScheduleBuilder(jobSettings))
                .WithIdentity(jobSettings.Name, JobTypes.GetJobType(jobSettings))
                .Build();

            return trigger;
        }

        private static DailyTimeIntervalScheduleBuilder GetScheduleBuilder(IJobSettings jobSettings)
        {
            var builder = DailyTimeIntervalScheduleBuilder.Create();

            builder = jobSettings.WeekDaysOnly
                ? builder.OnMondayThroughFriday()
                : builder.OnEveryDay();

            return builder
                .StartingDailyAt(new TimeOfDay(jobSettings.Time.Hours, jobSettings.Time.Minutes))
                .EndingDailyAfterCount(1);
        }
    }

    public interface IJobsRepository
    {
        List<DataUpdateJobSettings> GetDataUpdateJobs();

        DataUpdateJobSettings GetDataUpdateJob(string name);

        List<EconomicReleaseUpdateJobSettings> GetEconomicReleaseUpdateJobs();

        void DeleteJob(IJobSettings job);

        void ScheduleJob(IJobSettings job);

        IJobDetail GetJobDetails(string name, string type);
        List<DividendUpdateJobSettings> GetDividendUpdateJobs();
    }
}