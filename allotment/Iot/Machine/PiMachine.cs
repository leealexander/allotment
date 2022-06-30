using Iot.Device.DHTxx;
using System.Device.Gpio;
using UnitsNet;
using UnitsNet.Units;

namespace Allotment.Iot.Machine
{
    public class PiMachine : IIotMachine
    {
        private const int _doorPinOpen = 26;
        private const int _doorPinClose = 19;
        private const int _waterPin = 13;
        private readonly TimeSpan _doorActionTimeDelay = TimeSpan.FromSeconds(50);
        private CancellationTokenSource _doorOpenCancel = new();
        private CancellationTokenSource _doorCloseCancel = new();
        private LastDoorCommand? _lastDoorCommand;

        public async Task<bool> TryGetTempDetailsAsync(Action<TempDetails> tempDetailsFound)
        {
            using var dht = new Dht22(12);
            for (int tryTimes = 0; tryTimes < 30; tryTimes++)
            {
                var tempSuccess = dht.TryReadTemperature(out var temperature);
                var humiditySuccess = dht.TryReadHumidity(out var humidity);

                await Task.Delay(100);
                if (tempSuccess && humiditySuccess)
                {
                    tempDetailsFound(new TempDetails
                    {
                        TimeTakenUtc = DateTime.UtcNow,
                        Humidity = humidity,
                        Temperature = temperature
                    }); ;
                    return true;
                }
            }

            return false;
        }

        public bool IsWaterOn => IsPinOn(_waterPin);
        public bool AreDoorsOpening => IsPinOn(_doorPinOpen);
        public bool AreDoorsClosing => IsPinOn(_doorPinClose);
        public LastDoorCommand? LastDoorCommand => _lastDoorCommand;


        public async Task TurnAllOffAsync()
        {
            using GpioController controller = new();
            controller.OpenPin(_waterPin, PinMode.Output);
            controller.OpenPin(_doorPinClose, PinMode.Output);
            controller.OpenPin(_doorPinOpen, PinMode.Output);
            controller.Write(_waterPin, PinValue.High);
            controller.Write(_doorPinClose, PinValue.High);
            controller.Write(_doorPinOpen, PinValue.High);
            await Task.Delay(200);
            controller.ClosePin(_waterPin);
            controller.ClosePin(_doorPinClose);
            controller.ClosePin(_doorPinOpen);
        }

        public async Task WaterOnAsync()
        {
            using GpioController controller = new();
            controller.OpenPin(_waterPin, PinMode.Output);
            controller.Write(_waterPin, PinValue.Low);
            await Task.Delay(200);
        }

        public async Task WaterOffAsync()
        {
            using GpioController controller = new();
            controller.OpenPin(_waterPin, PinMode.Output);
            controller.Write(_waterPin, PinValue.High);
            await Task.Delay(200);
            controller.ClosePin(_waterPin);
        }

        public async Task DoorsOpenAsync()
        {
            if (_doorOpenCancel.IsCancellationRequested)
            {
                var old = _doorOpenCancel;
                _doorOpenCancel = new CancellationTokenSource();
                old.Dispose();
            }
            _doorCloseCancel.Cancel();
            await DoorActionAsync(_doorPinOpen, _doorOpenCancel.Token);
            if (!_doorOpenCancel.IsCancellationRequested)
            {
                _lastDoorCommand = Iot.LastDoorCommand.DoorsOpen;
            }
        }
        public async Task DoorsCloseAsync()
        {
            if (_doorCloseCancel.IsCancellationRequested)
            {
                var old = _doorCloseCancel;
                _doorCloseCancel = new CancellationTokenSource();
                old.Dispose();
            }
            _doorOpenCancel.Cancel();
            await DoorActionAsync(_doorPinClose, _doorCloseCancel.Token);
            if (!_doorCloseCancel.IsCancellationRequested)
            {
                _lastDoorCommand = Iot.LastDoorCommand.DoorsClosed;
            }
        }

        private async Task DoorActionAsync(int pin, CancellationToken cancellationToken)
        {
            using GpioController controller = new();
            controller.OpenPin(pin, PinMode.Output);
            try
            {
                controller.Write(pin, PinValue.Low);
                await Task.Delay((int)_doorActionTimeDelay.TotalMilliseconds, cancellationToken);
            }
            finally
            {
                controller.Write(pin, PinValue.High);
                await Task.Delay(200);
                controller.ClosePin(pin);
            }
        }

        public bool IsPinOn(int pin)
        {
            using GpioController controller = new();
            controller.OpenPin(pin, PinMode.Output);
            return controller.Read(pin) == PinValue.Low;
        }

        public List<TempDetails> GetDayReadings()
        {
            var readings = new List<TempDetails>();
            if (File.Exists(FileForToday))
            {
                readings.AddRange(from fl in File.ReadAllLines(FileForToday)
                                   let split = fl.Split(',')
                                   where split.Length == 3
                                   select new TempDetails
                                   {
                                       TimeTakenUtc = DateTime.Parse(split[0]).ToUniversalTime(),
                                       Temperature = new UnitsNet.Temperature(double.Parse(split[1]), TemperatureUnit.DegreeCelsius),
                                       Humidity = new UnitsNet.RelativeHumidity(double.Parse(split[2]), RelativeHumidityUnit.Percent)
                                   });
            }

            return readings;
        }

        public async Task StoreReadingAsync(TempDetails details)
        {
            await File.AppendAllLinesAsync(FileForToday, new[] { $"{details.TimeTakenUtc:o},{details.Temperature.DegreesCelsius},{details.Humidity.Percent}" });
        }

        private string FileForToday
        {
            get
            {
                return $"/data/temp/{DateTime.Now:dd-MM-yyyy}.csv";
            }
        }

    }
    public record TempDetails
    {
        public DateTime TimeTakenUtc { get; set; }
        public Temperature Temperature { get; set; }

        public RelativeHumidity Humidity { get; set; }
    }
}
