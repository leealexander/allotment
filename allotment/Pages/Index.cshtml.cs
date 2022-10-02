using Allotment.Machine;
using Allotment.Machine.Monitoring;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace allotment.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ITempMonitor _tempMonitor;
        private readonly IMachineControlService _machineControlService;


        public IndexModel(ILogger<IndexModel> logger, ITempMonitor tempMonitor, IMachineControlService machineControlService)
        {
            _logger = logger;
            _tempMonitor = tempMonitor; 
            _machineControlService = machineControlService;
            var readings = _tempMonitor.ReadingsByHour.ToArray();
            TempByHour = new HtmlString(string.Join(',', readings.Select(x => $"'{x?.Temperature.DegreesCelsius.ToString() ?? "null"}'")));
            HumidityByHour = new HtmlString(string.Join(',', readings.Select(x => $"'{x?.Humidity.Percent.ToString() ?? "null"}'")));
        }


        public string Status => _machineControlService.Status.Textual;

        public HtmlString Labels => new HtmlString(string.Join(',',Enumerable.Range(0, 24).Select(x => $"'{x:D2}'")));

        public HtmlString TempByHour { get; }
        public HtmlString HumidityByHour { get; }

        public async Task<IActionResult> OnPostDoorsOpen()
        {
            await _machineControlService.DoorsOpenAsync();
            return Redirect("/");
        }

        public async Task<IActionResult> OnPostDoorsClose()
        {
            await _machineControlService.DoorsCloseAsync();
            return Redirect("/");
        }

        public async Task<IActionResult> OnPostWaterOn()
        {
            await _machineControlService.WaterOnAsync();
            return Redirect("/");
        }

        public async Task<IActionResult> OnPostWaterOff()
        {
            await _machineControlService.WaterOffAsync();
            return Redirect("/");
        }

        public async Task<IActionResult> OnPostStopAll()
        {
            await _machineControlService.StopAllAsync();
            return Redirect("/");
        }
    }
}