using Allotment.DataStores;
using Allotment.DataStores.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Allotment.Pages
{
    public class LogModel : PageModel
    {
        private readonly ILogsStore _logsStore;

        public LogModel(ILogsStore logsStore)
        {
            _logsStore = logsStore;
        }

        [BindProperty]
        public DateTime SelectedDate { get; set; } = DateTime.Now;

        public IEnumerable<LogEntryModel> Logs { get; private set; } = Enumerable.Empty<LogEntryModel>();

        public async Task OnGet()
        {
            Logs = await _logsStore.GetDayLogsAsync();
        }
        public async Task OnPost()
        {
            Logs = await _logsStore.GetDayLogsAsync(DateOnly.FromDateTime(SelectedDate));
        }
    }
}
