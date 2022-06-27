using Allotmen.Iot.Monitoring;
using Allotment.Iot;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace allotment.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ITempMonitor _tempMonitor;
        private readonly IIotControlService _iotControlService;


        public IndexModel(ILogger<IndexModel> logger, ITempMonitor tempMonitor, IIotControlService iotControlService)
        {
            _logger = logger;
            _tempMonitor = tempMonitor;
            _iotControlService = iotControlService;
            var readings = _tempMonitor.ReadingsByHour.ToArray();
            TempByHour = new HtmlString(string.Join(',', readings.Select(x => $"'{x?.Temperature.DegreesCelsius.ToString() ?? "null"}'")));
            HumidityByHour = new HtmlString(string.Join(',', readings.Select(x => $"'{x?.Humidity.Percent.ToString() ?? "null"}'")));
        }

        public string TempDetails 
        {
            get
            {
                if (_tempMonitor.Current == null)
                {
                    return "No temperature readings available";
                }
                var t = _tempMonitor.Current;

                return $"temp={t.Temperature}, hum={t.Humidity}, taken={t.TimeTakenUtc.ToLocalTime()}";
            }
        }

        public string Status => _iotControlService.Status;

        public HtmlString Labels => new HtmlString(string.Join(',',Enumerable.Range(0, 24).Select(x => $"'{x:D2}'")));

        public HtmlString TempByHour { get; }
        public HtmlString HumidityByHour { get; }

        public async Task<IActionResult> OnPostDoorsOpen()
        {
            await _iotControlService.DoorsOpenAsync();
            return Redirect("/");
        }

        public async Task<IActionResult> OnPostDoorsClose()
        {
            await _iotControlService.DoorsCloseAsync();
            return Redirect("/");
        }

        public async Task<IActionResult> OnPostWaterOn()
        {
            await _iotControlService.WaterOnAsync(TimeSpan.FromMinutes(10));
            return Redirect("/");
        }

        public async Task<IActionResult> OnPostWaterOff()
        {
            await _iotControlService.WaterOffAsync();
            return Redirect("/");
        }
    }
}