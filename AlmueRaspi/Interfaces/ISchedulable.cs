using System;

namespace AlmueRaspi.Interfaces
{
    public interface ISchedulable : IDevice
    {
        bool JobsCreated { get; set; }

        bool TimerEnabled { get; set; }

        TimeSpan OnTime { get; set; }

        TimeSpan OffTime { get; set; }
    }
}
