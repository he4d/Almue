using System;

namespace AlmueRaspi.Interfaces
{
    public interface IShutter : IShuttable, IStoppable
    {
        int CompleteWayInSeconds { get; }
    }
}
