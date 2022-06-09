﻿// See https://aka.ms/new-console-template for more information
using Iot.Device.DHTxx;

Console.WriteLine("Starting temp reader...");
using var dht = new Dht11(12);
while (true)
{
    var tempSuccess = dht.TryReadTemperature(out var temperature);
    var humiditySuccess = dht.TryReadHumidity(out var humidity);
    var tempText = tempSuccess ? temperature.ToString() : "FAILED";
    var humidityText = humiditySuccess ? humidity.ToString() : "FAILED";

    Console.WriteLine($"{tempText} - {humidityText}");
    await Task.Delay(1000);
}
