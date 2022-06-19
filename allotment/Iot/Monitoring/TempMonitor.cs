using Allotment.Iot;
using Allotment.Jobs;

namespace Allotmen.Iot.Monitoring
{
    public interface ITempMonitor
    {
        TempDetails? Current { get; }
        TempDetails[] Readings { get; }
    }

    public class TempMonitor : IJobService, ITempMonitor
    {
        private readonly IIotFunctions _iotFunctions;
        private readonly List<TempDetails> _readings = new();
        private readonly ILogger<TempMonitor> _logger;

        public TempMonitor(ILogger<TempMonitor> logger, IIotFunctions iotFunctions)
        {
            _logger = logger;
            _iotFunctions = iotFunctions;
        }

        public TempDetails[] Readings
        {
            get
            {
                lock (_readings)
                {
                    return _readings.ToArray();
                }
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
                var readTemp = await _iotFunctions.TryGetTempDetailsAsync(x =>
                {
                    lock (_readings)
                    {
                        _readings.Add(x);
                        if (_readings.Count > 1440)
                        {
                            _readings.RemoveAt(0);
                        }
                    }
                });
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
