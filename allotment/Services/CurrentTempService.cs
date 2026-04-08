using Allotment.DataStores;
using UnitsNet;
using UnitsNet.Units;

namespace Allotment.Services
{
    public interface ICurrentTempService
    {
        /// <summary>
        /// Returns the best available temperature in Celsius.
        /// Uses the stored DHT22 reading if fresh (within 10 minutes),
        /// otherwise falls back to the solar controller temperature probe.
        /// </summary>
        Task<int?> GetCurrentTempCelsiusAsync();
    }

    public class CurrentTempService : ICurrentTempService
    {
        private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(10);

        private readonly ITempStore _tempStore;
        private readonly ISolarStore _solarStore;

        public CurrentTempService(ITempStore tempStore, ISolarStore solarStore)
        {
            _tempStore = tempStore;
            _solarStore = solarStore;
        }

        public async Task<int?> GetCurrentTempCelsiusAsync()
        {
            var stored = _tempStore.Current;
            if (stored != null && (DateTime.UtcNow - stored.TimeTakenUtc) <= StaleThreshold)
            {
                return (int)stored.Temperature.DegreesCelsius;
            }

            var solarReading = await _solarStore.GetCurrentReadingAsync();
            if (solarReading != null && solarReading.DeviceStatus.Temperature != 0)
            {
                return (int)solarReading.DeviceStatus.Temperature;
            }

            return null;
        }
    }
}
