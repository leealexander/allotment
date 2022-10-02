using Allotment.Jobs;

namespace Allotment.Machine
{
    public class MachineStartup : IJobService
    {
        private readonly IMachine _machine;
        private readonly IAuditLogger<MachineStartup> _auditLogger;

        public MachineStartup(IMachine machine, IAuditLogger<MachineStartup> auditLogger)
        {
            _machine = machine;
            _auditLogger = auditLogger;
        }

        public async Task RunAsync(IRunContext ctx)
        {
            await _auditLogger.LogAsync("Machine started.");
            await _machine.TurnAllOffAsync();
        }
    }
}
