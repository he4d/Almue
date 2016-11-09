using System;
using System.Collections.Generic;
using System.Linq;
using AlmueRaspi.Interfaces;
using AlmueRaspi.Scheduler.Jobs;
using NLog;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;

namespace AlmueRaspi.Scheduler
{
    public class DeviceScheduler : IDeviceScheduler
    {
        #region private fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IScheduler _scheduler;

        #endregion

        #region constructor

        public DeviceScheduler()
        {
            _scheduler = StdSchedulerFactory.GetDefaultScheduler();
            _scheduler.Start();
        }

        #endregion

        #region private methods

        private void CreateAllJobsForDevice(ISchedulable device)
        {
            var validActions = ValidJobActionsForDevice(device);
            foreach (var action in validActions)
            {
                TimeSpan timeSpanUtc;
                if (TryGetTimeSpan(device, action, out timeSpanUtc))
                {
                    Type jobType;
                    if (TryGetJobType(device, action, out jobType))
                    {
                        var job = JobBuilder.Create(jobType)
                            .WithIdentity($"{device.Description}/{action}", device.DeviceTypeName)
                            .UsingJobData("groupName", device.DeviceTypeName)
                            .Build();
                        job.JobDataMap.Put(device.DeviceTypeName, device);
                        var trigger = TriggerBuilder.Create()
                            .WithIdentity($"{device.Description}/{action}", device.DeviceTypeName)
                            .StartNow()
                            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(timeSpanUtc.Hours, timeSpanUtc.Minutes)
                            .InTimeZone(TimeZoneInfo.Utc))
                            .ForJob(job)
                            .Build();

                        if (trigger != null)
                        {
                            _scheduler.ScheduleJob(job, trigger);
                            device.JobsCreated = true;
                        }
                    }
                }
            }
        }

        private void DeleteAllJobsOfDevice(ISchedulable device)
        {
            var validActions = ValidJobActionsForDevice(device);
            foreach (var action in validActions)
            {
                var jobKey = (from x in _scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(device.DeviceTypeName))
                              where x.Name.Equals($"{device.Description}/{action}")
                              select x).FirstOrDefault();
                if (jobKey != null)
                {
                    _scheduler.DeleteJob(jobKey);
                    device.JobsCreated = false;
                }
            }
        }

        #endregion

        #region public methods

        public void UpdateTrigger(ISchedulable device, JobActions action)
        {
            if (device.JobsCreated)
            {
                var oldTrigger = _scheduler.GetTrigger(new TriggerKey($"{device.Description}/{action}", device.DeviceTypeName));
                if (oldTrigger != null)
                {
                    TimeSpan timeSpanUtc;
                    if (TryGetTimeSpan(device, action, out timeSpanUtc))
                    {
                        var tb = oldTrigger.GetTriggerBuilder();
                        var newTrigger = tb
                            .StartNow()
                            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(timeSpanUtc.Hours, timeSpanUtc.Minutes)
                            .InTimeZone(TimeZoneInfo.Utc))
                            .Build();
                        _scheduler.RescheduleJob(oldTrigger.Key, newTrigger);
                    }
                }
            }
        }

        public void DisableJobsOfDevice(ISchedulable device)
        {
            if (device.JobsCreated)
            {
                DeleteAllJobsOfDevice(device);
            }
        }

        public void EnableJobsOfDevice(ISchedulable device)
        {
            if (!device.JobsCreated)
            {
                CreateAllJobsForDevice(device);
            }
        }

        #endregion

        #region private static methods

        private static bool TryGetTimeSpan(ISchedulable device, JobActions action, out TimeSpan timeSpanUtc)
        {
            switch (action)
            {
                case JobActions.Close:
                case JobActions.Off:
                    timeSpanUtc = device.OffTime.LocalToUtc();
                    break;
                case JobActions.Open:
                case JobActions.On:
                    timeSpanUtc = device.OnTime.LocalToUtc();
                    break;
                default:
                    Logger.Error(
                        $"Time could not be set for device: {device.Description} - {device.GetType().Name} - action: {action}");
                    timeSpanUtc = new TimeSpan(0);
                    return false;
            }
            return true;
        }

        private static IEnumerable<JobActions> ValidJobActionsForDevice(ISchedulable device)
        {
            var retval = new List<JobActions>();
            if (device is IShuttable)
            {
                retval.Add(JobActions.Open);
                retval.Add(JobActions.Close);
            }
            else if (device is ISwitchable)
            {
                retval.Add(JobActions.On);
                retval.Add(JobActions.Off);
            }
            else
            {
                Logger.Error($"ValidJobActionsForDevice method could not resolve the JobActions for {device.Description} - {device.GetType().Name}");
            }
            return retval;
        }

        private static bool TryGetJobType(ISchedulable device, JobActions action, out Type jobType)
        {
            jobType = null;
            if (device is IShuttable)
            {
                switch (action)
                {
                    case JobActions.Open:
                        jobType = typeof(ShuttableOpenJob);
                        break;
                    case JobActions.Close:
                        jobType = typeof(ShuttableCloseJob);
                        break;
                    default:
                        Logger.Error($"TryGetJobType method - Action {action} not implemented for device {device.Description} - {device.GetType().Name}");
                        return false;
                }
            }
            else if (device is ISwitchable)
            {
                switch (action)
                {
                    case JobActions.On:
                        jobType = typeof(SwitchableOnJob);
                        break;
                    case JobActions.Off:
                        jobType = typeof(SwitchableOffJob);
                        break;
                    default:
                        Logger.Error($"TryGetJobType method - Action {action} not implemented for device {device.Description} - {device.GetType().Name}");
                        return false;
                }
            }
            else
            {
                Logger.Error($"TryGetJobType method could not resolve the JobType for {device.Description} - {device.GetType().Name}");
                return false;
            }
            return true;
        }

        #endregion
    }
}
