using Allotment.Iot;
using Allotment.Iot.Machine;
using Allotment.Jobs;

namespace Allotmen.Iot.Monitoring
{
    public interface ITempMonitor
    {
        TempDetails? Current { get; }
        IEnumerable<TempDetails> ReadingsByHour { get; }
    }

    public class TempMonitor : IJobService, ITempMonitor
    {
        private readonly IIotMachine _iotFunctions;
        private readonly List<TempDetails> _readings = new();
        private readonly ILogger<TempMonitor> _logger;

        public TempMonitor(ILogger<TempMonitor> logger, IIotMachine iotFunctions)
        {
            _logger = logger;
            _iotFunctions = iotFunctions;
            _readings = _iotFunctions.GetDayReadings();
        }


        public IEnumerable<TempDetails> ReadingsByHour
        {
            get
            {
                TempDetails []readings;
                lock (_readings)
                {
                    readings = _readings.ToArray();
                }

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
                lock(_readings)
                {
                    return _readings.Any() ? _readings[^1] : null;
                }
            }
        }


        public async Task RunAsync(IRunContext ctx)
        {
            try
            {
                TempDetails details = null;
                var readTemp = await _iotFunctions.TryGetTempDetailsAsync(x =>
                {
                    details = x;
                    lock (_readings)
                    {
                        if (_readings.Count > 0 && _readings[^1].TimeTakenUtc.ToLocalTime().Day != DateTime.Now.Day)
                        {
                            _readings.Clear();
                        }
                        _readings.Add(x);
                        if (_readings.Count > 1440)
                        {
                            _readings.RemoveAt(0);
                        }
                    }
                });
                if (readTemp && details is not null)
                {
                    await _iotFunctions.StoreReadingAsync(details);
                }
                ctx.RunAgainIn(readTemp ? TimeSpan.FromMinutes(1) : TimeSpan.FromSeconds(1));
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to read temp {0}", ex.Message);
            }
            ctx.RunAgainIn(TimeSpan.FromMinutes(1));
        }
    }
}
