using Allotment.Jobs;

namespace Allotment.Iot
{
    public class IotStartup : IJobService
    {
        private readonly IIotMachine _iotFunctions;
        public IotStartup(IIotMachine iotFunctions)
        {
            _iotFunctions = iotFunctions;
        }

        public async Task RunAsync(IRunContext ctx)
        {
            await _iotFunctions.TurnAllOffAsync();
        }
    }
}
