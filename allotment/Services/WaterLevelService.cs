using Allotment.DataStores;
using Allotment.DataStores.Models;
using Allotment.Machine.Monitoring.Models;
using Allotment.Utils;

namespace Allotment.Services
{
    public interface IWaterLevelService
    {
        Task<int?> GetLevelAsync();
        Task<int?> GetPercentageFullAsync();

        Task ProcessReadingsBatchAsync(IEnumerable<WaterLevelReadingModel> batch, int ?knownWaterHeightCm = null);
    }

    public class WaterLevelService : IWaterLevelService
    {
        private readonly IStateStore<WaterSensorStateModel> _waterState;
        private readonly ISettingsStore _settingsStore;
        private readonly IWaterLevelStore _waterLevelStore;
        private readonly IAuditLogger<WaterLevelService> _auditLogger;
        private WaterSensorStateModel ?_sensorState = null;

        public WaterLevelService(IStateStore<WaterSensorStateModel> waterState, ISettingsStore settingsStore, IWaterLevelStore waterLevelStore, IAuditLogger<WaterLevelService> auditLogger)
        {
            _waterState = waterState;
            _settingsStore = settingsStore;
            _waterLevelStore = waterLevelStore;
            _auditLogger = auditLogger;
        }

        public async Task<int?> GetPercentageFullAsync()
        {
            var levelCm = await GetLevelAsync();
            if (levelCm != null)
            {
                var settings = await _settingsStore.GetAsync();
                if (settings != null)
                {
                    var maxDepth = settings.Irrigation.WaterLevelSensor.WaterSourceMaxDepthCm;
                    var depth = Math.Min(levelCm.Value, maxDepth);
                    return (int)((double)depth / maxDepth * 100d);
                }
            }

            return null;
        }

        public async Task<int?> GetLevelAsync()
        {
            var state = await GetStateAsync();
            var knownReadings = state.KnownReadings.OrderBy(x=>x.Reading).ToList();
            if(knownReadings.Count == 0)
            {
                return null;
            }
            var reading = state.LastReading;
            if(reading == null)
            {
                return null;
            }

            var position = knownReadings.BinarySearch(reading, WaterLevelReadingModel.ReadingsComparer);

            if (position >= 0)
            {
                return knownReadings[position].KnownDepthCm;
            }
            position = ~position;

            int? depth;
            var readingDropPerCm = CalculateReadingDropPerCm(knownReadings);
            if (position < knownReadings.Count)
            {
                var calcDepth = knownReadings[position].KnownDepthCm - (int)((knownReadings[position].Reading - reading.Reading) / readingDropPerCm);
                if (calcDepth == null)
                {
                    throw new NullReferenceException();
                }
                depth = Math.Max(calcDepth.Value, 0);
            }
            else
            {
                position = knownReadings.Count - 1;
                var calcDepth = knownReadings[position].KnownDepthCm + (int)((reading.Reading - knownReadings[position].Reading) / readingDropPerCm);
                if (calcDepth == null)
                {
                    throw new NullReferenceException();
                }
                depth = calcDepth.Value;
            }

            return depth;
        }

        private double CalculateReadingDropPerCm(ICollection<WaterLevelReadingModel> knownReadings)
        {
            var dropsPerCm = new List<double>();
            WaterLevelReadingModel? last = null;
            foreach (var item in knownReadings.AsEnumerable().Reverse())
            {
                if (last != null && last.KnownDepthCm.HasValue && item.KnownDepthCm.HasValue)
                {
                    dropsPerCm.Add((last.Reading - item.Reading) / (last.KnownDepthCm.Value - item.KnownDepthCm.Value));
                }
                last = item;
            }

            return dropsPerCm.Count == 0 ? 1.6d : dropsPerCm.Sum() / dropsPerCm.Count; // 1.6 is from previous testing of sensor
        }


        private async Task<WaterSensorStateModel> GetStateAsync()
        {
            _sensorState ??= await _waterState.GetAsync();
            return _sensorState;
        }

        public async Task ProcessReadingsBatchAsync(IEnumerable<WaterLevelReadingModel> batch, int? knownWaterHeightCm = null)
        {
            _auditLogger.LogInformation($"Processing {batch.Count()} readings ");


            if(batch.Any())
            {
                await _waterLevelStore.StoreReadingAsync(new WaterLevelReadingModel
                {
                    KnownDepthCm = knownWaterHeightCm,
                    DateTakenUtc = DateTime.UtcNow,
                    Reading = (int)batch.Select(x=>x.Reading).Average(),
                }); 
            }
            else
            {
                await _auditLogger.AuditLogAsync($"No water pressure readings were available");
            }
        }
    }
}

