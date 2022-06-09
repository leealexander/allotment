using Iot.Device.DHTxx;
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
        public async Task<bool> TryGetTempDetailsAsync(Action<TempDetails> tempDetailsFound)
        {
            using var dht = new Dht11(12);
            for(int tryTimes = 0; tryTimes < 30; tryTimes++)
            {
                var tempSuccess = dht.TryReadTemperature(out var temperature);
                var humiditySuccess = dht.TryReadHumidity(out var humidity);
                var tempText = tempSuccess ? temperature.ToString() : "FAILED";
                var humidityText = humiditySuccess ? humidity.ToString() : "FAILED";

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
    }
}
