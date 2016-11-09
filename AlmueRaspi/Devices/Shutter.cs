using AlmueRaspi.Configuration;
using AlmueRaspi.Interfaces;
using Raspberry.IO.GeneralPurpose;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using NLog;

namespace AlmueRaspi.Devices
{
    public class Shutter : GpioDevice,
        IShutter,
        ISchedulable,
        IEmergencyReceiver,
        INotifyConfigPropertyChanged,
        IProvideDeviceStatus,
        ICanBeDisabled,
        IDisposable
    {
        #region private fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private Timer _timer;

        private readonly OutputPinConfiguration _openPin;

        private readonly OutputPinConfiguration _closePin;

        #endregion

        #region constructor

        public Shutter(ConfigurationObject.DevicesConfig.ShutterConfig config, GpioConnection gpioConnection) 
            : base(gpioConnection)
        {
            Description = config.Description;
            Floor = config.Floor;
            EmergencyEnabled = config.EmergencyEnabled;
            CompleteWayInSeconds = config.CompleteWayInSeconds;
            _timerEnabled = config.TimerEnabled;
            _onTime = config.OpenTime;
            _offTime = config.CloseTime;
            _openPin = config.OpenPin.Output().Disable();
            _closePin = config.ClosePin.Output().Disable();
            _openPin.Name = $"{config.Description}_openPin";
            _closePin.Name = $"{config.Description}_closePin";
            _deviceStatus = config.DeviceStatus;
            Disabled = config.Disabled;
        }

        #endregion

        #region rewritable methods

        protected virtual void OnConfigPropertyChanged([CallerMemberName] string propertyName = null)
        {
            ConfigPropertyChanged?.Invoke(this, new ConfigPropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region GpioDevice members

        protected override IEnumerable<PinConfiguration> AllDevicePins
        {
            get
            {
                yield return _closePin;
                yield return _openPin;
            }
        }

        public override string DeviceTypeName => nameof(Shutter);

        public override string Description { get; }

        public override string Floor { get; }

        #endregion

        #region private methods

        private void StartUpTimer()
        {
            _timer?.Dispose();
            _timer = new Timer(x => {
                Stop();
                DeviceStatus = DeviceStatus.Opened;
            }, null, TimeSpan.FromSeconds(CompleteWayInSeconds), Timeout.InfiniteTimeSpan);
        }

        private void StartDownTimer()
        {
            _timer?.Dispose();
            _timer = new Timer(x => {
                Stop();
                DeviceStatus = DeviceStatus.Closed;
            }, null, TimeSpan.FromSeconds(CompleteWayInSeconds), Timeout.InfiniteTimeSpan);
        }

        private void StopTimer()
        {
            _timer?.Dispose();
        }
        #endregion

        #region IShutter implementation

        public int CompleteWayInSeconds { get; }

        public void Open()
        {
            if (!Disabled && GpioConnection.IsOpened)
            {
                StartUpTimer();
                Logger.Info($"{Description} opens completely");
                DeviceStatus = DeviceStatus.Undefined;
                GpioConnection[_closePin] = false;
                GpioConnection[_openPin] = true;
            }
        }

        public void Close()
        {
            if (!Disabled && GpioConnection.IsOpened)
            {
                StartDownTimer();
                Logger.Info($"{Description} closes completely");
                DeviceStatus = DeviceStatus.Undefined;
                GpioConnection[_openPin] = false;
                GpioConnection[_closePin] = true;
            }
        }

        public void Stop()
        {
            if (!Disabled && GpioConnection.IsOpened)
            {
                StopTimer();
                Logger.Info($"{Description} stops");
                GpioConnection[_openPin] = false;
                GpioConnection[_closePin] = false;
            }
        }

        #endregion

        #region ISchedulable implementation

        public bool JobsCreated { get; set; }

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

        #region IEmergencyReceiver implementation

        public bool EmergencyEnabled { get; set; }

        public void EmergencyEventIncoming(object sender, EventArgs e)
        {
            Logger.Info($"EmergencyDevice: {sender.GetType().Name} opens {Description}");
            Open();
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

        #region IDisposable implementation

        public void Dispose()
        {
            _timer?.Dispose();
        }

        #endregion
    }
}
