using Allotment.DataStores;
using Allotment.DataStores.Models;
using Allotment.Machine;
using Allotment.Machine.Monitoring.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Xml.Linq;

namespace Allotment.Pages
{
    public class WaterLevelSensorModel : PageModel
    {
        private readonly IMachineControlService _machineControlService;
        private readonly IWaterLevelStore _waterLevelStore;
        private readonly IStateStore<WaterSensorStateModel> _knownLevelStore;

        public WaterLevelSensorModel(IMachineControlService machineControlService, IWaterLevelStore waterLevelStore, IStateStore<WaterSensorStateModel> knownLevelStore)
        {
            _machineControlService = machineControlService;
            _waterLevelStore = waterLevelStore;
            _knownLevelStore = knownLevelStore;
        }

        public IEnumerable<WaterLevelReadingModel> Readings { get; set; } = Enumerable.Empty<WaterLevelReadingModel>();
        public HtmlString GraphLabels { get; set; } = HtmlString.Empty;
        public HtmlString GraphPressureReadings { get; set; } = HtmlString.Empty;
        public HtmlString GraphHeightReadings { get; set; } = HtmlString.Empty;

        public bool IsWaterSensorOn { get; private set; }
        public int MinReading { get; set; }

        [BindProperty]
        public string? KnownReadings { get; set; }

        [BindProperty]
        public int ?KnownDepthCm { get; set; }

        [BindProperty]
        public string? Annotation { get; set; }

        public async Task OnGet()
        {
            var levels = await _knownLevelStore.GetAsync();
            KnownReadings = JsonSerializer.Serialize(levels, new JsonSerializerOptions { WriteIndented = true });
            Readings = await _waterLevelStore.GetReadingsAsync(TimeSpan.FromHours(24));
            MinReading = Readings.Select(x => x.Reading).Min();
            DateTime? lastDate = null;
            GraphLabels = new HtmlString(string.Join(',', Readings.Select(x =>
            {
                var dt = x.DateTakenUtc.ToLocalTime();
                var showDay = lastDate == null || lastDate != dt.Date;
                lastDate = dt.Date;
                var dayText = showDay ? $" {dt.DayOfWeek.ToString()}" : string.Empty;
                var dateLabel = $"{dayText} {dt:HH.mm}";
                var annotation = string.IsNullOrWhiteSpace(x.Annotation) ? string.Empty : $" {x.Annotation} ";
                if (x.KnownDepthCm.HasValue)
                {
                    return $"'{annotation}{x.KnownDepthCm}cm {dateLabel}'";
                }

                return $"'{annotation}{dateLabel}'";
            })));
            GraphPressureReadings = new HtmlString(string.Join(',', Readings.Select(x => $"{x.Reading - MinReading}")));
            GraphHeightReadings = new HtmlString("");
            IsWaterSensorOn = _machineControlService.IsWaterLevelSensorOn;
        }

        private void LevelReadings(IEnumerable<WaterLevelReadingModel> readings)
        {
            if (readings.Any())
            {
                var minValue = readings.Select(x => x.Reading).Min();
                foreach (var reading in readings)
                {
                    reading.Reading -= minValue;
                }
            }
        }

        public async Task OnPostTakeKnownReading()
        {
            try
            {
                await _machineControlService.WaterLevelMonitorOnAsync(Annotation, KnownDepthCm);
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("general-error", ex.Message);
            }
        }

        public async Task<IActionResult> OnPostSetKnownReadings()
        {
            try
            {
                if (KnownReadings != null)
                {
                    var readings = JsonSerializer.Deserialize<Allotment.DataStores.Models.WaterSensorStateModel>(KnownReadings);
                    if (readings != null)
                    {
                        await _knownLevelStore.StoreAsync(readings);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(KnownReadings), ex.Message);
            }

            return Page();
        }
    }
}
