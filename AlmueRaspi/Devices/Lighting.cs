using AlmueRaspi.Interfaces;
using System;
using AlmueRaspi.Configuration;
using Raspberry.IO.GeneralPurpose;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using NLog;

namespace AlmueRaspi.Devices
{
    public class Lighting : GpioDevice,
        ISwitchable,
        ISchedulable,
        INotifyConfigPropertyChanged,
        IProvideDeviceStatus,
        ICanBeDisabled
    {
        #region private fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly OutputPinConfiguration _switchPin;

        #endregion

        #region constructor

        public Lighting(ConfigurationObject.DevicesConfig.LightingConfig config, GpioConnection gpioConnection) 
            : base(gpioConnection)
        {
            Description = config.Description;
            Floor = config.Floor;
            _timerEnabled = config.TimerEnabled;
            _offTime = config.OffTime;
            _onTime = config.OnTime;
            _switchPin = config.SwitchPin.Output().Disable();
            _switchPin.Name = config.Description;
            _deviceStatus = config.DeviceStatus;
            Disabled = config.Disabled;
        }

        #endregion

        #region GpioDevice members

        protected override IEnumerable<PinConfiguration> AllDevicePins
        {
            get { yield return _switchPin; }
        }

        public override string DeviceTypeName => nameof(Lighting);

        public override string Description { get; }

        public override string Floor { get; }

        #endregion

        #region rewritable methods

        protected virtual void OnConfigPropertyChanged([CallerMemberName] string propertyName = null)
        {
            ConfigPropertyChanged?.Invoke(this, new ConfigPropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region ILighting implementation

        public void SwitchOn()
        {
            if (!Disabled && GpioConnection.IsOpened)
            {
                GpioConnection[_switchPin.Name] = true;
                DeviceStatus = DeviceStatus.On;
                Logger.Info($"{Description} switched on");
            }
        }

        public void SwitchOff()
        {
            if (!Disabled && GpioConnection.IsOpened)
            {
                GpioConnection[_switchPin.Name] = false;
                DeviceStatus = DeviceStatus.Off;
                Logger.Info($"{Description} switched off");
            }
        }

        #endregion

        #region ISchedulable implementation

        private bool _timerEnabled;
        public bool TimerEnabled
        {
            get
            {
                return _timerEnabled;
            }
            set
            {
                if (_timerEnabled != value)
                {
                    _timerEnabled = value;
                    OnConfigPropertyChanged();
                }
            }
        }

        public bool JobsCreated { get; set; }

        private TimeSpan _offTime;

        public TimeSpan OffTime
        {
            get
            {
                return _offTime;
            }
            set
            {
                if (_offTime != value)
                {
                    _offTime = value;
                    OnConfigPropertyChanged();
                }
            }
        }

        private TimeSpan _onTime;

        public TimeSpan OnTime
        {
            get
            {
                return _onTime;
            }
            set
            {
                if (_onTime != value)
                {
                    _onTime = value;
                    OnConfigPropertyChanged();
                }
            }
        }

        #endregion

        #region INotifyConfigPropertyChanged implementation

        public event EventHandler<ConfigPropertyChangedEventArgs> ConfigPropertyChanged;

        #endregion

        #region IProvideDeviceStatus implementation

        DeviceStatus _deviceStatus;
        public DeviceStatus DeviceStatus
        {
            get
            {
                return _deviceStatus;
            }
            private set
            {
                if (_deviceStatus != value)
                {
                    _deviceStatus = value;
                    OnConfigPropertyChanged();
                }
            }
        }

        #endregion

        #region ICanBeDisabled implementation

        private bool _disabled = true;
        public bool Disabled
        {
            get { return _disabled; }
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
    }
}
