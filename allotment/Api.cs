using Allotment.ApiModels;
using Allotment.DataStores;
using Allotment.Machine;
using System.Reflection;

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

                var dt = ToDateTime(model.ReadingTimeUtc);
                await service.StoreWaterLevelReadingAsync(model.Reading, dt);

                return Results.Ok();
            });

            return route;
        }

        private static DateTime ToDateTime(int timet)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timet);
        }

    }
}
