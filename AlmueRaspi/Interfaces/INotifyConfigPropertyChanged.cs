using System;

namespace AlmueRaspi.Interfaces
{
    public class ConfigPropertyChangedEventArgs : EventArgs
    {
        public string ConfigPropertyName { get; }
        public ConfigPropertyChangedEventArgs(string configPropertyName)
        {
            ConfigPropertyName = configPropertyName;
        }
    }

    public interface INotifyConfigPropertyChanged
    {
        event EventHandler<ConfigPropertyChangedEventArgs> ConfigPropertyChanged;
    }
}
