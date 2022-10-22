using Allotment.Machine;
using Allotment.Jobs;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Allotment.Machine.Monitoring;
using Allotment.DataStores;
using Allotment;
using Allotment.Services;
using Allotment.ApiModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Memory;
using Allotment.AppSettingsConfig;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.GetSection("AzureAD").Bind(options); 
        options.CorrelationCookie.SameSite = SameSiteMode.None;

        options.Events.OnRedirectToIdentityProvider = ctx =>
        {
            ctx.ProtocolMessage.RedirectUri = builder.Configuration["AuthCallbackUrl"];
            return Task.CompletedTask;
        };
    });


builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    //options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();


builder.Services
    .AddMachine(builder.Environment.IsDevelopment())
    .AddAllotmentConfig(builder.Configuration)
    .AddDataStores()
    .AddAllotmentServices()
    .AddTransient(typeof(IAuditLogger<>), typeof(AuditLogger<>));

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddJobs()
    .StartWith<MachineStartup>()
    .StartWith<TempMonitor>()
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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages().RequireAuthorization();
app.MapControllers();

app.MapGet("/api/status", (IMachineControlService service) =>
{
    var status = service.Status;

    return Results.Ok(new
    {
        GeneralStatus = service.Status.Textual,
        TakenAt = status.Temp == null ? "No readings available" : status.Temp.TimeTakenUtc.ToLocalTime().ToString(),
        Temp = status.Temp == null ? "Unknown" : status.Temp.Temperature.ToString(),
        Humidity = status.Temp == null ? "Unknown" : status.Temp.Humidity.ToString(),
        DoorsOpening = status.DoorsOpening,
        DoorsClosing = status.DoorsClosing,
        WaterOn = status.WaterOn
    });
}).RequireAuthorization();

app.MapPost("/api/waterlevel/readings", async (PostReadingApiModel model, IHttpContextAccessor httpAccessor, IMachineControlService service, ISettingsStore settingsStore) =>
{
    var httpContext = httpAccessor.HttpContext;

    var settings = await settingsStore.GetAsync();

    await service.StoreWaterLevelReadingAsync(model.Reading, model.ReadingTimeUtc);

    return Results.Ok();
});

app.Run();
