using Allotment.AppSettingsConfig;
using Allotment.DataStores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace Allotment
{
    public static class AllotmentAuthentication
    {
        public static IServiceCollection AddAllotmentAuthentication(this IServiceCollection services, ConfigurationManager configurationManager, AllotmentConfig allotmentConfig)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "JORC";
                options.DefaultChallengeScheme = "JORC";
            })
            .AddJwtBearer()
            .AddPolicyScheme("JORC", "JORC", options =>
            {
                // runs on each request
                options.ForwardDefaultSelector = context =>
                {
                    // filter by auth type
                    string authorization = context.Request.Headers[HeaderNames.Authorization];
                    if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                    {
                        return "Bearer";
                    }

                    // otherwise always check for cookie auth
                    return OpenIdConnectDefaults.AuthenticationScheme;
                };
            })
            .AddMicrosoftIdentityWebApp(options =>
            {
                configurationManager.GetSection("AzureAD").Bind(options);
                options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;

                options.Events.OnRedirectToIdentityProvider = ctx =>
                {
                    var settingsUrl = allotmentConfig.Auth.AuthCallbackUrl;
                    var currentRedirectUrl = new Uri(ctx.ProtocolMessage.RedirectUri);
                    settingsUrl = settingsUrl.Replace("{port}", currentRedirectUrl.Port.ToString());
                    ctx.ProtocolMessage.RedirectUri = settingsUrl;
                    return Task.CompletedTask;
                };
            });
            services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

            return services;
        }

        public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
        {
            private readonly ISettingsStore _settingsStore;
            private readonly AllotmentConfig _config;

            public ConfigureJwtBearerOptions(ISettingsStore settingsStore, AllotmentConfig config)
            {
                _settingsStore = settingsStore;
                _config = config;
            }

            public void Configure(string name, JwtBearerOptions options)
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settingsStore.Get().ApiJwtSecret));

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = _config.Auth.SiteUrl.ToString(),
                    ValidAudience = _config.Auth.SiteUrl.ToString(),
                    IssuerSigningKey = key, // temp value until we start up and grab the real one from settings.
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true
                };
            }

            public void Configure(JwtBearerOptions options)
            {
                // default case: no scheme name was specified
                Configure(string.Empty, options);
            }
        }
    }
}
