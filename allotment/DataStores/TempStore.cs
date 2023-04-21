using Allotment.Machine.Models;
using Allotment.Utils;
using Iot.Device.DHTxx;
using UnitsNet.Units;

namespace Allotment.DataStores
{
    public interface ITempStore
    {
        public TempDetails? Current { get; }
        IEnumerable<TempDetails> ReadingsByHour { get; }
        Task StoreReadingAsync(TempDetails details);
    }

    public class TempStore : DataStore, ITempStore
    {
        private TempDetails ?_lastRead = null;
        public TempStore(IFileSystem fileSystem) : base("temp/$date.csv", fileSystem)
        {
        }


        public IEnumerable<TempDetails> ReadingsByHour
        {
            get
            {
                var readings = GetDayReadings();

                TempDetails[] dayReadings = new TempDetails[24];
                double totalTemp = 0;
                double totalHumidity = 0;
                int hourCount = 0;
                TempDetails? lastReading = null;
                foreach (var r in readings)
                {
                    var hour = r.TimeTakenUtc.ToLocalTime().Hour;
                    if (lastReading == null || hour == lastReading.TimeTakenUtc.ToLocalTime().Hour)
                    {
                        hourCount++;
                    }
                    else
                    {
                        dayReadings[lastReading.TimeTakenUtc.ToLocalTime().Hour] = new TempDetails
                        {
                            TimeTakenUtc = r.TimeTakenUtc,
                            Temperature = new UnitsNet.Temperature(totalTemp / hourCount, r.Temperature.Unit),
                            Humidity = new UnitsNet.RelativeHumidity(totalHumidity / hourCount, r.Humidity.Unit),
                        };
                        totalTemp = totalHumidity = 0f;
                        hourCount = 1;
                    }
                    totalTemp += r.Temperature.Value;
                    totalHumidity += r.Humidity.Value;
                    lastReading = r;
                }

                if (lastReading != null)
                {
                    dayReadings[lastReading.TimeTakenUtc.ToLocalTime().Hour] = lastReading; // no need 
                }

                return dayReadings;
            }
        }

        public TempDetails? Current
        {
            get
            {
                if(_lastRead == null)
                {
                    var readings = GetDayReadings();
                    _lastRead = readings.Any() ? readings[^1] : null;
                }

                return _lastRead;
            }
        }


        private List<TempDetails> GetDayReadings()
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
            _lastRead = details;
        }
    }
}
