using Allotment.DataStores;

namespace Allotment
{
    public interface IAuditLogger<TArea>
    {
        Task AuditLogAsync(string message);
        void LogInformation(string message);
    }

    public class AuditLogger<TArea> : IAuditLogger<TArea>
    {
        private readonly ILogsStore _logsStore;
        private readonly ILogger<TArea> _logger;

        public AuditLogger(ILogsStore logsStore, ILogger<TArea> logger)
        {
            _logsStore = logsStore;
            _logger = logger;
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        public async Task AuditLogAsync(string message)
        {
            _logger.LogInformation(message);
            await _logsStore.StoreAsync(new DataStores.Models.LogEntryModel
            {
                EventDateUtc = DateTime.UtcNow,
                Area = typeof(TArea).Name,
                Message = message
            });
        }
    }
}
