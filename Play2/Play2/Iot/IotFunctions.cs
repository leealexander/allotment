using Iot.Device.DHTxx;
using System.Device.Gpio;
using UnitsNet;

namespace Allotment.Iot
{
    public interface IIotFunctions
    {
        bool AreDoorsClosing { get; }
        bool AreDoorsOpening { get; }
        bool IsWaterOn { get; }

        Task DoorsCloseAsync();
        Task DoorsOpenAsync();
        bool IsPinOn(int pin);
        Task<bool> TryGetTempDetailsAsync(Action<TempDetails> tempDetailsFound);
        Task WaterOnAsync(TimeSpan duration);
    }

    public class IotFunctions : IIotFunctions
    {
        private const int _doorPinOpen = 26;
        private const int _doorPinClose = 19;
        private const int _waterPin = 13;
        private readonly TimeSpan _doorActionTimeDelay = TimeSpan.FromSeconds(2);

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
                        Humidity = humidity,
                        Temperature = temperature
                    });
                    return true;
                }
            }

            return false;
        }

        public bool IsWaterOn => IsPinOn(_waterPin);
        public bool AreDoorsOpening => IsPinOn(_doorPinOpen);
        public bool AreDoorsClosing => IsPinOn(_doorPinClose);

        public async Task WaterOnAsync(TimeSpan duration)
        {
            using GpioController controller = new();
            controller.OpenPin(_waterPin, PinMode.Output);
            try
            {
                controller.Write(_waterPin, PinValue.Low);
                await Task.Delay((int)duration.TotalMilliseconds);
            }
            finally
            {
                controller.Write(_waterPin, PinValue.High);
                await Task.Delay(200);
                controller.ClosePin(_waterPin);
            }
        }

        public async Task DoorsOpenAsync()
        {
            await DoorActionAsync(_doorPinOpen);
        }
        public async Task DoorsCloseAsync()
        {
            await DoorActionAsync(_doorPinClose);
        }

        private async Task DoorActionAsync(int pin)
        {
            using GpioController controller = new();
            controller.OpenPin(pin, PinMode.Output);
            try
            {
                controller.Write(pin, PinValue.Low);
                await Task.Delay((int)_doorActionTimeDelay.TotalMilliseconds);
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
            try
            {
                return controller.Read(_waterPin) == PinValue.Low;
            }
            finally
            {
                controller.ClosePin(_waterPin);
            }
        }
    }
    public record TempDetails
    {
        public Temperature Temperature { get; set; }

        public RelativeHumidity Humidity { get; set; }
    }
}
