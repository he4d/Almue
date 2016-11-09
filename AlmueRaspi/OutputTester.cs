using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AlmueRaspi.Configuration;
using Raspberry.IO.GeneralPurpose;

namespace AlmueRaspi
{
    public class OutputTester : IDisposable
    {
        private readonly ConfigurationController _configController;
        private IEnumerable<OutputPinConfiguration> _allOutputPins;
        private GpioConnection _gpioConnection;

        public OutputTester(string fullConfigPath)
        {
            _configController = new ConfigurationController(fullConfigPath);
            InitializeGpio();
        }

        private void InitializeGpio()
        {
            var allShutterPins = (from x in _configController.Config.Devices.Shutters
                                  let onPin = x.OpenPin
                                  let offPin = x.ClosePin
                                  select new[] { onPin, offPin }).SelectMany(x => x).ToList();
            var allLightingPins = (from x in _configController.Config.Devices.Lightings
                                   select x.SwitchPin);
            var allPins = allShutterPins.Concat(allLightingPins);
            _allOutputPins = allPins.Select(pin => pin.Output().Enable());
            _gpioConnection  = new GpioConnection(_allOutputPins);
        }

        public void RunTest()
        {
            Console.WriteLine("Test-Start");
            //Der reihe nach jeden Pin 2Sec Low dann 2 Sec High
            foreach (var pin in _allOutputPins)
            {
                _gpioConnection[pin] = false;
                Thread.Sleep(2000);
                _gpioConnection[pin] = true;
                Thread.Sleep(2000);
            }

            //Alle Pins auf Low setzen 5 Sec. warten
            foreach (var pin in _allOutputPins)
                _gpioConnection[pin] = false;

            Thread.Sleep(5000);

            foreach (var pin in _allOutputPins)
                _gpioConnection[pin] = true;

            //Der reihe nach jeden Pin 250ms Low dann 250ms High - Vorwärts
            foreach (var pin in _allOutputPins)
            {
                _gpioConnection[pin] = false;
                Thread.Sleep(250);
                _gpioConnection[pin] = true;
                Thread.Sleep(250);
            }

            //Der reihe nach jeden Pin 250ms Low dann 250ms High - Rückwärts
            foreach (var pin in _allOutputPins.Reverse())
            {
                _gpioConnection[pin] = false;
                Thread.Sleep(250);
                _gpioConnection[pin] = true;
                Thread.Sleep(250);
            }

            Console.WriteLine("Test-End");
        }

        public void Dispose()
        {
            ((IDisposable)_gpioConnection).Dispose();
        }
    }
}
