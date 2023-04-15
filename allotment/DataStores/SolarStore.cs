using Allotment.DataStores.Models;
using Allotment.Machine.Monitoring.Models;
using System.Text;

namespace Allotment.DataStores
{
    public interface ISolarStore
    {
        Task<ICollection<SolarReadingModel>> GetReadingsAsync();
        Task StoreReadingAsync(SolarReadingModel details);
    }

    public class SolarStore : DataStore, ISolarStore
    {

        public SolarStore() : base("solar/$date.csv")
        {
        }

        public async Task StoreReadingAsync(SolarReadingModel details)
        {
            await File.AppendAllTextAsync(GetFilename(), ToCsv(details));
        }
        public async Task<ICollection<SolarReadingModel>> GetReadingsAsync()
        {
            var readings = new List<SolarReadingModel>();
            var fileName = GetFilename();
            if (File.Exists(fileName))
            {
                var end = 16;
                readings.AddRange(from fl in await File.ReadAllLinesAsync(fileName)
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
        private static string ToCsv(SolarReadingModel model)
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

            return sb.ToString();
        }
    }
}
