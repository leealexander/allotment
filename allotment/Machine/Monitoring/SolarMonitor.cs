using Allotment.DataStores;
using Allotment.Jobs;

namespace Allotment.Machine.Monitoring
{
    public class SolarMonitor : IJobService
    {
        private readonly ISolarStore _solarStore;
        private readonly IMachine _machine;
        private readonly ILogger<SolarMonitor> _logger;

        public SolarMonitor(ISolarStore solarStore, IMachine machine, ILogger<SolarMonitor> logger)
        {
            _solarStore = solarStore;
            _machine = machine;
            _logger = logger;
        }

        public async Task RunAsync(IRunContext ctx)
        {
            try
            {
                var reading = await _machine.TakeSolarReadingAsync();
                if (reading != null)
                {
                    await _solarStore.StoreReadingAsync(reading);
                    ctx.RunAgainIn(TimeSpan.FromMinutes(1));
                    return;
                }
                _logger.LogWarning("Solar reading returned null, will retry in 10s");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Solar monitor failed");
            }
            ctx.RunAgainIn(TimeSpan.FromSeconds(10));
        }
    }
}
