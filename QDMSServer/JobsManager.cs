// -----------------------------------------------------------------------
// <copyright file="JobsManager.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
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
                    .Create()
                    .WithIdentity(job.Name)
                    .UsingJobData(new JobDataMap(map))
                    .Build();
                scheduler.ScheduleJob(quartzJob, CreateTrigger(job));
            }
        }

        private static ITrigger CreateTrigger(DataUpdateJobDetails jobDetails)
        {
            ITrigger trigger = TriggerBuilder
                .Create()
                .WithSchedule(GetScheduleBuilder(jobDetails))
                .StartNow()
                .WithIdentity(jobDetails.Name + "Trigger")
                .Build();
            
            return trigger;
        }

        private static CronScheduleBuilder GetScheduleBuilder(DataUpdateJobDetails jobDetails)
        {
            if(jobDetails.WeekDaysOnly)
            {
                return CronScheduleBuilder.AtHourAndMinuteOnGivenDaysOfWeek(jobDetails.Time.Hours, jobDetails.Time.Minutes,
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday);
            }
            else
            {
                return CronScheduleBuilder.DailyAtHourAndMinute(jobDetails.Time.Hours, jobDetails.Time.Minutes);
            }
        }
    }
}
