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

        public TempMonitor(IIotFunctions iotFunctions)
        {
            _iotFunctions = iotFunctions;
        }

        public async Task RunAsync(IRunContext ctx)
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
        }
    }
}
