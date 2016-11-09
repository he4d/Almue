using AlmueRaspi.Configuration;
using AlmueRaspi.Devices;
using AlmueRaspi.Interfaces;
using AlmueRaspi.MQTT;
using AlmueRaspi.Scheduler;
using Raspberry.IO.GeneralPurpose;
using System;
using System.Collections.Generic;
using System.Linq;
using AlmueRaspi.Scheduler.Jobs;
using NLog;

namespace AlmueRaspi
{
    public sealed class DeviceController :
        IDisposable
    {
        #region private fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly GpioConnection _gpioConnection;

        private readonly IDeviceScheduler _deviceScheduler;

        #endregion

        #region public properties

        public List<IDevice> AllDevices { get; } = new List<IDevice>();

        #endregion

        #region constructor

        public DeviceController(IConfigurationController configController, IDeviceScheduler scheduler = null)
        {
            _gpioConnection = new GpioConnection();
            if (_gpioConnection.IsOpened)
                InitializeDevices(configController.Config.Devices);
            if (scheduler != null)
            {
                _deviceScheduler = scheduler;
                RegisterDevicesToScheduler();
            }

        }

        #endregion

        #region public methods

        public void DeviceMessageIncoming(object sender, MqttDeviceEventArgs e)
        {
            Logger.Info($"{e.DeviceDescription} - {e.DeviceAction} - {e.DeviceType} - {e.Message}");
            ControlDeviceByMqttMessage(e.DeviceDescription, e.DeviceType, e.DeviceAction, e.Message);
        }

        #endregion

        #region private methods

        private void InitializeDevices(ConfigurationObject.DevicesConfig devicesConfig)
        {
            devicesConfig.Shutters?.ToList().ForEach(x =>
            {
                AllDevices.Add(new Shutter(x, _gpioConnection));
            });
            devicesConfig.Lightings?.ToList().ForEach(x =>
            {
                AllDevices.Add(new Lighting(x, _gpioConnection));
            });
            devicesConfig.WindMonitors?.ToList().ForEach(x =>
            {
                AllDevices.Add(new WindMonitor(x, _gpioConnection));
            });

            RegisterAllEmergencyDevices();
        }

        private void RegisterAllEmergencyDevices()
        {
            var allEmergencyDevices = AllDevices.OfType<IEmergencyReceiver>().ToArray();
            var allEmergencyNotifiers = AllDevices.OfType<INotifyEmergencyReceivers>();

            foreach (var emergencyNotifier in allEmergencyNotifiers)
                foreach (var emergencyDevice in allEmergencyDevices)
                {
                    if (emergencyDevice.EmergencyEnabled)
                        emergencyNotifier.NotifyEmergencyEvent += emergencyDevice.EmergencyEventIncoming;
                }
        }

        private void RegisterEmergencyDevice(IEmergencyReceiver device)
        {
            var allEmergencyNotifiers = AllDevices.OfType<INotifyEmergencyReceivers>();

            foreach (var emergencyNotifier in allEmergencyNotifiers)
            {
                emergencyNotifier.NotifyEmergencyEvent -= device.EmergencyEventIncoming;
                emergencyNotifier.NotifyEmergencyEvent += device.EmergencyEventIncoming;
            }
        }

        private void UnregisterEmergencyDevice(IEmergencyReceiver device)
        {
            var allEmergencyNotifiers = AllDevices.OfType<INotifyEmergencyReceivers>();

            foreach (var emergencyNotifier in allEmergencyNotifiers)
            {
                emergencyNotifier.NotifyEmergencyEvent -= device.EmergencyEventIncoming;
            }
        }

        private void RegisterDevicesToScheduler()
        {
            var allTimeableDevices = AllDevices.OfType<ISchedulable>();

            foreach (var dev in allTimeableDevices)
            {
                if (dev.TimerEnabled)
                    _deviceScheduler.EnableJobsOfDevice(dev);
            }
        }

        private void ControlDeviceByMqttMessage(string deviceDescription, Type deviceType, MqttDeviceActions deviceAction, string message)
        {
            var device = (from x in AllDevices
                          where x.Description.Equals(deviceDescription) && x.GetType() == deviceType
                          select x).FirstOrDefault();

            if (device == null)
            {
                Logger.Fatal($"{deviceDescription} of type {deviceType.Name} could not be found in the configured device-collection");
                return;
            }

            switch (deviceAction)
            {
                case MqttDeviceActions.None:
                    break;
                case MqttDeviceActions.Open:
                    if (typeof(IShuttable).IsAssignableFrom(deviceType))
                    {
                        var dev = device as IShuttable;
                        dev?.Open();
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(IShuttable)}");
                    }
                    break;
                case MqttDeviceActions.Close:
                    if (typeof(IShuttable).IsAssignableFrom(deviceType))
                    {
                        var dev = device as IShuttable;
                        dev?.Close();
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(IShuttable)}");
                    }
                    break;
                case MqttDeviceActions.Stop:
                    if (typeof(IStoppable).IsAssignableFrom(deviceType))
                    {
                        var dev = device as IStoppable;
                        dev?.Stop();
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(IStoppable)}");
                    }
                    break;
                case MqttDeviceActions.EnableDevice:
                    if (typeof(ICanBeDisabled).IsAssignableFrom(deviceType))
                    {
                        var dev = device as ICanBeDisabled;
                        if (dev != null)
                        {
                            dev.Disabled = false;
                            if (typeof(ISchedulable).IsAssignableFrom(deviceType))
                            {
                                var devSchedulable = dev as ISchedulable;
                                if (devSchedulable != null && devSchedulable.TimerEnabled)
                                    _deviceScheduler.EnableJobsOfDevice(devSchedulable);
                            }
                        }
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(ICanBeDisabled)}");
                    }
                    break;
                case MqttDeviceActions.DisableDevice:
                    if (typeof(ICanBeDisabled).IsAssignableFrom(deviceType))
                    {
                        var dev = device as ICanBeDisabled;
                        if (dev != null)
                        {
                            dev.Disabled = true;
                            if (typeof(ISchedulable).IsAssignableFrom(deviceType))
                            {
                                var devSchedulable = dev as ISchedulable;
                                if (devSchedulable != null && devSchedulable.TimerEnabled)
                                    _deviceScheduler.DisableJobsOfDevice(devSchedulable);
                            }
                        }
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(ICanBeDisabled)}");
                    }
                    break;
                case MqttDeviceActions.EnableEmergency:
                    if (typeof(IEmergencyReceiver).IsAssignableFrom(deviceType))
                    {
                        var dev = device as IEmergencyReceiver;
                        if (dev != null)
                        {
                            dev.EmergencyEnabled = true;
                            RegisterEmergencyDevice(dev);
                        }
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(IEmergencyReceiver)}");
                    }
                    break;
                case MqttDeviceActions.DisableEmergency:
                    if (typeof(IEmergencyReceiver).IsAssignableFrom(deviceType))
                    {
                        var dev = device as IEmergencyReceiver;
                        if (dev != null)
                        {
                            dev.EmergencyEnabled = false;
                            UnregisterEmergencyDevice(dev);
                        }
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(IEmergencyReceiver)}");
                    }
                    break;
                case MqttDeviceActions.EnableTimer:
                    if (typeof(ISchedulable).IsAssignableFrom(deviceType))
                    {
                        var dev = device as ISchedulable;
                        if (dev != null)
                        {
                            dev.TimerEnabled = true;
                            _deviceScheduler.EnableJobsOfDevice(dev);
                        }
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(ISchedulable)}");
                    }
                    break;
                case MqttDeviceActions.DisableTimer:
                    if (typeof(ISchedulable).IsAssignableFrom(deviceType))
                    {
                        var dev = device as ISchedulable;
                        if (dev != null)
                        {
                            dev.TimerEnabled = false;
                            _deviceScheduler.DisableJobsOfDevice(dev);
                        }
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(ISchedulable)}");
                    }
                    break;
                case MqttDeviceActions.SetOnTime:
                    if (typeof(ISchedulable).IsAssignableFrom(deviceType))
                    {
                        var dev = device as ISchedulable;
                        TimeSpan res;
                        if (TimeSpan.TryParse(message, out res))
                        {
                            if (dev != null)
                            {
                                dev.OnTime = res;
                                if (dev is ISwitchable)
                                    _deviceScheduler.UpdateTrigger(dev, JobActions.On);
                                else if (dev is IShuttable)
                                    _deviceScheduler.UpdateTrigger(dev, JobActions.Open);
                                else
                                    Logger.Fatal($"Jobaction not supported for the device {dev.GetType().Name}");
                            }
                        }
                        else
                        {
                            Logger.Error($"Time: {message} could not be parsed into a {nameof(TimeSpan)} ");
                        }
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(ISchedulable)}");
                    }
                    break;
                case MqttDeviceActions.SetOffTime:
                    if (typeof(ISchedulable).IsAssignableFrom(deviceType))
                    {
                        var dev = device as ISchedulable;
                        TimeSpan res;
                        if (TimeSpan.TryParse(message, out res))
                        {
                            if (dev != null)
                            {
                                dev.OffTime = res;
                                if (dev is ISwitchable)
                                    _deviceScheduler.UpdateTrigger(dev, JobActions.Off);
                                else if (dev is IShuttable)
                                    _deviceScheduler.UpdateTrigger(dev, JobActions.Close);
                                else
                                    Logger.Fatal($"Jobaction not supported for the device {dev.GetType().Name}");
                            }
                        }
                        else
                        {
                            Logger.Error($"Time: {message} could not be parsed into a {nameof(TimeSpan)} ");
                        }
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(ISchedulable)}");
                    }
                    break;
                case MqttDeviceActions.On:
                    if (typeof(ISwitchable).IsAssignableFrom(deviceType))
                    {
                        var dev = device as ISwitchable;
                        dev?.SwitchOn();
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(ISwitchable)}");
                    }
                    break;
                case MqttDeviceActions.Off:
                    if (typeof(ISwitchable).IsAssignableFrom(deviceType))
                    {
                        var dev = device as ISwitchable;
                        dev?.SwitchOff();
                    }
                    else
                    {
                        Logger.Error($"{deviceDescription} does not implement {nameof(ISwitchable)}");
                    }
                    break;
            }
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            ((IDisposable)_gpioConnection).Dispose();
        }

        #endregion

    }
}
