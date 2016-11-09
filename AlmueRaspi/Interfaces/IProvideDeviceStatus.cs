namespace AlmueRaspi.Interfaces
{
    public enum DeviceStatus
    {
        Undefined,
        Opened,
        Closed,
        On,
        Off,
        FailState
    }

    public interface IProvideDeviceStatus : IDevice
    {
        DeviceStatus DeviceStatus { get; }
    }
}
