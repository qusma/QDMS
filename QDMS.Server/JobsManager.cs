// -----------------------------------------------------------------------
// <copyright file="JobsManager.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using NLog;
using QDMS;
using QDMS.Server.Jobs;
using Quartz;

namespace QDMSServer
{
    public static class JobsManager
    {
        public static void ScheduleJob(IScheduler scheduler, IJobSettings job)
        {
            ScheduleJobs(scheduler, new[] { job });
        }

        public static void ScheduleJobs<T>(IScheduler scheduler, IEnumerable<T> jobs) where T: IJobSettings
        {
            if (jobs == null) return;

            //then convert and add them
            foreach (IJobSettings job in jobs)
            {
                IDictionary map = new Dictionary<string, string> { { "settings", JsonConvert.SerializeObject(job) } };

                IJobDetail quartzJob = JobBuilder
                    .Create<DataUpdateJob>()
                    .WithIdentity(job.Name, JobTypes.GetJobType(job))
                    .UsingJobData(new JobDataMap(map))
                    .Build();
                try
                {
                    scheduler.ScheduleJob(quartzJob, CreateTrigger(job));
                }
                catch(Exception ex)
                {
                    var logger = LogManager.GetCurrentClassLogger();
                    logger.Log(LogLevel.Error, "Quartz Error scheduling job: " + ex.Message);
                }
            }
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
}
