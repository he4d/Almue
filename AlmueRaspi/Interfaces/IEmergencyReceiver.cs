using System;

namespace AlmueRaspi.Interfaces
{
    public interface IEmergencyReceiver
    {
        bool EmergencyEnabled { get; set; }

        void EmergencyEventIncoming(object sender, EventArgs e);
    }
}
