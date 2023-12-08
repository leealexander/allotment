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
            try
            {
                //  We don't want to measure when water is being drawn as this could effect the reading
                if (_machineService.IsWaterOn)
                {
                    ctx.RunAgainIn(TimeSpan.FromSeconds(30));
                    return;
                }

                if (!_machineService.IsWaterLevelSensorOn)
                {
                    await _machineService.WaterLevelMonitorOnAsync();
                }
            }
            catch (Exception ex)
            {
                await _auditLogger.AuditLogAsync($"Exception in water logger: {ex.Message}");
            }

            var sleepDuration = (await _settingsStore.GetAsync()).Irrigation.WaterLevelSensor.PeriodicCheckDuration;
            ctx.RunAgainIn(sleepDuration);
        }
    }
}
