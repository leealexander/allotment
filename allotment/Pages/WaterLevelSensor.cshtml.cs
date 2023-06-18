using Allotment.DataStores;
using Allotment.Machine;
using Allotment.Machine.Monitoring.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Allotment.Pages
{
    public class WaterLevelSensorModel : PageModel
    {
        private readonly IMachineControlService _machineControlService;
        private readonly IWaterLevelStore _waterLevelStore;

        public WaterLevelSensorModel(IMachineControlService machineControlService, IWaterLevelStore waterLevelStore)
        {
            _machineControlService = machineControlService;
            _waterLevelStore = waterLevelStore;
        }

        public IEnumerable<WaterLevelReadingModel> Readings { get; set; } = Enumerable.Empty<WaterLevelReadingModel>();
        public HtmlString GraphLabels { get; set; } = HtmlString.Empty;
        public HtmlString GraphPressureReadings { get; set; } = HtmlString.Empty;
        public bool IsWaterSensorOn { get; private set; }

        [Required]
        [BindProperty]
        public int ?KnownDepthCm { get; set; }

        public async Task OnGet()
        {
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

        public async Task OnPost()
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
    }
}
