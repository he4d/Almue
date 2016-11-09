using System;
using Raspberry.IO.GeneralPurpose;
using AlmueRaspi.Interfaces;
using AlmueRaspi.Configuration;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NLog;

namespace AlmueRaspi.Devices
{
    //TODO: Vollständig implementieren und testen!
    public class WindMonitor : GpioDevice,
        INotifyEmergencyReceivers,
        INotifyConfigPropertyChanged,
        ICanBeDisabled
    {
        #region private fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static int _pulseCounter;

        private readonly InputPinConfiguration _inputPinConfig;

        #endregion

        #region constructor

        public WindMonitor(ConfigurationObject.DevicesConfig.WindMonitorConfig config, GpioConnection gpioConnection) 
            : base(gpioConnection)
        {
            Description = config.Description;
            _inputPinConfig = config.InPin.Input();
            _inputPinConfig.Name = config.Description;
            _inputPinConfig.OnStatusChanged(CountPulses);
            Disabled = config.Disabled;
        }

        #endregion

        #region private methods

        private void CountPulses(bool status)
        {
            if (!status)
            {
                Logger.Warn("Event Detected from WindSensor < 10");
                _pulseCounter++;
                if (_pulseCounter > 10)
                {
                    Logger.Warn("Event Detected from WindSensor > 10");
                    OnSensorEvent();
                }
            }
        }

        #endregion

        #region GpioDevice members

        protected override IEnumerable<PinConfiguration> AllDevicePins
        {
            get
            {
                yield return _inputPinConfig;
            }
        }

        public override string DeviceTypeName => nameof(WindMonitor);

        public override string Description { get; }

        public override string Floor => string.Empty;

        #endregion

        #region ICanBeDisabled implementation

        private bool _disabled = true;
        public bool Disabled
        {
            get
            {
                return _disabled;
            }
            set
            {
                if (_disabled != value)
                {
                    if (!value)
                    {
                        foreach (var pin in AllDevicePins)
                        {
                            if (!GpioConnection.Contains(pin))
                                GpioConnection.Add(pin);
                        }
                        _disabled = false;
                    }
                    else
                    {
                        foreach (var pin in AllDevicePins)
                        {
                            if (GpioConnection.Contains(pin))
                                GpioConnection.Remove(pin);
                        }
                        _disabled = true;
                    }
                    OnConfigPropertyChanged();
                }
            }
        }

        #endregion

        #region INotifyEmergencyReceivers implementation

        public event EventHandler NotifyEmergencyEvent;

        #endregion

        #region INotifyConfigPropertyChanged implementation

        public event EventHandler<ConfigPropertyChangedEventArgs> ConfigPropertyChanged;

        #endregion

        #region rewritable methods

        protected virtual void OnConfigPropertyChanged([CallerMemberName] string propertyName = null)
        {
            ConfigPropertyChanged?.Invoke(this, new ConfigPropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnSensorEvent()
        {
            NotifyEmergencyEvent?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
