namespace AlmueRaspi.Interfaces
{
    public interface ISwitchable : IDevice
    {
        void SwitchOn();

        void SwitchOff();
    }
}
