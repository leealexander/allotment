using Allotment.DataStores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Allotment.Pages
{
    public class SettingsModel : PageModel
    {
        private readonly ISettingsStore _settingsStore;

        public SettingsModel(ISettingsStore settingsStore)
        {
            _settingsStore = settingsStore;
        }

        [BindProperty]
        public string ?Settings { get; set; }

        [BindProperty]
        public string? Action { get; set; }

        public async Task OnGet()
        {
            await SetSettingsModelValueAsync();
        }
        public async Task OnPost()
        {
            try
            {
                switch (Action)
                {
                    case "Save":
                        if (Settings != null)
                        {
                            var settings = JsonSerializer.Deserialize<Allotment.DataStores.Models.SettingsModel>(Settings);
                            if (settings != null)
                            {
                                await _settingsStore.StoreAsync(settings);
                            }
                        }
                        else
                        {
                            await SetSettingsModelValueAsync();
                            throw new ArgumentException("Settings couldn't be deserialised into json");
                        }
                        break;

                    case "Reset":
                        await _settingsStore.StoreAsync(new DataStores.Models.SettingsModel());
                        await SetSettingsModelValueAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Settings", ex.Message);
            }
        }

        private async Task SetSettingsModelValueAsync()
        {
            var settings = await _settingsStore.GetAsync();
            Settings = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
