using AlmueRaspi.Interfaces;
using Quartz;
using NLog;

namespace AlmueRaspi.Scheduler.Jobs
{
    public class ShuttableOpenJob : IJob
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;
            var devTypeName = dataMap.GetString("groupName");
            var device = (IShuttable)dataMap.Get(devTypeName);
            var deviceCasted = device as IDevice;
            if (deviceCasted == null)
                Logger.Fatal("This Device does not implement IDevice!");
            else
                Logger.Info($"Started for: {deviceCasted.Description} - {deviceCasted.Floor}");
            device.Open();
        }
    }
}
