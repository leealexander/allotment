using Allotment.Machine;
using Allotment.Machine.Models;
using Allotment.Jobs;
using Allotment.DataStores;
using UnitsNet;
using UnitsNet.Units;

namespace Allotment.Machine.Monitoring
{
    public class TempMonitor : IJobService
    {
        private readonly IMachine _machine;
        private readonly ITempStore _tempStore;
        private readonly ILogger<TempMonitor> _logger;

        public TempMonitor(ILogger<TempMonitor> logger, IMachine machine, ITempStore tempStore)
        {
            _logger = logger;
            _machine = machine;
            _tempStore = tempStore;
        }

        public async Task RunAsync(IRunContext ctx)
        {
            try
            {
                TempDetails? details = default;
                var readTemp = await _machine.TryGetTempDetailsAsync(x =>
                {
                    details = x;
                });

                if (!readTemp || details is null)
                {
                    details = await TryGetSolarTempFallbackAsync();
                }

                if (details is not null)
                {
                    await _tempStore.StoreReadingAsync(details);
                }
                else
                {
                    ctx.RunAgainIn(TimeSpan.FromSeconds(10));
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to read temp {0}", ex.Message);
            }
            ctx.RunAgainIn(TimeSpan.FromMinutes(1));
        }

        private async Task<TempDetails?> TryGetSolarTempFallbackAsync()
        {
            var solarReading = await _machine.TakeSolarReadingAsync();
            if (solarReading is null || solarReading.DeviceStatus.Temperature == 0)
            {
                return null;
            }

            _logger.LogWarning("Main temp sensor unavailable, falling back to solar probe temperature");
            return new TempDetails
            {
                TimeTakenUtc = DateTime.UtcNow,
                Temperature = new Temperature(solarReading.DeviceStatus.Temperature, TemperatureUnit.DegreeCelsius),
                Humidity = new RelativeHumidity(0, RelativeHumidityUnit.Percent),
            };
        }
    }
}
