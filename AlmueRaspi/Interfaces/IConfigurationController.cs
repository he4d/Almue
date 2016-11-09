using System;
using AlmueRaspi.Configuration;

namespace AlmueRaspi.Interfaces
{
    public interface IConfigurationController
    {
        byte[] JsonConfigurationByteArray { get; }

        ConfigurationObject Config { get; }

        event EventHandler OnConfigChange;
    }
}
