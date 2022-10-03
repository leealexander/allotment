using Allotment.Machine;
using Allotment.Jobs;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Allotment.Machine.Monitoring;
using Allotment.DataStores;
using Allotment;

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
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

builder.Services.AddMachine(builder.Environment.IsDevelopment());
builder.Services.AddJobs()
    .StartWith<MachineStartup>()
    .StartWith<TempMonitor>()
    .StartWith<WaterLevelMonitor>();
builder.Services.AddDataStores();
builder.Services.AddTransient(typeof(IAuditLogger<>), typeof(AuditLogger<>));

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

app.MapRazorPages();
app.MapControllers();

app.MapGet("/api/status", (IMachineControlService iotService) =>
{
    var status = iotService.Status;

    return Results.Ok(new
    {
        GeneralStatus = iotService.Status.Textual,
        TakenAt = status.Temp == null ? "No readings available" : status.Temp.TimeTakenUtc.ToLocalTime().ToString(),
        Temp = status.Temp == null ? "Unknown" : status.Temp.Temperature.ToString(),
        Humidity = status.Temp == null ? "Unknown" : status.Temp.Humidity.ToString(),
        DoorsOpening = status.DoorsOpening, 
        DoorsClosing = status.DoorsClosing,
        WaterOn = status.WaterOn
    });
});

app.Run();
