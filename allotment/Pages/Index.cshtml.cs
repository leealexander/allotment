using Allotment.DataStores;
using Allotment.Machine;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;

namespace allotment.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ITempStore _tempStore;
        private readonly IMachineControlService _machineControlService;
        private readonly ISettingsStore _settingsStore;

        public IndexModel(ILogger<IndexModel> logger, ITempStore tempStore, IMachineControlService machineControlService, ISettingsStore settingsStore)
        {
            _logger = logger;
            _tempStore = tempStore; 
            _machineControlService = machineControlService;
            _settingsStore = settingsStore;
        }

        public async Task OnGetAsync()
        {
            var readings = _tempStore.ReadingsByHour.ToArray();
            TempByHour = new HtmlString(string.Join(',', readings.Select(x => $"'{x?.Temperature.DegreesCelsius.ToString() ?? "null"}'")));
            HumidityByHour = new HtmlString(string.Join(',', readings.Select(x => $"'{x?.Humidity.Percent.ToString() ?? "null"}'")));

            var settings = await _settingsStore.GetAsync();
            AutopilotEnabled = settings.Autopilot.Enabled;
        }

        public string MachineTitle => _machineControlService.MachineTitle;

        public string Status => _machineControlService.Status.Textual;

        public HtmlString Labels => new HtmlString(string.Join(',',Enumerable.Range(0, 24).Select(x => $"'{x:D2}'")));

        public HtmlString TempByHour { get; set; } = HtmlString.Empty;
        public HtmlString HumidityByHour { get; set; } = HtmlString.Empty;

        [BindProperty]
        public bool AutopilotEnabled { get; set; }

        public async Task<IActionResult> OnPostDoorsOpen()
        {
            await _machineControlService.DoorsOpenAsync();
            return Redirect("/");
        }

        public async Task<IActionResult> OnPostToggleAutoPilot()
        {
            var settings = await _settingsStore.GetAsync();
            settings.Autopilot.Enabled = AutopilotEnabled;
            await _settingsStore.StoreAsync(settings);
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
        public IActionResult OnPostDownloadData()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            ZipFile.CreateFromDirectory(DataStore.BaseDir, tempFile, CompressionLevel.Fastest, includeBaseDirectory: false);

            var stream = new FileStream(tempFile, FileMode.Open);
            return File(stream, "application/zip", "allotment-data.zip");
        }
    }
}