// -----------------------------------------------------------------------
// <copyright file="JobsManager.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using NLog;
using QDMS;
using Quartz;

// Takes DataUpdateJobs from the QDMS system and creates appropriate jobs in the scheduler.

namespace QDMSServer
{
    public static class JobsManager
    {
        public static void ScheduleJobs(IScheduler scheduler, List<DataUpdateJobDetails> jobs)
        {
            if (jobs == null || jobs.Count == 0) return;

            //start by clearing all the jobs
            scheduler.Clear();

            //then convert and add them
            foreach (DataUpdateJobDetails job in jobs)
            {
                IDictionary map = new Dictionary<string, object> { { "details", job } };

                IJobDetail quartzJob = JobBuilder
                    .Create<DataUpdateJob>()
                    .WithIdentity(job.Name)
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

        private static ITrigger CreateTrigger(DataUpdateJobDetails jobDetails)
        {
            ITrigger trigger = TriggerBuilder
                .Create()
                .WithSchedule(GetScheduleBuilder(jobDetails))
                .WithIdentity(jobDetails.Name + "Trigger")
                .Build();
            
            return trigger;
        }

        private static DailyTimeIntervalScheduleBuilder GetScheduleBuilder(DataUpdateJobDetails jobDetails)
        {
            if(jobDetails.WeekDaysOnly)
            {
                return DailyTimeIntervalScheduleBuilder
                    .Create()
                    .OnMondayThroughFriday()
                    .StartingDailyAt(new TimeOfDay(jobDetails.Time.Hours, jobDetails.Time.Minutes))
                    .EndingDailyAfterCount(1);
            }
            else
            {
                return DailyTimeIntervalScheduleBuilder
                    .Create()
                    .OnEveryDay()
                    .StartingDailyAt(new TimeOfDay(jobDetails.Time.Hours, jobDetails.Time.Minutes))
                    .EndingDailyAfterCount(1);
            }
        }
    }
}
