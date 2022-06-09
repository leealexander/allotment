// See https://aka.ms/new-console-template for more information

using Allotment;
using System.Device.Gpio;

Console.WriteLine("Starting...");
using GpioController controller = new();
var end = false;
controller.OpenPin(26, PinMode.Output);
controller.OpenPin(19, PinMode.Output);
controller.OpenPin(13, PinMode.Output);
controller.Write(26, PinValue.High);
controller.Write(19, PinValue.High);
controller.Write(13, PinValue.High);

while (!end)
{
    Console.WriteLine("Enter command:");
    var command = Console.ReadLine();
    switch (command?.ToLower())
    {
        case "open door":
            Console.WriteLine(" opening..");
            controller.Write(26, PinValue.Low);
            Console.WriteLine(" done!");
            break;
        case "close door":
            Console.WriteLine(" closing..");
            controller.Write(19, PinValue.Low);
            Console.WriteLine(" done!");
            break;
        case "Water on":
            Console.WriteLine(" water on..");
            controller.Write(13, PinValue.Low);
            Console.WriteLine(" done!");
            break;
        case "Water off":
            Console.WriteLine(" water on..");
            controller.Write(13, PinValue.High);
            Console.WriteLine(" done!");
            break;
        case "low":
            Console.WriteLine(" all to low....");
            controller.Write(26, PinValue.High);
            controller.Write(19, PinValue.High);
            Console.WriteLine(" done!");
            break;
        case "exit":
        case "quit":
        case "e":
        case "q":
            end = true;
            break;
    }
}

Console.WriteLine("shutting down....");
controller.ClosePin(26);
controller.ClosePin(19);
controller.ClosePin(13);
Console.WriteLine("Finished!");

//IotFunctions iot = new();
//Console.WriteLine("Press any key to stop");
//while (!Console.KeyAvailable)
//{
//    Console.WriteLine("trying...");
//    var getIotSuccess = await iot.TryGetTempDetailsAsync(tempDetails =>
//    {
//        Console.WriteLine($"Temp={tempDetails.Temperature} Humidity={tempDetails.Humidity}");
//    });
//    if (getIotSuccess)
//    {
//        await iot.OpenDoorsAsync();
//        await iot.CloseDoorsAsync();
//    }
//    else
//    {
//        Console.WriteLine("FAILED");
//    };
//    await Task.Delay(3000);
//}

