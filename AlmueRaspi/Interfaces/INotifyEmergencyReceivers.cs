using System;

namespace AlmueRaspi.Interfaces
{
    public interface INotifyEmergencyReceivers
    {
        event EventHandler NotifyEmergencyEvent;
    }
}
