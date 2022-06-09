// See https://aka.ms/new-console-template for more information

using Allotment;
using System.Device.Gpio;

using GpioController controller = new();
var end = false;
while (!end)
{
    var command = Console.ReadLine();
    controller.OpenPin(26, PinMode.Output);
    controller.OpenPin(19, PinMode.Output);
    switch (command?.ToLower())
    {
        case "open door":
            controller.Write(26, PinValue.High);
            break;
        case "close door":
            controller.Write(19, PinValue.High);
            break;
        case "low":
            controller.Write(26, PinValue.Low);
            controller.Write(19, PinValue.Low);
            break;
        case "exit":
        case "quit":
        case "e":
        case "q":
            end = true;
            break;
    }
    controller.Write(26, PinValue.Low);
    controller.Write(19, PinValue.Low);
    controller.ClosePin(26);
    controller.ClosePin(19);
}

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

