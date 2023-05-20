using Allotment.DataStores;
using Allotment.Jobs;

namespace Allotment.Machine
{
    public class AutoPilot : IJobService
    {
        private readonly ISettingsStore _settingsStore;
        private readonly ITempStore _tempStore;
        private readonly ISolarStore _solarStore;
        private readonly IMachine _machine;
        private readonly IAuditLogger<AutoPilot> _auditLogger;

        public AutoPilot(ISettingsStore settingsStore, ITempStore tempStore, ISolarStore solarStore, IMachine machine, IAuditLogger<AutoPilot> auditLogger)
        {
            _settingsStore = settingsStore;
            _tempStore = tempStore;
            _solarStore = solarStore;
            _machine = machine;
            _auditLogger = auditLogger;
        }

        public async Task RunAsync(IRunContext ctx)
        {
            var settings = (await _settingsStore.GetAsync()).AutoPilot;
            if (!settings.Enabled)
            {
                return;
            }

            var temp = await GetTempAsync();
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
                    if (_machine.LastDoorCommand == null || _machine?.LastDoorCommand == LastDoorCommand.DoorsClosed)
                    {
                        await _auditLogger.AuditLogAsync($"Opening doors as temp {temp}c is higher than {settings.OpenDoorsWhenTempGreater}c");
                        await _machine.DoorsOpenAsync();
                    }
                }
                ctx.RunAgainIn(TimeSpan.FromMinutes(5));
            }
            else
            {
                ctx.RunAgainIn(TimeSpan.FromSeconds(10));
            }

        }

        private async Task<int?> GetTempAsync()
        {
            int? reading = null;
            var currentReading = _tempStore.Current;
            if (currentReading == null)
            {
                var solarReading = await _solarStore.GetCurrentReadingAsync();
                if(solarReading != null)
                {
                    reading = (int)solarReading.Battery.Temperature;
                }
            }
            else
            {
                reading = (int)currentReading.Temperature.Value;
            }

            return reading;
        }
    }
}
