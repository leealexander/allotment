using Allotment.Machine.Models;
using Allotment.Machine.Monitoring.Models;

namespace Allotment.Machine
{
    public enum LastDoorCommand { DoorsOpen, DoorsClosed }
    public interface IMachine
    {
        public string Title { get; }
        bool AreDoorsClosing { get; }
        bool AreDoorsOpening { get; }
        LastDoorCommand? LastDoorCommand { get; }
        bool IsWaterOn { get; }
        bool IsWaterLevelSensorOn { get; }

        Task DoorsCloseAsync();
        Task DoorsOpenAsync();
        Task WaterOnAsync();
        Task WaterOffAsync();

        Task WaterLevelSensorPowerOnAsync();
        Task WaterLevelSensorPowerOffAsync();

        public Task TurnAllOffAsync();

        public Task<SolarReadingModel?> TakeSolarReadingAsync();

        Task<bool> TryGetTempDetailsAsync(Action<TempDetails> tempDetailsFound);
    }
}
