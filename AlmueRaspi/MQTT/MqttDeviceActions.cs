namespace AlmueRaspi.MQTT
{
    public enum MqttDeviceActions
    {
        None = 0,
        Open,
        Close,
        Stop,
        On,
        Off,
        SetOnTime,
        SetOffTime,
        DisableTimer,
        EnableTimer,
        DisableDevice,
        EnableDevice,
        EnableEmergency,
        DisableEmergency
    }
}
