using Allotment.Machine;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Client;

namespace Allotment.Pages
{
    public class WaterLevelSensorModel : PageModel
    {
        private readonly IMachineControlService _machineControlService;

        public WaterLevelSensorModel(IMachineControlService machineControlService)
        {
            _machineControlService = machineControlService;
        }
        public void OnGet()
        {
        }

        public async Task OnPost()
        {
            await _machineControlService.WaterLevelMonitorOnAsync();
        }
    }
}
