using Allotment.DataStores;
using Allotment.Jobs;

namespace Allotment.Machine.Monitoring
{


    public class SolarMonitor : IJobService
    {
        private readonly ISolarStore _solarStore;
        private readonly IMachine _machine;

        public SolarMonitor(ISolarStore solarStore, IMachine machine)
        {
            _solarStore = solarStore;
            _machine = machine;
        }

        public async Task RunAsync(IRunContext ctx)
        {
            var reading = await _machine.TakeSolarReadingAsync();
            if(reading != null)
            {
                await _solarStore.StoreReadingAsync(reading);
                ctx.RunAgainIn(TimeSpan.FromMinutes(1));
            }
            else
            {
                ctx.RunAgainIn(TimeSpan.FromSeconds(10));
            }
        }
    }
}
