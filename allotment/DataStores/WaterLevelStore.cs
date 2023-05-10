using Allotment.DataStores.Models;
using Allotment.Machine.Models;
using Allotment.Machine.Monitoring.Models;
using Allotment.Services;
using Allotment.Utils;
using UnitsNet.Units;

namespace Allotment.DataStores
{
    public interface IWaterLevelStore
    {
        Task ApplyKnownWaterLevelAsync(int levelCm);
        Task<ICollection<WaterLevelReadingModel>> GetReadingsAsync();
        Task StoreReadingAsync(WaterLevelReadingModel details);
    }

    public class WaterLevelStore : DataStore, IWaterLevelStore
    {
        private readonly ISettingsStore _settingsStore;
        private readonly IStateStore<WaterSensorStateModel> _stateModel;
        private readonly IAuditLogger<WaterLevelService> _auditLogger;

        public WaterLevelStore(ISettingsStore settingsStore, IStateStore<WaterSensorStateModel> stateModel, IFileSystem fileSystem, IAuditLogger<WaterLevelService> auditLogger) 
            : base("water-level/readings.csv", fileSystem)
        {
            _settingsStore = settingsStore;
            _stateModel = stateModel;
            _auditLogger = auditLogger;
        }

        public async Task<ICollection<WaterLevelReadingModel>> GetReadingsAsync()
        {
            var readings = new List<WaterLevelReadingModel>();
            var fileName = GetFilename();
            if (File.Exists(fileName))
            {
                readings.AddRange(from fl in await File.ReadAllLinesAsync(fileName)
                                  let split = fl.Split(',')
                                  where split.Length == 3
                                  select new WaterLevelReadingModel
                                  {
                                      DateTakenUtc = DateTime.Parse(split[0]).ToUniversalTime(),
                                      Reading = int.Parse(split[1]),
                                      KnownDepthCm = string.IsNullOrWhiteSpace(split[2]) ? null : int.Parse(split[2]),
                                  });
            }

            return readings;
        }

        public async Task ApplyKnownWaterLevelAsync(int levelCm)
        {
            var lastReading = (await GetReadingsAsync()).LastOrDefault();
            if(lastReading != null )
            {
                var settings = await _settingsStore.GetAsync();
                if(DateTime.UtcNow - lastReading.DateTakenUtc <= settings.Irrigation.WaterLevelSensor.PoweredOnDuration)
                {
                    lastReading.KnownDepthCm = levelCm;
                    await StoreReadingAsync(lastReading);
                }
            }
        }

        public async Task StoreReadingAsync(WaterLevelReadingModel details)
        {
            _auditLogger.LogInformation($"Storing waterlevel processed reading {details.Reading} at '{GetFilename()}'");
            var knownDepth = details.KnownDepthCm.HasValue ? details.KnownDepthCm.Value.ToString() : "";
            await File.AppendAllLinesAsync(GetFilename(), new[] { $"{details.DateTakenUtc:o},{details.Reading},{knownDepth}"});

            var state = await _stateModel.GetAsync();
            state.LastReading = details;
            await _stateModel.StoreAsync(state);
        }
    }
}
