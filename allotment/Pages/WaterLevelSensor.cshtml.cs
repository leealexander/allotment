using Allotment.DataStores;
using Allotment.Machine;
using Allotment.Machine.Monitoring.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.Net;

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
            if (KnownDepthCm.HasValue)
            {
                await _machineControlService.WaterLevelMonitorOnAsync(KnownDepthCm.Value);
            }
        }

        public Task<IActionResult> OnPostReadings(int reading, DateTime startTimeUtc)
        {
            _machineControlService.StoreWaterLevelReading(reading, startTimeUtc);
            return Task.FromResult(new ContentResult()
            {
                Content = "read",
                StatusCode = (int)HttpStatusCode.Created
            });
        }
    }
}
