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
        public static void ScheduleJob(IScheduler scheduler, IJobDetails job)
        {
            ScheduleJobs(scheduler, new[] { job });
        }

        public static void ScheduleJobs<T>(IScheduler scheduler, IEnumerable<T> jobs) where T: IJobDetails
        {
            if (jobs == null) return;

            //then convert and add them
            foreach (IJobDetails job in jobs)
            {
                IDictionary map = new Dictionary<string, string> { { "settings", JsonConvert.SerializeObject(job) } };

                IJobDetail quartzJob = JobBuilder
                    .Create<DataUpdateJob>()
                    .WithIdentity(job.Name, JobTypes.DataUpdate)
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

        private static ITrigger CreateTrigger(IJobDetails jobDetails)
        {
            ITrigger trigger = TriggerBuilder
                .Create()
                .WithSchedule(GetScheduleBuilder(jobDetails))
                .WithIdentity(jobDetails.Name, JobTypes.DataUpdate)
                .Build();
            
            return trigger;
        }

        private static DailyTimeIntervalScheduleBuilder GetScheduleBuilder(IJobDetails jobDetails)
        {
            var builder = DailyTimeIntervalScheduleBuilder.Create();

            builder = jobDetails.WeekDaysOnly 
                ? builder.OnMondayThroughFriday() 
                : builder.OnEveryDay();

            return builder
                .StartingDailyAt(new TimeOfDay(jobDetails.Time.Hours, jobDetails.Time.Minutes))
                .EndingDailyAfterCount(1);
        }
    }
}
