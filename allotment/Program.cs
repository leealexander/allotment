using Allotment;
using Allotment.ApiModels;
using Allotment.AppSettingsConfig;
using Allotment.DataStores;
using Allotment.Jobs;
using Allotment.Machine;
using Allotment.Machine.Monitoring;
using Allotment.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var allotmentConfig = services.AddAllotmentConfig(builder.Configuration);

if(allotmentConfig.Auth.AuthenticationEnabled)
{
    services.AddAllotmentAuthentication(builder.Configuration, allotmentConfig);

    services.AddAuthorization(options =>
    {
        // By default, all incoming requests will be authorized according to the default policy.
        options.FallbackPolicy = options.DefaultPolicy;
    });
    services.AddRazorPages()
        .AddMicrosoftIdentityUI();
}
else
{
    services.AddRazorPages();
}




services
    .AddMachine(builder.Environment.IsDevelopment())
    .AddDataStores()
    .AddAllotmentServices()
    .AddTransient(typeof(IAuditLogger<>), typeof(AuditLogger<>));

services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

services.AddJobs()
    .StartWith<MachineStartup>()
    .StartWith<TempMonitor>()
    .StartWith<SolarMonitor>()
    .StartWith<WaterLevelMonitor>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCookiePolicy(new CookiePolicyOptions
{
    Secure = CookieSecurePolicy.Always
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

if (allotmentConfig.Auth.AuthenticationEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRazorPages().RequireAuthorization();
}
else
{
    app.MapRazorPages();
}

app.MapControllers();

app.AddAllotmentApi();


app.Run();
