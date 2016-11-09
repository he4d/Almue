using System.Collections.Generic;
using System.Linq;
using AlmueRaspi.Configuration;

namespace AlmueRaspi.MQTT
{
    static class MqttTopicGenerator
    {
        #region public const fields

        public const string ShutterTopicPrefix = "almue/shutter/";
        public const string LightingTopicPrefix = "almue/lighting/";
        public const string ConfigTopic = "almue/config";

        #endregion

        #region public methods

        public static Dictionary<string, List<string>> GenerateDeviceWithTopicsDictionary(ConfigurationObject.DevicesConfig devices)
        {
            var retval = new Dictionary<string, List<string>>();

            if (devices.Shutters != null)
            {
                foreach (var device in devices.Shutters)
                {
                    var topicsList = new List<string>
                    {
                    $"{ShutterTopicPrefix}{device.Floor}/{device.Description}",
                    $"{ShutterTopicPrefix}{device.Floor}/{device.Description}/timeron",
                    $"{ShutterTopicPrefix}{device.Floor}/{device.Description}/timeroff",
                    $"{ShutterTopicPrefix}{device.Floor}/{device.Description}/status",
                    };
                    retval.Add(device.Description, topicsList);
                }
            }

            if (devices.Lightings != null)
            {
                foreach (var device in devices.Lightings)
                {
                    var topicsList = new List<string>
                    {
                        $"{LightingTopicPrefix}{device.Floor}/{device.Description}",
                        $"{LightingTopicPrefix}{device.Floor}/{device.Description}/status",
                    };
                    retval.Add(device.Description, topicsList);
                }
            }

            return retval;
        }

        #endregion
    }
}
