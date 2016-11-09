using System;
using System.Linq;
using AlmueRaspi.Devices;
using AlmueRaspi.Interfaces;
using Newtonsoft.Json;
using NLog;

namespace AlmueRaspi.Configuration
{
    public class ConfigurationController : IConfigurationController
    {

        #region private fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _configPath;

        #endregion

        #region constructor

        public ConfigurationController(string configPath)
        {
            _configPath = configPath;
            var text = System.IO.File.ReadAllText(configPath);
            Config = JsonConvert.DeserializeObject<ConfigurationObject>(text);
        }

        #endregion

        #region public methods

        public void ConfigEntryChanged(object sender, ConfigPropertyChangedEventArgs e)
        {
            if (sender is Lighting)
            {
                var lighting = (Lighting)sender;
                var discreteConfigObject =
                    (from x in Config.Devices.Lightings where x.Description.Equals(lighting.Description) select x).FirstOrDefault();
                if (discreteConfigObject != null)
                {
                    switch (e.ConfigPropertyName)
                    {
                        case "Disabled":
                            discreteConfigObject.Disabled = lighting.Disabled;
                            break;
                        case "TimerEnabled":
                            discreteConfigObject.TimerEnabled = lighting.TimerEnabled;
                            break;
                        case "OffTime":
                            discreteConfigObject.OffTime = lighting.OffTime;
                            break;
                        case "OnTime":
                            discreteConfigObject.OnTime = lighting.OnTime;
                            break;
                        case "DeviceStatus":
                            discreteConfigObject.DeviceStatus = lighting.DeviceStatus;
                            break;
                    }
                }
                else
                    Logger.Fatal($"Configobject for {lighting.Description} not found!");
            }
            else if (sender is Shutter)
            {
                var shutter = (Shutter)sender;
                var discreteConfigObject =
                    (from x in Config.Devices.Shutters where x.Description.Equals(shutter.Description) select x).FirstOrDefault();
                if (discreteConfigObject != null)
                {
                    switch (e.ConfigPropertyName)
                    {
                        case "Disabled":
                            discreteConfigObject.Disabled = shutter.Disabled;
                            break;
                        case "TimerEnabled":
                            discreteConfigObject.TimerEnabled = shutter.TimerEnabled;
                            break;
                        case "OffTime":
                            discreteConfigObject.CloseTime = shutter.OffTime;
                            break;
                        case "OnTime":
                            discreteConfigObject.OpenTime = shutter.OnTime;
                            break;
                        case "DeviceStatus":
                            discreteConfigObject.DeviceStatus = shutter.DeviceStatus;
                            break;
                    }
                }
                else
                    Logger.Fatal($"Configobject for {shutter.Description} not found!");
            }
            else if (sender is WindMonitor)
            {
                var windMonitor = (WindMonitor)sender;
                var discreteConfigObject =
                    (from x in Config.Devices.WindMonitors where x.Description.Equals(windMonitor.Description) select x).FirstOrDefault();
                if (discreteConfigObject != null)
                {
                    switch (e.ConfigPropertyName)
                    {
                        case "Disabled":
                            discreteConfigObject.Disabled = windMonitor.Disabled;
                            break;
                    }
                }
                Logger.Fatal($"Configobject for {windMonitor.Description} not found!");
            }
            else
                return;

            SaveConfigFile();

            OnConfigChange?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region private methods

        private void SaveConfigFile()
        {
            System.IO.File.WriteAllText(_configPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
        }

        #endregion

        #region IConfigurationController implementation

        public byte[] JsonConfigurationByteArray => System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Config.Devices));

        public ConfigurationObject Config { get; }

        public event EventHandler OnConfigChange;

        #endregion

    }
}
