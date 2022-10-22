using Allotment.ApiModels;
using Allotment.DataStores;
using Allotment.Machine;

namespace Allotment
{
    public static class Api
    {
        public static IEndpointRouteBuilder AddAllotmentApi(this IEndpointRouteBuilder route)
        {
            route.MapGet("/api/status", (IMachineControlService service) =>
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
            });

            route.MapPost("/api/waterlevel/readings", async (PostReadingApiModel model, IHttpContextAccessor httpAccessor, IMachineControlService service, ISettingsStore settingsStore) =>
            {
                var httpContext = httpAccessor.HttpContext;

                var settings = await settingsStore.GetAsync();

                await service.StoreWaterLevelReadingAsync(model.Reading, model.ReadingTimeUtc);

                return Results.Ok();
            });

            return route;
        }
    }
}
