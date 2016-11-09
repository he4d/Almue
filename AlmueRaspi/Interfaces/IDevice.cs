namespace AlmueRaspi.Interfaces
{
    public interface IDevice
    {
        string DeviceTypeName { get; }

        string Description { get; }

        string Floor { get; }
    }
}
