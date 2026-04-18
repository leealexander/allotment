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
            route.MapGet("/api/status", async (IMachineControlService service, ISolarStore solarStore) =>
            {
                var status = service.Status;
                var solar = await solarStore.GetCurrentReadingAsync();

                return Results.Ok(new
                {
                    GeneralStatus = service.Status.Textual,
                    TakenAt = status.Temp == null ? "No readings available" : status.Temp.TimeTakenUtc.ToLocalTime().ToString(),
                    Temp = status.Temp == null ? "Unknown" : status.Temp.Temperature.ToString(),
                    BatteryCharge = solar == null ? "—" : $"{solar.Battery.StateOfCharge}%",
                    SolarWatts = solar == null ? "—" : $"{solar.SolarPanel.Watts:F1}W",
                    DoorsOpening = status.DoorsOpening,
                    DoorsClosing = status.DoorsClosing,
                    WaterOn = status.WaterOn
                });
            });

            return route;
        }
    }
}
