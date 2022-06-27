using Allotment.Jobs;

namespace Allotment.Iot
{
    public class IotStartup : IJobService
    {
        private readonly IIotFunctions _iotFunctions;
        public IotStartup(IIotFunctions iotFunctions)
        {
            _iotFunctions = iotFunctions;
        }

        public async Task RunAsync(IRunContext ctx)
        {
            await _iotFunctions.TurnOffAllPinsAsync();
        }
    }
}
