using Allotment.Iot.Machine;

namespace Allotment.Iot
{
    public enum LastDoorCommand { DoorsOpen, DoorsClosed }
    public interface IIotMachine: ITemperatureSupport
    {
        bool AreDoorsClosing { get; }
        bool AreDoorsOpening { get; }
        LastDoorCommand? LastDoorCommand { get; }
        bool IsWaterOn { get; }

        Task DoorsCloseAsync();
        Task DoorsOpenAsync();
        Task WaterOnAsync();
        Task WaterOffAsync();

        public Task TurnAllOffAsync();

    }

    public interface ITemperatureSupport
    {
        Task<bool> TryGetTempDetailsAsync(Action<TempDetails> tempDetailsFound);
        List<TempDetails> GetDayReadings();
        Task StoreReadingAsync(TempDetails details);
    }
}
