using Allotment.Iot.Models;
using Allotment.Jobs;
using UnitsNet;

namespace Allotment.Iot.Machine
{
    public class FakeMachine : IIotMachine
    {
        private readonly IJobManager _jobManager;
        private bool _isClosing = false;
        private bool _isOpening = false;
        private bool _isWaterOn = false;
        private static TimeSpan _operationTimeSpan = TimeSpan.FromSeconds(10);
        private int[] _dayTemp = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };
        private int[] _dayHum = new[] { 70, 72, 76, 78, 80, 82, 84, 90, 89, 84, 75, 70, 68, 67, 66, 65, 65, 58, 62, 63, 65, 67, 68 };

        public FakeMachine(IJobManager jobManager)
        {
            _jobManager = jobManager;
        }

        public bool AreDoorsClosing => _isClosing;

        public bool AreDoorsOpening => _isOpening;

        public LastDoorCommand? LastDoorCommand { get; set; }

        public bool IsWaterOn => _isWaterOn;

        public Task DoorsCloseAsync()
        {
            _isClosing = true;
            _isOpening = false;
            LastDoorCommand = Iot.LastDoorCommand.DoorsClosed;
            _jobManager.RunJobIn(_ctx =>
            {
                _isClosing = false;
                return Task.CompletedTask;
            }, _operationTimeSpan);
            return Task.CompletedTask;
        }

        public Task DoorsOpenAsync()
        {
            _isOpening = true;
            _isClosing = false;
            LastDoorCommand = Iot.LastDoorCommand.DoorsOpen;
            _jobManager.RunJobIn(_ctx =>
            {
                _isOpening = false;
                return Task.CompletedTask;
            }, _operationTimeSpan);
            return Task.CompletedTask;
        }

        public Task<bool> TryGetTempDetailsAsync(Action<TempDetails> tempDetailsFound)
        {
            var now = DateTime.Now;
            var hour = now.Hour;
            tempDetailsFound(new TempDetails
            {
                Temperature = new Temperature((double)_dayTemp[hour+1], UnitsNet.Units.TemperatureUnit.DegreeCelsius),
                Humidity = new RelativeHumidity((double)_dayHum[hour+1], UnitsNet.Units.RelativeHumidityUnit.Percent),
                TimeTakenUtc = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, DateTimeKind.Utc)
            });
            return Task.FromResult(true);
        }

        public Task TurnAllOffAsync()
        {
            _isOpening = _isClosing = _isWaterOn = false;
            return Task.CompletedTask;
        }

        public Task WaterOffAsync()
        {
            _isWaterOn = false;
            return Task.CompletedTask;
        }

        public Task WaterOnAsync()
        {
            _isWaterOn = true;
            _jobManager.RunJobIn(_ctx =>
            {
                _isWaterOn = false;
                return Task.CompletedTask;
            }, _operationTimeSpan);
            return Task.CompletedTask;
        }

        public List<TempDetails> GetDayReadings()
        {
            var now = DateTime.Now;
            var hour = now.Hour;
            var results = Enumerable.Range(0, hour + 1).Select(x => new TempDetails
            {
                Temperature = new Temperature((double)_dayTemp[x], UnitsNet.Units.TemperatureUnit.DegreeCelsius),
                Humidity = new RelativeHumidity((double)_dayHum[x], UnitsNet.Units.RelativeHumidityUnit.Percent),
                TimeTakenUtc = new DateTime(now.Year, now.Month, now.Day, x, 0, 0, DateTimeKind.Utc)
            }).ToList();
            results.Last().TimeTakenUtc = DateTime.UtcNow;
            return results;
        }

        public Task StoreReadingAsync(TempDetails details)
        {
            return Task.CompletedTask;
        }
    }
}
