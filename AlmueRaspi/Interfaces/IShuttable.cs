namespace AlmueRaspi.Interfaces
{
    public interface IShuttable : IDevice
    {
        void Open();

        void Close();
    }
}
