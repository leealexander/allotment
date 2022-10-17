using Allotment.Machine.Models;
using Allotment.Jobs;
using Allotment.DataStores;
using Allotment.Machine.Monitoring.Models;
using System.Collections.Concurrent;

namespace Allotment.Machine
{
    public interface IMachineControlService
    {
        bool AreDoorsClosing { get; }
        bool AreDoorsOpening { get; }
        bool IsWaterOn { get; }
        bool IsWaterLevelSensorOn { get; }
        Task DoorsCloseAsync();
        Task DoorsOpenAsync();
        Task WaterOnAsync();
        Task WaterOffAsync();
        Task StopAllAsync();

        Task WaterLevelMonitorOnAsync( int ?knownWaterHeightCm = null );
        void StoreWaterLevelReading(DateTime dateTakenUtc, int reading);

        public CurrentStatus Status { get; }
    }

    public class MachineControlService : IMachineControlService
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly IMachine _machine;
        private readonly IJobManager _jobManager;
        private readonly ITempStore _tempStore;
        private readonly ISettingsStore _settingsStore;
        private readonly IWaterLevelStore _waterLevelStore;
        private TimeSpan _waterOnDuration;
        private DateTime ?_waterOnTimeUtc = null;
        private DateTime? _waterLevelOnUtc = null;
        private CancellationTokenSource ?_waterOffCancellation = null;
        private CancellationTokenSource? _waterLevelSensorOffCancellation = null;
        private ConcurrentBag<WaterLevelReadingModel> _currentSessionWaterLevelReadings = new();

        public MachineControlService(IMachine machine, IJobManager jobManager, ITempStore tempStore, ISettingsStore settingsStore, IWaterLevelStore waterLevelStore)
        {
            _machine = machine;
            _jobManager = jobManager;
            _tempStore = tempStore;
            _settingsStore = settingsStore;
            _waterLevelStore = waterLevelStore;
        }


        public bool AreDoorsClosing  => _machine.AreDoorsClosing;
        public bool AreDoorsOpening => _machine.AreDoorsOpening;
        public bool IsWaterOn => _machine.IsWaterOn;
        public bool IsWaterLevelSensorOn => _machine.IsWaterLevelSensorOn;

        public CurrentStatus Status
        {
            get
            {
                var status = new CurrentStatus
                {
                    DoorsOpening = _machine.AreDoorsOpening,
                    DoorsClosing = _machine.AreDoorsClosing,
                    WaterOn = _machine.IsWaterOn,
                    WaterSensorOn = _machine.IsWaterLevelSensorOn,
                };
                try
                {
                    _machine.TryGetTempDetailsAsync(x => status.Temp = x);
                    var doors = status.DoorsClosing ? "Doors closing" : "";
                    if (string.IsNullOrWhiteSpace(doors))
                    {
                        doors = status.DoorsOpening ? "Doors opening" : "";
                    }
                    if (string.IsNullOrWhiteSpace(doors))
                    {
                        doors = _machine.LastDoorCommand == null ? "Unknown door state" : _machine.LastDoorCommand.ToString();
                    }
                    var water = status.WaterOn ? $"Water is on TTL: {WaterTimeLeft()} " : "Water is off";
                    var waterLevel = status.WaterSensorOn ? $"Water level sensor is on" : "Water level sensor is off";
                    status.Textual = $"{doors} - {water} - {waterLevel}";
                }
                catch(Exception ex )
                {
                    status.Textual = $"Error: {ex.Message}";
                }

                return status;
            }
        }

        public void StoreWaterLevelReading(DateTime dateTakenUtc, int reading)
        {
            if (_machine.IsWaterLevelSensorOn)
            {
                _currentSessionWaterLevelReadings.Add(new WaterLevelReadingModel
                {
                    DateTakenUtc = dateTakenUtc,
                    Reading = reading
                });
            }
        }

        public async Task WaterLevelMonitorOnAsync(int? knownWaterHeightCm = null)
        {
            if(!_machine.IsWaterLevelSensorOn)
            {
                if (_waterLevelSensorOffCancellation is not null)
                {
                    _waterLevelSensorOffCancellation.Cancel();
                    _waterLevelSensorOffCancellation.Dispose();
                }
                var settings = await _settingsStore.GetAsync();
                _waterLevelOnUtc = DateTime.UtcNow;
                await _machine.WaterLevelSensorPowerOnAsync();

                _waterLevelSensorOffCancellation = new CancellationTokenSource();
                _currentSessionWaterLevelReadings.Clear();
                _jobManager.RunJobIn(async ctx =>
                {
                    await _machine.WaterLevelSensorPowerOffAsync();
                    if (knownWaterHeightCm.HasValue)
                    {
                        await _waterLevelStore.ApplyKnownWaterLevelAsync(knownWaterHeightCm.Value);
                    }

                    _currentSessionWaterLevelReadings.Clear();
                }, settings.Irrigation.WaterLevelSensor.PoweredOnDuration, _waterLevelSensorOffCancellation.Token);
            }
        }


        public async Task StopAllAsync()
        {
            await _machine.TurnAllOffAsync();
        }


        public async Task DoorsOpenAsync()
        {
            await _machine.DoorsOpenAsync();
        }
        public async Task DoorsCloseAsync()
        {
            await _machine.DoorsCloseAsync();
        }
        public async Task WaterOnAsync()
        {
            if (!_machine.IsWaterOn) 
            {
                if(_waterOffCancellation is not null)
                {
                    _waterOffCancellation.Cancel();
                    _waterOffCancellation.Dispose();
                }
                var settings = await _settingsStore.GetAsync();
                await _machine.WaterOnAsync();
                _waterOnDuration = settings.Irrigation.WaterOnDuration;
                _waterOnTimeUtc = DateTime.UtcNow;
                _waterOffCancellation = new CancellationTokenSource();
                _jobManager.RunJobIn(ctx => _machine.WaterOffAsync(), _waterOnDuration, _waterOffCancellation.Token);
            }
        }

        public async Task WaterOffAsync()
        {
            await _machine.WaterOffAsync();
        }

        private string WaterTimeLeft()
        {
            if (_waterOnTimeUtc.HasValue)
            {
                var left = _waterOnDuration - (DateTime.UtcNow - _waterOnTimeUtc.Value);
                return $"{(int)left.TotalMinutes} mins {(int)left.Seconds} secs";
            }

            return "";
        }
    }
}
