using Allotment.DataStores;
using Allotment.Machine.Monitoring.Models;
using Allotment.Machine.SolarAccessors;
using NModbus;
using NModbus.Serial;
using System.IO.Ports;

namespace Allotment.Machine
{
    public interface ISolarReader
    {
        Task<SolarReadingModel?> TakeReadingAsync();
    }

    public class SolarReader : ISolarReader
    {
        private readonly ILogger<SolarReader> _logger;
        private readonly ISettingsStore _settings;

        public SolarReader(ILogger<SolarReader> logger, ISettingsStore settings)
        {
            _logger = logger;
            _settings = settings;
        }
        public async Task<SolarReadingModel?> TakeReadingAsync()
        {
            var settings = await _settings.GetAsync();
            try
            {
                using var serialPort = new SerialPort(settings.SolarChargerSettingsModel.SerialAddress, settings.SolarChargerSettingsModel.BaudRate);
                serialPort.DataBits = 8;
                serialPort.StopBits = StopBits.One;
                serialPort.Parity = Parity.None;
                serialPort.Open();

                var factory = new ModbusFactory();

                var master = factory.CreateRtuMaster(serialPort);

                byte slaveId = 1;
                var reading = new SolarReadingModel();
                await StatusAccesssor.FillAsync(slaveId, master, reading.DeviceStatus);
                await PowerAndTempAccessor.FillAsync(slaveId, master, reading);
                return reading;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to read solar charger on port {settings.SolarChargerSettingsModel.SerialAddress}, reason: {ex.Message}");
            }

            return null;
        }
    }
}
