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

        public async Task OpenDoorsAsync()
        {
            await DoorActionAsync(_doorPinOpen);
        }
        public async Task CloseDoorsAsync()
        {
            await DoorActionAsync(_doorPinClose);
        }

        private async Task DoorActionAsync(int pin)
        {
            Console.WriteLine($"Creating GPIO {pin}...");
            using GpioController controller = new();
            controller.OpenPin(pin, PinMode.Output);
            Console.WriteLine($"Setting {pin} pin to ouput...");
            try
            {
                Console.WriteLine($"{pin} to high...");
                controller.Write(pin, PinValue.High);
                await Task.Delay((int)_doorActionTimeDelay.TotalMilliseconds);
            }
            finally
            {
                Console.WriteLine($"{pin} to low...");
                controller.Write(pin, PinValue.Low);
                await Task.Delay(1000);
            }
            Console.WriteLine($"{pin} Done!");
        }
    }
}
