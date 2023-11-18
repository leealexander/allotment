using Allotment.DataStores;
using Allotment.Jobs;

namespace Allotment.Machine.Monitoring
{
    public class WaterLevelMonitor : IJobService
    {
        private readonly IMachineControlService _machineService;
        private readonly ISettingsStore _settingsStore;
        private readonly IAuditLogger<WaterLevelMonitor> _auditLogger;

        public WaterLevelMonitor(IMachineControlService machineService, ISettingsStore settingsStore, IAuditLogger<WaterLevelMonitor> auditLogger)
        {
            _machineService = machineService;
            _settingsStore = settingsStore;
            _auditLogger = auditLogger;
        }


        public async Task RunAsync(IRunContext ctx)
        {
            //  We don't want to measure when water is being drawn as this could effect the reading
            await _auditLogger.AuditLogAsync("Checking water level...");
            if (_machineService.IsWaterOn)
            {
                await _auditLogger.AuditLogAsync("Water is already on so waiting 30secs...");
                ctx.RunAgainIn(TimeSpan.FromSeconds(30));
                return;
            }

            if (!_machineService.IsWaterLevelSensorOn)
            {
                await _auditLogger.AuditLogAsync("Turning on water monitor..");
                await _machineService.WaterLevelMonitorOnAsync();
            }
            else
            {
                await _auditLogger.AuditLogAsync("Water monitor already on, doing nothing!");
            }

            var sleepDuration = (await _settingsStore.GetAsync()).Irrigation.WaterLevelSensor.PeriodicCheckDuration;
            await _auditLogger.AuditLogAsync($"Sleeping for {sleepDuration}");
            ctx.RunAgainIn(sleepDuration);
        }
    }
}
