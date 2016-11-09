using AlmueRaspi.Scheduler.Jobs;

namespace AlmueRaspi.Interfaces
{
    public interface IDeviceScheduler
    {
        void UpdateTrigger(ISchedulable device, JobActions action);

        void DisableJobsOfDevice(ISchedulable device);

        void EnableJobsOfDevice(ISchedulable device);
    }
}
