using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AlmueAndroid
{
    public enum DeviceStatus
    {
        Undefined = 0,
        Opened,
        Closed,
        On,
        Off,
        FailState
    }

    public class DevicesConfigObject
    {
        public ICollection<ShutterConfig> Shutters { get; set; }

        public ICollection<LightingConfig> Lightings { get; set; }

        public ICollection<WindMonitorConfig> WindMonitors { get; set; }

        public class ShutterConfig
        {
            public string Description { get; set; }

            public string Floor { get; set; }

            public string OpenPin { get; set; }

            public string ClosePin { get; set; }

            public int CompleteWayInSeconds { get; set; }

            public bool EmergencyEnabled { get; set; }

            public bool Disabled { get; set; }

            public bool TimerEnabled { get; set; }

            public TimeSpan OpenTime { get; set; }

            public TimeSpan CloseTime { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public DeviceStatus DeviceStatus { get; set; }
        }

        public class LightingConfig
        {
            public string Description { get; set; }

            public string Floor { get; set; }

            public string SwitchPin { get; set; }

            public bool Disabled { get; set; }

            public bool TimerEnabled { get; set; }

            public TimeSpan OnTime { get; set; }

            public TimeSpan OffTime { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public DeviceStatus DeviceStatus { get; set; }
        }

        public class WindMonitorConfig
        {
            public string Description { get; set; }

            public string InPin { get; set; }

            public bool Disabled { get; set; }
        }
    }
}
