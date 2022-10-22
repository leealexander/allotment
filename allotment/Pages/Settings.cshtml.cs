using Allotment.AppSettingsConfig;
using Allotment.DataStores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Allotment.Pages
{
    public class SettingsModel : PageModel
    {
        private readonly ISettingsStore _settingsStore;
        private readonly AllotmentConfig _allotmentConfig;

        public SettingsModel(ISettingsStore settingsStore, AllotmentConfig allotmentConfig)
        {
            _settingsStore = settingsStore;
            _allotmentConfig = allotmentConfig;
        }

        [BindProperty]
        public string ?Settings { get; set; }

        [BindProperty]
        public string? Action { get; set; }

        public async Task<string> GenerateTokenAsync()
        {
            var settings = await _settingsStore.GetAsync();
            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.ApiJwtSecret));

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                     new Claim(ClaimTypes.NameIdentifier, "Api key"),
                }),
                Expires = DateTime.UtcNow.AddYears(30),
                Issuer = _allotmentConfig.Auth.SiteUrl.ToString(),
                Audience = _allotmentConfig.Auth.SiteUrl.ToString(),
                SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }



        public async Task OnGet()
        {
            await SetSettingsModelValueAsync();
        }

        public async Task<IActionResult> OnPost()
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

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Settings", ex.Message);
            }
            return Page();
        }

        private async Task SetSettingsModelValueAsync()
        {
            var settings = await _settingsStore.GetAsync();
            Settings = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
