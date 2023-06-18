using Allotment.DataStores;
using Allotment.Machine.Models;
using Allotment.Machine.Monitoring.Models;
using Allotment.Machine.Readers;
using Iot.Device.DHTxx;
using System.Device.Gpio;

namespace Allotment.Machine
{
    public class PiMachine : IMachine
    {
        private const int _doorPinOpen = 19;
        private const int _doorPinClose = 26;
        private const int _waterPin = 13;
        private const int _waterLevelSensorPowerPin = 6;
        private readonly ISettingsStore _settingsStore;
        private readonly IAuditLogger<PiMachine> _auditLogger;
        private readonly ISolarReader _solarReader;
        private CancellationTokenSource _doorOpenCancel = new();
        private CancellationTokenSource _doorCloseCancel = new();
        private LastDoorCommand? _lastDoorCommand;

        public string Title => "Live";

        public bool IsPressurSensorOn => IsPinOn(_waterLevelSensorPowerPin);

        public bool IsWaterOn => IsPinOn(_waterPin);
        public bool IsWaterLevelSensorOn => IsPinOn(_waterLevelSensorPowerPin);
        public bool AreDoorsOpening => IsPinOn(_doorPinOpen);
        public bool AreDoorsClosing => IsPinOn(_doorPinClose);
        public LastDoorCommand? LastDoorCommand => _lastDoorCommand;

        public PiMachine(ISettingsStore settingsStore, IAuditLogger<PiMachine> auditLogger, ISolarReader solarReader)
        {
            _settingsStore = settingsStore;
            _auditLogger = auditLogger;
            _solarReader = solarReader;
        }

        public async Task<SolarReadingModel?> TakeSolarReadingAsync()
        {
            return await _solarReader.TakeReadingAsync();
        }

        public async Task TurnAllOffAsync()
        {
            await _auditLogger.AuditLogAsync("Turn all off.");
            using GpioController controller = new();
            controller.OpenPin(_waterPin, PinMode.Output);
            controller.OpenPin(_doorPinClose, PinMode.Output);
            controller.OpenPin(_doorPinOpen, PinMode.Output);
            controller.OpenPin(_waterLevelSensorPowerPin, PinMode.Output);

            controller.Write(_waterPin, PinValue.High);
            controller.Write(_doorPinClose, PinValue.High);
            controller.Write(_doorPinOpen, PinValue.High);
            controller.Write(_waterLevelSensorPowerPin, PinValue.High);

            await Task.Delay(200);
            controller.ClosePin(_waterPin);
            controller.ClosePin(_doorPinClose);
            controller.ClosePin(_doorPinOpen);
            controller.ClosePin(_waterLevelSensorPowerPin);
        }



        public async Task WaterLevelSensorPowerOnAsync()
        {
            await _auditLogger.AuditLogAsync("Water butt level sensor on.");
            using GpioController controller = new();
            controller.OpenPin(_waterLevelSensorPowerPin, PinMode.Output);
            controller.Write(_waterLevelSensorPowerPin, PinValue.Low);
            await Task.Delay(200);
        }

        public async Task WaterLevelSensorPowerOffAsync()
        {
            await _auditLogger.AuditLogAsync("Water butt level sensor off.");
            using GpioController controller = new();
            controller.OpenPin(_waterLevelSensorPowerPin, PinMode.Output);
            controller.Write(_waterLevelSensorPowerPin, PinValue.High);
            await Task.Delay(200);
            controller.ClosePin(_waterLevelSensorPowerPin);
        }


        public async Task WaterOnAsync()
        {
            await _auditLogger.AuditLogAsync("Water on.");
            using GpioController controller = new();
            controller.OpenPin(_waterPin, PinMode.Output);
            controller.Write(_waterPin, PinValue.Low);
            await Task.Delay(200);
        }

        public async Task WaterOffAsync()
        {
            await _auditLogger.AuditLogAsync("Water off.");
            using GpioController controller = new();
            controller.OpenPin(_waterPin, PinMode.Output);
            controller.Write(_waterPin, PinValue.High);
            await Task.Delay(200);
            controller.ClosePin(_waterPin);
        }

        public async Task DoorsOpenAsync()
        {
            await _auditLogger.AuditLogAsync("Doors open.");
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
                _lastDoorCommand = Machine.LastDoorCommand.DoorsOpen;
            }
        }
        public async Task DoorsCloseAsync()
        {
            await _auditLogger.AuditLogAsync("Doors close.");
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
                _lastDoorCommand = Machine.LastDoorCommand.DoorsClosed;
            }
        }


        public bool IsPinOn(int pin)
        {
            using GpioController controller = new();
            controller.OpenPin(pin, PinMode.Output);
            return controller.Read(pin) == PinValue.Low;
        }

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




        private async Task DoorActionAsync(int pin, CancellationToken cancellationToken)
        {
            using GpioController controller = new();
            controller.OpenPin(pin, PinMode.Output);
            try
            {
                controller.Write(pin, PinValue.Low);
                var onForMillSecs = (int)(await _settingsStore.GetAsync()).Doors.ActionPowerOnDuration.TotalMilliseconds;
                await Task.Delay(onForMillSecs, cancellationToken);
            }
            finally
            {
                controller.Write(pin, PinValue.High);
                await Task.Delay(200);
                controller.ClosePin(pin);
            }
        }
    }
}
