using AlmueRaspi.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AlmueRaspi.MQTT;
using AlmueRaspi.Scheduler;
using NLog.Config;
using NLog.Targets;
using NLog;
using AlmueRaspi.Interfaces;

namespace AlmueRaspi
{
    internal static class MainApplication
    {
        #region private fields

        private const string ConfigFileName = "config.json";
        private static readonly string ApplicationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private static readonly string FullConfigPath = Path.Combine(ApplicationPath, ConfigFileName);
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region MAINAPPLICATION ENTRY POINT

        public static void Main(string[] args)
        {
            if (NativeMethods.getuid() != 0)
            {
                Console.WriteLine("Program must be started as root");
                return;
            }
            if (!File.Exists(FullConfigPath))
            {
                Console.WriteLine($"No configuration file found in {ApplicationPath}");
                Logger.Fatal($"No configuration file found in {ApplicationPath}");
                return;
            }

            if (args.Length != 0)
            {
                if (args[0].Equals("-outputtest"))
                {
                    var tester = new OutputTester(FullConfigPath);
                    tester.RunTest();
                    tester.Dispose();
                    return;
                }
                Console.WriteLine("Unknown Argument.. ");
                Console.WriteLine("Try without argument or with -outputtest for testing the configured GPIO Ports");
                return;
            }

            try
            {
                InitializeLogger();
                InitializeApplication();
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Error occured during initialization: {ex.Message}");
            }
        }

        #endregion

        #region private methods

        private static void InitializeLogger()
        {
            var config = new LoggingConfiguration();

            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            fileTarget.FileName = "/var/log/almue/almue.log";
            fileTarget.ArchiveEvery = FileArchivePeriod.Day;
            fileTarget.ArchiveFileName = "/var/log/almue/almue.{####}.log";
            fileTarget.ArchiveNumbering = ArchiveNumberingMode.Rolling;
            fileTarget.MaxArchiveFiles = 30;

            var rule = new LoggingRule("*", LogLevel.Info, fileTarget);
            config.LoggingRules.Add(rule);
            LogManager.Configuration = config;
        }

        private static void InitializeApplication()
        {

            var configController = new ConfigurationController(FullConfigPath);

            var scheduler = new DeviceScheduler();

            var deviceController = new DeviceController(configController, scheduler);

            var mqttController = new MqttController(configController);
            mqttController.ReceivedDeviceMessage += deviceController.DeviceMessageIncoming;

            if (deviceController.AllDevices.Any())
            {
                var allConfigNotifiers = deviceController.AllDevices.OfType<INotifyConfigPropertyChanged>();
                foreach (var notifier in allConfigNotifiers)
                    notifier.ConfigPropertyChanged += configController.ConfigEntryChanged;
            }
        }

        #endregion
    }
}
