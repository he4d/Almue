using System;
using System.Collections.Generic;
using System.Linq;
using uPLibrary.Networking.M2Mqtt;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Threading.Tasks;

namespace AlmueAndroid
{
    public class MqttController
    {
        private static MqttController _instance;

        public static MqttController Instance => _instance ?? (_instance = new MqttController());

        private MqttClient _mqttClient;

        public event EventHandler ReceivedConfig;

        public event EventHandler FailedToConnect;

        public event EventHandler ConnectionToBrokerClosed;

        public DevicesConfigObject ConfigObject { get; private set; }

        private MqttController() { }

        public bool IsConnectedToBroker => _mqttClient != null && _mqttClient.IsConnected;

        public IEnumerable<string> AllConfiguredFloors
        {
            get
            {
                if (ConfigObject != null)
                {
                    var retval = new List<string>();
                    if (ConfigObject.Shutters != null)
                        retval.AddRange(from x in ConfigObject.Shutters select x.Floor);
                    if (ConfigObject.Lightings != null)
                        retval.AddRange(from x in ConfigObject.Lightings select x.Floor);
                    return retval.Distinct();
                }
                return null;
            }
        }

        public void ConnectToBroker(string brokerHostName, string userName, string password)
        {
            _mqttClient = new MqttClient(brokerHostName);
            _mqttClient.MqttMsgPublishReceived -= MqttMsgPublishReceived;
            _mqttClient.MqttMsgPublishReceived += MqttMsgPublishReceived;
            _mqttClient.ConnectionClosed -= MqttConnectionClosed;
            _mqttClient.ConnectionClosed += MqttConnectionClosed;
            Task.Factory.StartNew(() =>
            {
                _mqttClient.Connect(Guid.NewGuid().ToString(), userName, password);
            }).ContinueWith(ConnectingEnded);
        }

        private void MqttConnectionClosed(object sender, EventArgs e) => ConnectionToBrokerClosed?.Invoke(this, EventArgs.Empty);

        private void ConnectingEnded(Task task)
        {
            if (task.IsFaulted || task.IsCanceled)
                FailedToConnect?.Invoke(this, EventArgs.Empty);
            if (task.IsCompleted && _mqttClient.IsConnected)
                _mqttClient.Subscribe(new[] { "almue/config" }, new[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        public IEnumerable<DevicesConfigObject.ShutterConfig> GetAllShuttersOfFloor(string floor) =>
            (from x in ConfigObject.Shutters where x.Floor.Equals(floor) select x);

        public IEnumerable<DevicesConfigObject.LightingConfig> GetAllLightingsOfFloor(string floor) =>
            (from x in ConfigObject.Lightings where x.Floor.Equals(floor) select x);

        public DevicesConfigObject.ShutterConfig GetShutterByTag(string floor, string desc) =>
            (from x in ConfigObject.Shutters where x.Description.Equals(desc) && x.Floor.Equals(floor) select x).FirstOrDefault();

        public DevicesConfigObject.LightingConfig GetLightingByTag(string floor, string desc) =>
            (from x in ConfigObject.Lightings where x.Description.Equals(desc) && x.Floor.Equals(floor) select x).FirstOrDefault();

        public IEnumerable<DeviceItem> GetAllDevicesOfFloor(string floor)
        {

            if (ConfigObject.Shutters != null)
            {
                var allShutters = (from x in ConfigObject.Shutters where x.Floor.Equals(floor) select x.Description).ToArray();
                if (allShutters.Any())
                    foreach (var shutter in allShutters)
                        yield return new DeviceItem(floor, shutter, DeviceType.Shutter);
            }

            if (ConfigObject.Lightings != null)
            {
                var allLightings = (from x in ConfigObject.Lightings where x.Floor.Equals(floor) select x.Description).ToArray();
                if (allLightings.Any())
                    foreach (var lighting in allLightings)
                        yield return new DeviceItem(floor, lighting, DeviceType.Lighting);
            }
        }

        private void MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            if (e.Topic.Equals("almue/config"))
            {
                var str = System.Text.Encoding.UTF8.GetString(e.Message);
                ConfigObject = JsonConvert.DeserializeObject<DevicesConfigObject>(str);
                ReceivedConfig?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SendCommand(string topic, string message)
        {
            if (_mqttClient.IsConnected)
            {
                _mqttClient.Publish(topic, System.Text.Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
            }
        }

        public void OpenAllShutters()
        {
            if (ConfigObject.Shutters.Any())
            {
                foreach (var shutter in ConfigObject.Shutters)
                {
                    var topic = $"almue/shutter/{shutter.Floor}/{shutter.Description}";
                    SendCommand(topic, "open");
                }
            }
        }

        public void CloseAllShutters()
        {
            if (ConfigObject.Shutters.Any())
            {
                foreach (var shutter in ConfigObject.Shutters)
                {
                    var topic = $"almue/shutter/{shutter.Floor}/{shutter.Description}";
                    SendCommand(topic, "close");
                }
            }
        }

        public void Disconnect()
        {
            _mqttClient.Disconnect();
            _mqttClient = null;
        }
    }
}
