﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Quartz;

namespace Topshelf.SimpleInjector.Quartz
{
    public class QuartzConfigurator
    {
        #region Private Fields

        private ICollection<Func<ITrigger>> _triggers;
        private ICollection<Tuple<Func<IJobListener>, IMatcher<JobKey>[]>> _jobListeners;

        #endregion

        #region Properties

        public Func<IJobDetail> Job { get; private set; }

        public ICollection<Func<ITrigger>> Triggers
        {
            get { return _triggers ?? (_triggers = new Collection<Func<ITrigger>>()); }
        }

        public ICollection<Tuple<Func<IJobListener>, IMatcher<JobKey>[]>> JobListeners
        {
            get { return _jobListeners ?? (_jobListeners = new Collection<Tuple<Func<IJobListener>, IMatcher<JobKey>[]>>()); }
        }

        public Func<bool> JobEnabled { get; private set; }

        #endregion

        #region Quartz Builder Framework

        public QuartzConfigurator WithJob(Func<IJobDetail> jobDetail)
        {
            Job = jobDetail;
            return this;
        }

        public QuartzConfigurator AddTrigger(Func<ITrigger> jobTrigger)
        {
            Triggers.Add(jobTrigger);
            return this;
        }

        public QuartzConfigurator EnableJobWhen(Func<bool> jobEnabled)
        {
            JobEnabled = jobEnabled;
            return this;
        }

        public QuartzConfigurator AddJobListener(Func<IJobListener> jobListener, params IMatcher<JobKey>[] keyEquals)
        {
            JobListeners.Add(Tuple.Create(jobListener, keyEquals));
            return this;
        }

        #endregion

        #region Simple Configuration Extensions

        /// <summary>
        /// Create a job with a CronSchedule
        /// </summary>
        /// <typeparam name="TJob">The Job that is registered with the SimpleInjector container</typeparam>
        /// <param name="cronExpression">The cronExpression the job must be triggered by</param>
        /// <param name="tz">Timezone of cron expression</param>
        /// <param name="jobIdentity">Unique Identifier for the Job. If null is passed, the namespace including class name will be used</param>
        /// <returns>The QuartzConfigurator for chained constructions</returns>
        public QuartzConfigurator WithCronSchedule<TJob>(string cronExpression, TimeZoneInfo tz, string jobIdentity = null) where TJob : IJob
        {
            if (CronExpression.IsValidExpression(cronExpression))
            {
                CreateJobDetailFunc<TJob>(jobIdentity);

                IScheduleBuilder scheduleBuilder = CronScheduleBuilder
                    .CronSchedule(cronExpression)
                    .InTimeZone(tz);

                Func<ITrigger> trigger = () => TriggerBuilder
                    .Create()
                    .WithSchedule(scheduleBuilder)
                    .Build();
                AddTrigger(trigger);

                return this;
            }

            throw new ArgumentException("must specify a valid cron expression", "cronExpression");
        }

        /// <summary>
        /// Create a job with a simple forever repeatable schedule defined by a TimeSpan
        /// </summary>
        /// <typeparam name="TJob">The Job that is registered with the SimpleInjector container</typeparam>
        /// <param name="timeSpan">The TimeSpan the job must be triggered by</param>
        /// <param name="jobIdentity">Unique Identifier for the Job. If null is passed, the namespace including class name will be used</param>
        /// <returns>The QuartzConfigurator for chained constructions</returns>
        public QuartzConfigurator WithSimpleRepeatableSchedule<TJob>(TimeSpan timeSpan, string jobIdentity = null) where TJob : IJob
        {
            CreateJobDetailFunc<TJob>(jobIdentity);

            Func<ITrigger> trigger = () => TriggerBuilder
                .Create()
                .WithSimpleSchedule(builder => builder
                    .WithInterval(timeSpan)
                    .RepeatForever())
                .Build();
            AddTrigger(trigger);

            return this;
        }

        #endregion

        #region Private Methods

        private void CreateJobDetailFunc<TJob>(string jobIdentity) where TJob : IJob
        {
            if (string.IsNullOrWhiteSpace(jobIdentity))
            {
                jobIdentity = typeof(TJob).ToString();
            }

            Func<IJobDetail> jobDetail = () => JobBuilder
                .Create<TJob>()
                .WithIdentity(jobIdentity)
                .Build();
            WithJob(jobDetail);
        }

        #endregion
    }
}