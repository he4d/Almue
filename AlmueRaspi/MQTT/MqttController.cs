using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlmueRaspi.Interfaces;
using NLog;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace AlmueRaspi.MQTT
{
    public class MqttController
    {
        #region private fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IConfigurationController _configController;

        private static readonly string ClientId = Guid.NewGuid().ToString();

        private MqttClient _mqttClient;

        private readonly Dictionary<string, List<string>> _allDevicesAndTopics;

        #endregion

        #region constructor

        public MqttController(IConfigurationController configController)
        {
            _configController = configController;
            _allDevicesAndTopics = MqttTopicGenerator.GenerateDeviceWithTopicsDictionary(_configController.Config.Devices);
            InitializeMqttClient();
            if (!_mqttClient.IsConnected)
            {
                Logger.Error($"Could not connect to { _configController.Config.Broker.Ip } as { _configController.Config.Broker.User}");
                Environment.Exit(-1);
            }
            else
            {
                Logger.Info($"Successfully connected to { _configController.Config.Broker.Ip } as { _configController.Config.Broker.User}");
            }
            SubscribeToTopics();
            SendConfigToRetainedTopic();
            _configController.OnConfigChange += OnConfigChanges;
        }

        #endregion

        #region public events

        public event EventHandler<MqttDeviceEventArgs> ReceivedDeviceMessage;

        #endregion

        #region private methods

        private void OnConfigChanges(object sender, EventArgs eventArgs)
        {
            SendConfigToRetainedTopic();
        }

        private void SendConfigToRetainedTopic()
        {
            Task.Run(() =>
            {
                _mqttClient.Publish(MqttTopicGenerator.ConfigTopic, _configController.JsonConfigurationByteArray,
                    MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
            });
        }

        private void SubscribeToTopics()
        {
            foreach (var device in _allDevicesAndTopics)
                foreach (var topic in device.Value)
                    _mqttClient.Subscribe(new[] { topic }, new[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        }

        private void InitializeMqttClient()
        {
            _mqttClient = new MqttClient(_configController.Config.Broker.Ip);
            _mqttClient.MqttMsgPublishReceived += MqttMsgPublishReceived;
            _mqttClient.Connect(ClientId, _configController.Config.Broker.User, _configController.Config.Broker.Password);
        }

        private void MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            var deviceDescription = _allDevicesAndTopics.FirstOrDefault(x => x.Value.Contains(e.Topic)).Key;
            MqttDeviceEventArgs mqttDeviceEventArgs;
            if (MqttDeviceEventArgs.TryParse(e.Topic, e.Message, deviceDescription, out mqttDeviceEventArgs))
                OnReceivedMqttPublish(mqttDeviceEventArgs);
            else
                Logger.Error($"Unknown topic and message received:\nTopic: {e.Topic}\nMessage: {System.Text.Encoding.UTF8.GetString(e.Message)}");
        }

        #endregion

        #region rewritable methods

        protected virtual void OnReceivedMqttPublish(MqttDeviceEventArgs mqttDeviceEventArgs)
        {
            ReceivedDeviceMessage?.Invoke(this, mqttDeviceEventArgs);
        }

        #endregion
    }
}
