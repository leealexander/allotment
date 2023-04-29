using Allotment.DataStores;
using Allotment.Jobs;

namespace Allotment.Machine.Monitoring
{
    public class WaterLevelMonitor : IJobService
    {
        private readonly IMachineControlService _machineService;
        private readonly ISettingsStore _settingsStore;

        public WaterLevelMonitor(IMachineControlService machineService, ISettingsStore settingsStore)
        {
            _machineService = machineService;
            _settingsStore = settingsStore;
        }


        public async Task RunAsync(IRunContext ctx)
        {
            //  We don't want to measure when water is being drawn as this could effect the reading
            if(_machineService.IsWaterOn)
            {
                ctx.RunAgainIn(TimeSpan.FromSeconds(30));
                return;
            }

            if (!_machineService.IsWaterLevelSensorOn)
            {
                await _machineService.WaterLevelMonitorOnAsync();
            }

            ctx.RunAgainIn((await _settingsStore.GetAsync()).Irrigation.WaterLevelSensor.PeriodicCheckDuration);
        }
    }
}
