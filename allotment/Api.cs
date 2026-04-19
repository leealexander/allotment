using Allotment.ApiModels;
using Allotment.DataStores;
using Allotment.DataStores.Models;
using Allotment.Machine;
using System.Reflection;

namespace Allotment
{
    public static class Api
    {
        public static IEndpointRouteBuilder AddAllotmentApi(this IEndpointRouteBuilder route)
        {
            route.MapGet("/api/status", async (IMachineControlService service, ISolarStore solarStore, IStateStore<WaterSensorStateModel> waterState) =>
            {
                var status = service.Status;
                var solar = await solarStore.GetCurrentReadingAsync();
                var water = await waterState.GetAsync();

                DateTime? electricalTakenUtc = status.Temp?.TimeTakenUtc ?? solar?.DateTakenUtc;
                DateTime? waterTakenUtc = water.LastReading?.DateTakenUtc;

                return Results.Ok(new
                {
                    GeneralStatus = service.Status.Textual,
                    ElectricalTakenAtUtc = electricalTakenUtc?.ToString("o"),
                    WaterTakenAtUtc = waterTakenUtc?.ToString("o"),
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
