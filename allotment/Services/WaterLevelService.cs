using Allotment.DataStores;
using Allotment.DataStores.Models;
using Allotment.Machine.Monitoring.Models;

namespace Allotment.Services
{
    public interface IWaterLevelService
    {
        Task<int?> GetLevelAsync();
        Task<int?> GetPercentageFullAsync();

        Task ProcessReadingsBatchAsync(IEnumerable<WaterLevelReadingModel> batch);
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
                    return (int)(depth / maxDepth * 100d);
                }
            }

            return null;
        }

        public async Task<int?> GetLevelAsync()
        {
            var state = await GetStateAsync();
            var knownReadings = state.KnownReadings;
            var reading = state.LastReading;
            if(reading == null)
            {
                return null;
            }

            var position = knownReadings.BinarySearch(reading, WaterLevelReadingModel.ReadingsComparer);

            if (position >= 0)
            {
                return knownReadings[position].Reading;
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

        public async Task ProcessReadingsBatchAsync(IEnumerable<WaterLevelReadingModel> batch)
        {
            var settings = await _settingsStore.GetAsync();
            var batchReadings = batch.OrderBy(x => x.DateTakenUtc).ToArray();
            var minReadings = settings.Irrigation.WaterLevelSensor.MinReadingsPerSensonOnSession;
            if (batchReadings.Length < minReadings)
            {
                await _auditLogger.LogAsync($"Couldn't store a water level reading as Not enough readings were taken min {minReadings}: '{string.Join(", ", batchReadings.Select(x=>x.Reading))}");
                return;
            }

            var runs = new List<List<WaterLevelReadingModel>>();
            runs.Add(new());

            List<WaterLevelReadingModel> filteredReadings = new();
            for(int i = 1; i < batchReadings.Length; i++)
            {
                var currentRun = runs.Last();
                var left = batchReadings[i - 1];
                var right = batchReadings[i];
                if (Math.Abs(left.Reading - right.Reading) <= settings.Irrigation.WaterLevelSensor.MaxDevianceBetweenReadingsAllowed)
                {
                    if(currentRun.Last() != left)
                    {
                        currentRun.Add(left);
                    }
                    currentRun.Add(right);
                }
                else if (currentRun.Count > 0)
                {
                    runs.Add(new());
                }
            }

            var maxRunLength = runs.Max(x => x.Count);
            var qualifyingRuns = runs.Where(x => x.Count > 0 && x.Count == maxRunLength).ToArray();
            if(qualifyingRuns.Length == 1)
            {
                var run = qualifyingRuns[0];
                await _waterLevelStore.StoreReadingAsync(new WaterLevelReadingModel
                {
                    DateTakenUtc = DateTime.UtcNow,
                    Reading = (int)run.Average(x => x.Reading),
                }); 
            }
            else
            {
                await _auditLogger.LogAsync($"Couldn't store a water level reading as there were no qualifying runs of ~contiguous readings");
            }
        }
    }
}

