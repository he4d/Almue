using AlmueRaspi.Devices;
using System;
using NLog;

namespace AlmueRaspi.MQTT
{
    public class MqttDeviceEventArgs : EventArgs
    {
        #region private fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region public properties

        public string DeviceDescription { get; }

        public Type DeviceType { get; }

        public MqttDeviceActions DeviceAction { get; }

        public string Message { get; }

        #endregion

        #region constructor

        public MqttDeviceEventArgs(string deviceDescription, Type deviceType,
            MqttDeviceActions deviceAction, string message)
        {
            DeviceAction = deviceAction;
            DeviceType = deviceType;
            DeviceDescription = deviceDescription;
            Message = message;
        }

        #endregion

        #region public methods

        public static bool TryParse(string topic, byte[] message, string deviceDescription, out MqttDeviceEventArgs mqttDeviceEventArgs)
        {
            mqttDeviceEventArgs = null;
            var messageString = System.Text.Encoding.UTF8.GetString(message);
            Type deviceType;
            MqttDeviceActions deviceAction;
            if (topic.StartsWith(MqttTopicGenerator.ShutterTopicPrefix))
            {
                deviceType = typeof(Shutter);
                if (topic.EndsWith("timeron"))
                {
                    deviceAction = MqttDeviceActions.SetOnTime;
                }
                else if (topic.EndsWith("timeroff"))
                {
                    deviceAction = MqttDeviceActions.SetOffTime;
                }
                else if (topic.EndsWith(deviceDescription))
                {
                    switch (messageString)
                    {
                        case "open":
                            deviceAction = MqttDeviceActions.Open;
                            break;
                        case "close":
                            deviceAction = MqttDeviceActions.Close;
                            break;
                        case "stop":
                            deviceAction = MqttDeviceActions.Stop;
                            break;
                        case "disable":
                            deviceAction = MqttDeviceActions.DisableDevice;
                            break;
                        case "enable":
                            deviceAction = MqttDeviceActions.EnableDevice;
                            break;
                        case "disabletimer":
                            deviceAction = MqttDeviceActions.DisableTimer;
                            break;
                        case "enabletimer":
                            deviceAction = MqttDeviceActions.EnableTimer;
                            break;
                        case "enableemergency":
                            deviceAction = MqttDeviceActions.EnableEmergency;
                            break;
                        case "disableemergency":
                            deviceAction = MqttDeviceActions.DisableEmergency;
                            break;
                        default:
                            return false;
                    }
                }
                else
                {
                    Logger.Fatal($"Shutter topic: {topic} not subscribed by MqttController, something went terribly wrong..");
                    return false;
                }
            }
            else if (topic.StartsWith(MqttTopicGenerator.LightingTopicPrefix))
            {
                deviceType = typeof(Lighting);
                if (topic.EndsWith(deviceDescription))
                {
                    switch (messageString)
                    {
                        case "on":
                            deviceAction = MqttDeviceActions.On;
                            break;
                        case "off":
                            deviceAction = MqttDeviceActions.Off;
                            break;
                        case "disable":
                            deviceAction = MqttDeviceActions.DisableDevice;
                            break;
                        case "enable":
                            deviceAction = MqttDeviceActions.EnableDevice;
                            break;
                        case "disabletimer":
                            deviceAction = MqttDeviceActions.DisableTimer;
                            break;
                        case "enabletimer":
                            deviceAction = MqttDeviceActions.EnableTimer;
                            break;
                        default:
                            return false;
                    }
                }
                else if (topic.EndsWith("timeron"))
                {
                    deviceAction = MqttDeviceActions.SetOnTime;
                }
                else if (topic.EndsWith("timeroff"))
                {
                    deviceAction = MqttDeviceActions.SetOffTime;
                }
                else
                {
                    Logger.Fatal($"Lighting topic: {topic} not subscribed by MqttController, something went terribly wrong..");
                    return false;
                }
            }
            else
            {
                Logger.Fatal($"Other topic: {topic} not subscribed by MqttController, something went terribly wrong..");
                return false;
            }
            mqttDeviceEventArgs = new MqttDeviceEventArgs(deviceDescription, deviceType, deviceAction, messageString);
            return true;
        }

        #endregion
    }
}
