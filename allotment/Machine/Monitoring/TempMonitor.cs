using Allotment.Machine;
using Allotment.Machine.Models;
using Allotment.Jobs;
using Allotment.DataStores;

namespace Allotment.Machine.Monitoring
{
    public class TempMonitor : IJobService
    {
        private readonly IMachine _machine;
        private readonly ITempStore _tempStore;
        private readonly ILogger<TempMonitor> _logger;

        public TempMonitor(ILogger<TempMonitor> logger, IMachine machine, ITempStore tempStore)
        {
            _logger = logger;
            _machine = machine;
            _tempStore = tempStore;
        }

        public async Task RunAsync(IRunContext ctx)
        {
            try
            {
                TempDetails ?details = default;
                var readTemp = await _machine.TryGetTempDetailsAsync(x =>
                {
                    details = x;
                });

                if (readTemp && details is not null)
                {
                    await _tempStore.StoreReadingAsync(details);
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
