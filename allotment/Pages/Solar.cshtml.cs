using Allotment.DataStores;
using Allotment.Machine.Monitoring.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Allotment.Pages
{
    public class SolarModel : PageModel
    {
        private readonly ISolarStore _solarStore;

        public SolarModel(ISolarStore solarStore)
        {
            _solarStore = solarStore;
        }
        public async Task OnGet()
        {
            LastReading = (await _solarStore.GetReadingsAsync()).LastOrDefault() ?? new SolarReadingModel();
        }

        public SolarReadingModel LastReading { get; set; } = new SolarReadingModel();
    }
}
