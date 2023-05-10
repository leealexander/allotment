using Allotment.DataStores;
using Allotment.Jobs;

namespace Allotment.Machine
{
    public class MachineStartup : IJobService
    {
        private readonly IMachine _machine;
        private readonly IAuditLogger<MachineStartup> _auditLogger;
        private readonly ISettingsStore _settingsStore;

        public MachineStartup(IMachine machine, IAuditLogger<MachineStartup> auditLogger, ISettingsStore settingsStore)
        {
            _machine = machine;
            _auditLogger = auditLogger;
            _settingsStore = settingsStore;
        }

        public async Task RunAsync(IRunContext ctx)
        {
            await _auditLogger.AuditLogAsync("Machine started.");
            await _machine.TurnAllOffAsync();
            await _settingsStore.StoreAsync(await _settingsStore.GetAsync()); // save initial settings if not stored before.
        }
    }
}
