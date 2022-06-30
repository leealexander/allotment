using Allotment.Iot.Models;
using Allotment.Jobs;

namespace Allotment.Iot
{
    public interface IIotControlService
    {
        bool AreDoorsClosing { get; }
        bool AreDoorsOpening { get; }
        bool IsWaterOn { get; }
        Task DoorsCloseAsync();
        Task DoorsOpenAsync();
        Task WaterOnAsync(TimeSpan duration);
        Task WaterOffAsync();
        Task StopAllAsync();

        public CurrentStatus Status { get; }
    }

    public class IotControlService : IIotControlService
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly IIotMachine _functions;
        private readonly IJobManager _jobManager;

        public IotControlService(IIotMachine functions, IJobManager jobManager)
        {
            _functions = functions;
            _jobManager = jobManager;
        }


        public bool AreDoorsClosing  => _functions.AreDoorsClosing;
        public bool AreDoorsOpening => _functions.AreDoorsOpening;
        public bool IsWaterOn => _functions.IsWaterOn;

        public CurrentStatus Status
        {
            get
            {
                var status = new CurrentStatus
                {
                    DoorsOpening = _functions.AreDoorsOpening,
                    DoorsClosing = _functions.AreDoorsClosing,
                    WaterOn = _functions.IsWaterOn,
                };
                try
                {
                    _functions.TryGetTempDetailsAsync(x => status.Temp = x);
                    var doors = status.DoorsClosing ? "Doors closing" : "";
                    if (string.IsNullOrWhiteSpace(doors))
                    {
                        doors = status.DoorsOpening ? "Doors opening" : "";
                    }
                    if (string.IsNullOrWhiteSpace(doors))
                    {
                        doors = _functions.LastDoorCommand == null ? "Unknown door state" : _functions.LastDoorCommand.ToString();
                    }
                    var water = status.WaterOn ? "Water is on" : "Water is off";
                    status.Textual = $"{doors} - {water}";
                }
                catch(Exception ex )
                {
                    status.Textual = $"Error: {ex.Message}";
                }

                return status;
            }
        }

        public async Task StopAllAsync()
        {
            await _functions.TurnAllOffAsync();
        }


        public async Task DoorsOpenAsync()
        {
            await _functions.DoorsOpenAsync();
        }
        public async Task DoorsCloseAsync()
        {
            await _functions.DoorsCloseAsync();
        }
        public async Task WaterOnAsync(TimeSpan duration)
        {
            await _functions.WaterOnAsync();
            _jobManager.RunJobIn(ctx => _functions.WaterOffAsync(), duration);
        }
        public async Task WaterOffAsync()
        {
            await _functions.WaterOffAsync();
        }
    }
}
