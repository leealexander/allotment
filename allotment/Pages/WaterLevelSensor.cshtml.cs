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
        public bool IsWaterSensorOn { get; private set; }

        [BindProperty]
        public string? KnownReadings { get; set; }

        [BindProperty]
        public int ?KnownDepthCm { get; set; }

        public async Task OnGet()
        {
            var levels = await _knownLevelStore.GetAsync();
            KnownReadings = JsonSerializer.Serialize(levels, new JsonSerializerOptions { WriteIndented = true });
            Readings = await _waterLevelStore.GetReadingsAsync();
            GraphLabels = new HtmlString(string.Join(',', Readings.Select(x =>
            {
                if (x.KnownDepthCm.HasValue)
                {
                    return $"'{x.KnownDepthCm}cm {x.DateTakenUtc:HH.mm.ss}'";
                }

                return $"'{x.DateTakenUtc:HH.mm.ss}'";
            })));
            GraphPressureReadings = new HtmlString(string.Join(',', Readings.Select(x => $"{x.Reading}")));
            IsWaterSensorOn = _machineControlService.IsWaterLevelSensorOn;
        }

        public async Task OnPostTakeKnownReading()
        {
            try
            {
                if (KnownDepthCm.HasValue)
                {
                    await _machineControlService.WaterLevelMonitorOnAsync(KnownDepthCm.Value);
                }
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
