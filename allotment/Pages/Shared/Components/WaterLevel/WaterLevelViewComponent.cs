using Allotment.Services;
using Microsoft.AspNetCore.Mvc;

namespace Allotment.Pages.Shared.Components.WaterLevel
{
    public class WaterLevelViewComponent: ViewComponent
    {
        private readonly IWaterLevelService _waterLevelService;

        public WaterLevelViewComponent(IWaterLevelService waterLevelService)
        {
            _waterLevelService = waterLevelService;
        }

        public async Task<IViewComponentResult> Invoke()
        {
            var model = new Models.WaterLevelViewModel();
            var percentFull = await _waterLevelService.GetPercentageFullAsync();
            var level = await _waterLevelService.GetLevelAsync();
            if(level != null && percentFull != null)
            {
                model.WaterLevelStatus = $"{percentFull}% {level}cm";
            }
            else
            {
                model.WaterLevelStatus = "Sensor needs known level readings...Go to water sensor page.";
            }

            return View("Default", model);
        }
    }
}
