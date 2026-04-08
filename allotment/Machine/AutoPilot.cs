using Allotment.DataStores;
using Allotment.Jobs;
using Allotment.Services;

namespace Allotment.Machine
{
    public class AutoPilot : IJobService
    {
        private readonly ISettingsStore _settingsStore;
        private readonly ICurrentTempService _currentTempService;
        private readonly IMachine _machine;
        private readonly IAuditLogger<AutoPilot> _auditLogger;

        public AutoPilot(ISettingsStore settingsStore, ICurrentTempService currentTempService, IMachine machine, IAuditLogger<AutoPilot> auditLogger)
        {
            _settingsStore = settingsStore;
            _currentTempService = currentTempService;
            _machine = machine;
            _auditLogger = auditLogger;
        }

        public async Task RunAsync(IRunContext ctx)
        {
            var settings = (await _settingsStore.GetAsync()).Autopilot;
            if (settings.Enabled)
            {
                var temp = await _currentTempService.GetCurrentTempCelsiusAsync();
                _auditLogger.LogInformation($"Running AutoPilot temp={temp}");
                if (temp != null)
                {
                    if (temp < settings.CloseDoorsWhenTempBelow)
                    {
                        if (_machine.LastDoorCommand == null || _machine?.LastDoorCommand == LastDoorCommand.DoorsOpen)
                        {
                            await _auditLogger.AuditLogAsync($"Closing doors as temp {temp}c is below {settings.CloseDoorsWhenTempBelow}c");
                            await _machine.DoorsCloseAsync();
                        }
                    }

                    if (temp > settings.OpenDoorsWhenTempGreater)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference. weird warning
                        if (_machine.LastDoorCommand == null || _machine.LastDoorCommand == LastDoorCommand.DoorsClosed)
                        {
                            await _auditLogger.AuditLogAsync($"Opening doors as temp {temp}c is higher than {settings.OpenDoorsWhenTempGreater}c");
                            await _machine.DoorsOpenAsync();
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                    ctx.RunAgainIn(TimeSpan.FromMinutes(5));
                    return;
                }
            }

            ctx.RunAgainIn(TimeSpan.FromSeconds(10));
        }

    }
}
