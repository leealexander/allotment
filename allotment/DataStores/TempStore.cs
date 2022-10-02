using Allotment.Machine.Models;
using Iot.Device.DHTxx;
using UnitsNet.Units;

namespace Allotment.DataStores
{
    public interface ITempStore
    {
        List<TempDetails> GetDayReadings();
        Task StoreReadingAsync(TempDetails details);
    }

    public class TempStore : DataStore, ITempStore
    {
        public TempStore(): base("temp/dd-mm-yyyy.csv")
        {
        }

        public List<TempDetails> GetDayReadings()
        {
            var readings = new List<TempDetails>();
            var fileName = GetFilename();
            if (File.Exists(fileName))
            {
                readings.AddRange(from fl in File.ReadAllLines(fileName)
                                  let split = fl.Split(',')
                                  where split.Length == 3
                                  select new TempDetails
                                  {
                                      TimeTakenUtc = DateTime.Parse(split[0]).ToUniversalTime(),
                                      Temperature = new UnitsNet.Temperature(double.Parse(split[1]), TemperatureUnit.DegreeCelsius),
                                      Humidity = new UnitsNet.RelativeHumidity(double.Parse(split[2]), RelativeHumidityUnit.Percent)
                                  });
            }

            return readings;
        }

        public async Task StoreReadingAsync(TempDetails details)
        {
            await File.AppendAllLinesAsync(GetFilename(), new[] { $"{details.TimeTakenUtc:o},{details.Temperature.DegreesCelsius},{details.Humidity.Percent}" });
        }
    }
}
