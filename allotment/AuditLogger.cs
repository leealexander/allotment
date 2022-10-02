using Allotment.DataStores;

namespace Allotment
{
    public interface IAuditLogger<TArea>
    {
        Task LogAsync(string message);
    }

    public class AuditLogger<TArea> : IAuditLogger<TArea>
    {
        private readonly ILogsStore _logsStore;

        public AuditLogger(ILogsStore logsStore)
        {
            _logsStore = logsStore;
        }
        public async Task LogAsync(string message)
        {
            await _logsStore.StoreAsync(new DataStores.Models.LogEntryModel
            {
                EventDateUtc = DateTime.UtcNow,
                Area = typeof(TArea).Name,
                Message = message
            });
        }
    }
}
