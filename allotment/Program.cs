using Allotmen.Iot.Monitoring;
using Allotment.Iot;
using Allotment.Jobs;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.GetSection("AzureAD").Bind(options);
        options.CorrelationCookie.SameSite = SameSiteMode.None;

        options.Events.OnRedirectToIdentityProvider = ctx =>
        {
            if (!ctx.ProtocolMessage.RedirectUri.Contains("//localhost", StringComparison.InvariantCultureIgnoreCase))
            {
                var uriBuilder = new UriBuilder(ctx.ProtocolMessage.RedirectUri)
                {
                    Scheme = Uri.UriSchemeHttps,
                    Port = 2280
                };
                ctx.ProtocolMessage.RedirectUri = uriBuilder.ToString();
            }
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

builder.Services.AddIot(builder.Environment.IsDevelopment());
builder.Services.AddJobs()
    .StartWith<IotStartup>()
    .StartWith<TempMonitor>();

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

app.MapGet("/api/status", (IIotControlService iotService) =>
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
