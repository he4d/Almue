namespace AlmueAndroid
{
    public class DeviceItem
    {
        public string Description { get; }

        public string Floor { get; }

        public DeviceType DeviceType { get; }

        public DeviceItem(string floor, string description, DeviceType type)
        {
            Floor = floor;
            Description = description;
            DeviceType = type;
        }
    }
}
