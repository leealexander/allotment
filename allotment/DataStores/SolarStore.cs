using Allotment.Machine.Monitoring.Models;
using Allotment.Utils;
using System.Text;

namespace Allotment.DataStores
{
    public interface ISolarStore
    {
        Task<SolarReadingModel?> GetCurrentReadingAsync();
        Task<SolarStore.SolarHourReading[]> GetReadingsByHourAsync();
        Task StoreReadingAsync(SolarReadingModel details);
    }

    public class SolarStore : DataStore, ISolarStore
    {
        private SolarReadingModel ?_lastRead = null;
        private readonly IFileSystem _fileSystem;

        public SolarStore(IFileSystem fileSystem) : base("solar/$date.csv", fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task StoreReadingAsync(SolarReadingModel details)
        {
            await _fileSystem.AppendAllTextAsync(GetFilename(), ToCsv(details));
        }

        public record SolarHourReading
        {
            public ElectricalVariables SolarPanel { get; } = new();
            public ElectricalVariables Load { get; } = new();
            public Battery Battery { get; } = new();
        }


        public async Task<SolarHourReading[]> GetReadingsByHourAsync()
        {
            var readings = await GetReadingsAsync();

            var dayReadings = new SolarHourReading[24];
            SolarReadingModel? lastReading = null;
            int hourIndex = -1;
            void AvgStats(int count)
            {
                if (hourIndex >= 0)
                {
                    var r = dayReadings[hourIndex];
                    r.Load.Avg(count);
                    r.SolarPanel.Avg(count);
                    r.Battery.Avg(count);
                }
            }

            int hourSampleCount = 0;
            foreach (var r in readings)
            {
                var hour = r.DateTakenUtc.ToLocalTime().Hour;
                if (lastReading == null || hour != lastReading.DateTakenUtc.ToLocalTime().Hour)
                {
                    AvgStats(hourSampleCount);
                    hourSampleCount = 0;
                    hourIndex = hour;
                    dayReadings[hourIndex] = new();
                }
                var dr = dayReadings[hourIndex];
                dr.Load.Add(r.Load);
                dr.SolarPanel.Add(r.SolarPanel);
                dr.Battery.Add(r.Battery);

                hourSampleCount++;
                lastReading = r;
            }
            AvgStats(hourSampleCount);

            return dayReadings.ToArray();
        }

        public async Task<SolarReadingModel?> GetCurrentReadingAsync()
        {
            if (_lastRead == null)
            {
                var readings = await GetReadingsAsync();
                _lastRead = readings.Any() ? readings[^1] : null;
            }

            return _lastRead;
        }

        private async Task<List<SolarReadingModel>> GetReadingsAsync()
        {
            var readings = new List<SolarReadingModel>();
            var fileName = GetFilename();
            if (_fileSystem.Exists(fileName))
            {
                var end = 16;
                readings.AddRange(from fl in await _fileSystem.ReadAllLinesAsync(fileName)
                                  let split = fl.Split(',')
                                  where split.Length == end + 1
                                  select new SolarReadingModel
                                  {
                                      DateTakenUtc = DateTime.Parse(split[0]).ToUniversalTime(),
                                      DeviceStatus = new DeviceStatus
                                      {
                                          Temperature = double.Parse(split[1]),
                                          Charge = StringStatusValue.Parse(split[2]),
                                          Battery = StringStatusValue.Parse(split[3]),
                                          Load = StringStatusValue.Parse(split[4]),
                                          Controller = StringStatusValue.Parse(split[5]),
                                          SolarPanel = StringStatusValue.Parse(split[6]),
                                      },
                                      SolarPanel = new ElectricalVariables
                                      {
                                          Voltage = double.Parse(split[7]),
                                          Current = double.Parse(split[8]),
                                          Watts = double.Parse(split[9]),
                                      },
                                      Load = new ElectricalVariables
                                      {
                                          Voltage = double.Parse(split[10]),
                                          Current = double.Parse(split[11]),
                                          Watts = double.Parse(split[12]),
                                      },
                                      Battery = new Battery
                                      {
                                          Temperature = double.Parse(split[13]),
                                          StateOfCharge = ushort.Parse(split[14]),
                                          Current = double.Parse(split[15]),
                                          Voltage = double.Parse(split[end]),
                                      }
                                  });
            }

            return readings;
        }
        public static string ToCsv(SolarReadingModel model)
        {
            var sb = new StringBuilder();
            sb.Append($"{model.DateTakenUtc:o}");
            sb.Append($",{model.DeviceStatus.Temperature}");
            sb.Append($",{model.DeviceStatus.Charge}");
            sb.Append($",{model.DeviceStatus.Battery}");
            sb.Append($",{model.DeviceStatus.Load}");
            sb.Append($",{model.DeviceStatus.Controller}");
            sb.Append($",{model.DeviceStatus.SolarPanel}");

            sb.Append($",{model.SolarPanel.Voltage}");
            sb.Append($",{model.SolarPanel.Current}");
            sb.Append($",{model.SolarPanel.Watts}");

            sb.Append($",{model.Load.Voltage}");
            sb.Append($",{model.Load.Current}");
            sb.Append($",{model.Load.Watts}");

            sb.Append($",{model.Battery.Temperature}");
            sb.Append($",{model.Battery.StateOfCharge}");
            sb.Append($",{model.Battery.Current}");
            sb.Append($",{model.Battery.Voltage}");
            sb.AppendLine(); 

            return sb.ToString();
        }
    }

    public static class Extensions
    {
        public static void Add(this ElectricalVariables ev, ElectricalVariables toAdd)
        {
            ev.Current += toAdd.Current;
            ev.Voltage += toAdd.Voltage;
            ev.Watts += toAdd.Watts;
        }

        public static void Avg(this ElectricalVariables ev, int count)
        {
            ev.Current /= count;
            ev.Voltage /= count;
            ev.Watts /= count;
        }
        public static void Add(this Battery ev, Battery toAdd)
        {
            ev.Current += toAdd.Current;
            ev.Voltage += toAdd.Voltage;
            ev.Temperature += toAdd.Temperature;
            ev.StateOfCharge += toAdd.StateOfCharge;
        }

        public static void Avg(this Battery ev, int count)
        {
            ev.Current /= count;
            ev.Voltage /= count;
            ev.Temperature /= count;
            ev.StateOfCharge /= count;
        }
    }
}
