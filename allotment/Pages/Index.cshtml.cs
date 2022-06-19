using Allotmen.Iot.Monitoring;
using Allotment.Iot;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        }

        public string TempDetails => _tempMonitor.Current == null ? "No temperature readings available" : _tempMonitor.Current.ToString();

        public string Status => _iotControlService.Status;



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