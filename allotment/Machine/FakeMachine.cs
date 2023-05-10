using Allotment.Machine.Models;
using Allotment.Jobs;
using UnitsNet;
using Allotment.DataStores;
using Allotment.Machine.Monitoring.Models;

namespace Allotment.Machine
{
    public class FakeMachine : IMachine
    {
        private readonly IJobManager _jobManager;
        private readonly IAuditLogger<FakeMachine> _auditLogger;
        private readonly IWaterLevelStore _waterLevelStore;
        private bool _isClosing = false;
        private bool _isOpening = false;
        private bool _isWaterOn = false;
        private bool _isWaterLevelMonitorOn = false;
        private static TimeSpan _operationTimeSpan = TimeSpan.FromSeconds(10);
        private int[] _dayTemp = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };
        private int[] _dayHum = new[] { 70, 72, 76, 78, 80, 82, 84, 90, 89, 84, 75, 70, 68, 67, 66, 65, 65, 58, 62, 63, 65, 67, 68 };

        private int[] _samplePressureReadings = new[] 
        {
            4081, // 4cm
            4124, // 30cm
            4122, // 28cm
            4121, // 26cm
            4119, // 24cm
            4116, // 22cm
            4109, // 20cm
            4098, // 10cm
            4084, // 05cm
            4123, // 30cm
        };
        private int _samplePressureReadingsIndex = 0;

        public FakeMachine(IJobManager jobManager, IAuditLogger<FakeMachine> auditLogger, IWaterLevelStore waterLevelStore)
        {
            _jobManager = jobManager;
            _auditLogger = auditLogger;
            _waterLevelStore = waterLevelStore;
        }

        public bool AreDoorsClosing => _isClosing;

        public bool AreDoorsOpening => _isOpening;

        public LastDoorCommand? LastDoorCommand { get; set; }

        public bool IsWaterOn => _isWaterOn;
        public bool IsWaterLevelSensorOn => _isWaterLevelMonitorOn;

        public string Title => "Debug fake machine";

        public async Task DoorsCloseAsync()
        {
            await _auditLogger.AuditLogAsync("Doors close.");
            _isClosing = true;
            _isOpening = false;
            LastDoorCommand = Machine.LastDoorCommand.DoorsClosed;
            _jobManager.RunJobIn(_ctx =>
            {
                _isClosing = false;
                return Task.CompletedTask;
            }, _operationTimeSpan);
        }

        public async Task DoorsOpenAsync()
        {
            await _auditLogger.AuditLogAsync("Doors open.");
            _isOpening = true;
            _isClosing = false;
            LastDoorCommand = Machine.LastDoorCommand.DoorsOpen;
            _jobManager.RunJobIn(_ctx =>
            {
                _isOpening = false;
                return Task.CompletedTask;
            }, _operationTimeSpan);
        }

        public Task<bool> TryGetTempDetailsAsync(Action<TempDetails> tempDetailsFound)
        {
            var now = DateTime.Now;
            var hour = now.Hour;
            tempDetailsFound(new TempDetails
            {
                Temperature = new Temperature(_dayTemp[hour + 1], UnitsNet.Units.TemperatureUnit.DegreeCelsius),
                Humidity = new RelativeHumidity(_dayHum[hour + 1], UnitsNet.Units.RelativeHumidityUnit.Percent),
                TimeTakenUtc = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, DateTimeKind.Utc)
            });
            return Task.FromResult(true);
        }

        public async Task TurnAllOffAsync()
        {
            await _auditLogger.AuditLogAsync("Turn all off.");
            _isWaterLevelMonitorOn = _isOpening = _isClosing = _isWaterOn = false;
        }

        public async Task WaterOffAsync()
        {
            await _auditLogger.AuditLogAsync("Water off.");
            _isWaterOn = false;
        }

        public async Task WaterOnAsync()
        {
            await _auditLogger.AuditLogAsync("Water on.");
            _isWaterOn = true;
        }



        public List<TempDetails> GetDayReadings()
        {
            var now = DateTime.Now;
            var hour = now.Hour;
            var results = Enumerable.Range(0, hour + 1).Select(x => new TempDetails
            {
                Temperature = new Temperature(_dayTemp[x], UnitsNet.Units.TemperatureUnit.DegreeCelsius),
                Humidity = new RelativeHumidity(_dayHum[x], UnitsNet.Units.RelativeHumidityUnit.Percent),
                TimeTakenUtc = new DateTime(now.Year, now.Month, now.Day, x, 0, 0, DateTimeKind.Utc)
            }).ToList();
            results.Last().TimeTakenUtc = DateTime.UtcNow;
            return results;
        }

        public async Task StoreTempReadingAsync(TempDetails details)
        {
            await Task.CompletedTask;
        }

        public async Task WaterLevelSensorPowerOnAsync()
        {
            await _auditLogger.AuditLogAsync("Water butt level sensor on.");
            _isWaterLevelMonitorOn = true;
            await _waterLevelStore.StoreReadingAsync( new WaterLevelReadingModel { DateTakenUtc = DateTime.UtcNow, Reading = _samplePressureReadings[_samplePressureReadingsIndex++] });
        }

        public async Task WaterLevelSensorPowerOffAsync()
        {
            await _auditLogger.AuditLogAsync("Water butt level sensor off.");
            _isWaterLevelMonitorOn = false;
        }

        public Task<SolarReadingModel?> TakeSolarReadingAsync()
        {
            var m = new SolarReadingModel();
            m.DeviceStatus.Controller.Description = "Fake reading";
            m.DateTakenUtc = DateTime.UtcNow;
            m.Battery.StateOfCharge = 100;
            m.SolarPanel.Watts = 20;
            m.SolarPanel.Voltage = 24;
            return Task.FromResult((SolarReadingModel?)m);
        }
    }
}
