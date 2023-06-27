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
                var after = DateTime.UtcNow.AddHours(-24);
                var allReadings = from fl in await File.ReadAllLinesAsync(fileName)
                               let split = fl.Split(',')
                               where split.Length >= 3
                               select new WaterLevelReadingModel
                               {
                                   DateTakenUtc = DateTime.Parse(split[0]).ToUniversalTime(),
                                   Reading = int.Parse(split[1]),
                                   KnownDepthCm = string.IsNullOrWhiteSpace(split[2]) ? null : int.Parse(split[2]),
                                   Annotation = split.Length > 3 ? split[3] : string.Empty,
                               };
                readings.AddRange(allReadings.Where(x=>x.DateTakenUtc > after));
            }

            return readings;
        }


        public async Task StoreReadingAsync(WaterLevelReadingModel details)
        {
            _auditLogger.LogInformation($"Storing waterlevel processed reading {details.Reading} at '{GetFilename()}'");
            var knownDepth = details.KnownDepthCm.HasValue ? details.KnownDepthCm.Value.ToString() : "";
            await File.AppendAllLinesAsync(GetFilename(), new[] { $"{details.DateTakenUtc:o},{details.Reading},{knownDepth},{details.Annotation}"});

            var state = await _stateModel.GetAsync();
            state.LastReading = details;
            if (details.KnownDepthCm.HasValue)
            {
                var reading = state.KnownReadings.SingleOrDefault(x=>x.KnownDepthCm == details.KnownDepthCm.Value);
                if(reading != null)
                {
                    state.KnownReadings.Remove(reading);
                }
                state.KnownReadings.Add(details);
            }
            await _stateModel.StoreAsync(state);
        }
    }
}
