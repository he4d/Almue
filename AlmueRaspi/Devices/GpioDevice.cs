using System.Collections.Generic;
using AlmueRaspi.Interfaces;
using Raspberry.IO.GeneralPurpose;

namespace AlmueRaspi.Devices
{
    public abstract class GpioDevice : IDevice
    {
        protected GpioDevice(GpioConnection gpioConnection)
        {
            GpioConnection = gpioConnection;
        }

        protected abstract IEnumerable<PinConfiguration> AllDevicePins { get; }

        protected GpioConnection GpioConnection { get; }

        public abstract string DeviceTypeName { get; }

        public abstract string Description { get; }

        public abstract string Floor { get; }
    }
}
