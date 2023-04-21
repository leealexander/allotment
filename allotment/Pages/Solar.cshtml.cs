using Allotment.DataStores;
using Allotment.Machine.Monitoring.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Allotment.Pages
{
    public class SolarModel : PageModel
    {
        private readonly ISolarStore _solarStore;
        private SolarStore.SolarHourReading[]? _stats = null;

        public SolarModel(ISolarStore solarStore)
        {
            _solarStore = solarStore;
        }
        public async Task OnGet()
        {
            LastReading = await _solarStore.GetCurrentReadingAsync() ?? new SolarReadingModel();
        }
        public HtmlString Labels => new HtmlString(string.Join(',', Enumerable.Range(0, 24).Select(x => $"'{x:D2}'")));

        public async Task<HtmlString> GetSolarWattageByHourAsync()
        {
            var stats = await GetStatsAsync();
            return new HtmlString(string.Join(',', stats.Select(x => $"'{x?.SolarPanel.Watts.ToString() ?? "null"}'")));
        }

        public async Task<HtmlString> GetBatterySocByHourAsync()
        {
            var stats = await GetStatsAsync();
            return new HtmlString(string.Join(',', stats.Select(x => $"'{x?.Battery.StateOfCharge.ToString() ?? "null"}'")));
        }


        public SolarReadingModel LastReading { get; set; } = new SolarReadingModel();

        private async Task<SolarStore.SolarHourReading[]> GetStatsAsync()
        {
            if(_stats == null)
            {
                _stats = await _solarStore.GetReadingsByHourAsync();
            }

            return _stats;
        }
    }
}
