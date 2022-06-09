// See https://aka.ms/new-console-template for more information

using Allotment;
using System.Device.Gpio;

Console.WriteLine("Starting...");
IotFunctions iot = new();

bool end = false;
while (!end)
{
    Console.WriteLine("Enter command:");
    var command = Console.ReadLine();
    switch (command?.ToLower())
    {
        case "temp":
            Console.WriteLine(" Reading temp..");
            var result = await iot.TryGetTempDetailsAsync(r =>
            {
                Console.WriteLine($" T={r.Temperature} H={r.Humidity}");
            });
            if (!result)
            {
                Console.WriteLine(" failed to read temp");
            }
            break;
        case "door open":
            Console.WriteLine(" opening..");
            await iot.DoorsOpenAsync();
            Console.WriteLine(" done!");
            break;
        case "door close":
            Console.WriteLine(" closing..");
            await iot.DoorsCloseAsync();
            Console.WriteLine(" done!");
            break;
        case "water on":
            Console.WriteLine(" water on for 3 secs..");
            await iot.WaterOnAsync(TimeSpan.FromSeconds(3));
            break;
        case "water on?":
            Console.WriteLine($" Is water on? anwser={iot.IsWaterOn()}");
            break;
        case "exit":
        case "quit":
        case "e":
        case "q":
            end = true;
            break;
    }
}

