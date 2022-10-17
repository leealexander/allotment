using Allotment.DataStores;
using Allotment.Jobs;

namespace Allotment.Machine.Monitoring
{
    public class WaterLevelMonitor : IJobService
    {
        private readonly IMachineControlService _machineService;
        private readonly ISettingsStore _settingsStore;
        private static bool _firstTime = true;

        public WaterLevelMonitor(IMachineControlService machineService, ISettingsStore settingsStore)
        {
            _machineService = machineService;
            _settingsStore = settingsStore;
        }


        public async Task RunAsync(IRunContext ctx)
        {
            if (!_firstTime && !_machineService.IsWaterLevelSensorOn)
            {
                await _machineService.WaterLevelMonitorOnAsync();
            }
            _firstTime = false;
            ctx.RunAgainIn((await _settingsStore.GetAsync()).Irrigation.WaterLevelSensor.PeriodicCheckDuration);
        }
    }
}
