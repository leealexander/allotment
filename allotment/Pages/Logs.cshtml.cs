using Allotment.DataStores;
using Allotment.DataStores.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Allotment.Pages
{
    public class LogModel : PageModel
    {
        private readonly ILogsStore _logsStore;

        public LogModel(ILogsStore logsStore)
        {
            _logsStore = logsStore;
        }

        public IEnumerable<LogEntryModel> Logs { get; private set; }

        public async Task OnGet()
        {
            Logs = await _logsStore.GetDayLogsAsync();
        }
    }
}
