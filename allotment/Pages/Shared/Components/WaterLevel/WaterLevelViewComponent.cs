using Microsoft.AspNetCore.Mvc;

namespace Allotment.Pages.Shared.Components.WaterLevel
{
    public class WaterLevelViewComponent: ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("Default", new Models.WaterLevelViewModel
            {
            });
        }
    }
}
