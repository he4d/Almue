using AlmueRaspi.Interfaces;
using Raspberry.IO.GeneralPurpose;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AlmueRaspi.Configuration
{
    public class ConfigurationObject
    {
        public BrokerConfig Broker { get; set; }

        public DevicesConfig Devices { get; set; }

        public class BrokerConfig
        {
            public string Ip { get; set; }

            public int Port { get; set; }

            public string User { get; set; }

            public string Password { get; set; }
        }

        public class DevicesConfig
        {
            public ICollection<ShutterConfig> Shutters { get; set; }

            public ICollection<LightingConfig> Lightings { get; set; }

            public ICollection<WindMonitorConfig> WindMonitors { get; set; }

            public class ShutterConfig
            {
                public string Description { get; set; }

                public string Floor { get; set; }

                public ConnectorPin OpenPin { get; set; }

                public ConnectorPin ClosePin { get; set; }

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

                public ConnectorPin SwitchPin { get; set; }

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

                public ConnectorPin InPin { get; set; }

                public bool Disabled { get; set; }
            }
        }
    }
}
