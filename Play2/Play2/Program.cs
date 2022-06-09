// See https://aka.ms/new-console-template for more information

using Allotment;
using Iot.Device.DHTxx;

Console.WriteLine("Starting temp reader...");
using var dht = new Dht11(12);
while (true)
{
    var tempSuccess = dht.TryReadTemperature(out var temperature);
    var humiditySuccess = dht.TryReadHumidity(out var humidity);
    var tempText = tempSuccess ? temperature.ToString() : "FAILED";
    var humidityText = humiditySuccess ? humidity.ToString() : "FAILED";

    IotFunctions iot = new();
    while (true)
    {
        Console.WriteLine("trying...");
        var getIotResult = await iot.TryGetTempDetailsAsync(tempDetails =>
        {
            Console.WriteLine($"Temp={tempDetails.Temperature} Humidity={tempDetails.Humidity}");
        });
        if (!getIotResult)
        {
            Console.WriteLine("FAILED");
        };
        await Task.Delay(1000);
    }
}
