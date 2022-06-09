using Iot.Device.DHTxx;
using System.Device.Gpio;
using UnitsNet;

namespace Allotment
{
    public record TempDetails
    {
        public Temperature Temperature { get; set; }

        public RelativeHumidity Humidity { get; set; }
    }

    public class IotFunctions
    {
        private const int _doorPinOpen = 26;
        private const int _doorPinClose = 19;
        private const int _waterPin = 13;
        private readonly TimeSpan _doorActionTimeDelay = TimeSpan.FromSeconds(2);

        public async Task<bool> TryGetTempDetailsAsync(Action<TempDetails> tempDetailsFound)
        {
            using var dht = new Dht11(12);
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

        public bool IsWaterOn()
        {
            using GpioController controller = new();
            controller.OpenPin(_waterPin, PinMode.Output);
            try
            {
                return controller.Read(_waterPin) == PinValue.Low;
            }
            finally
            {
                controller.ClosePin(_waterPin);
            }
        }

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
    }
}
