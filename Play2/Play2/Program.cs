// See https://aka.ms/new-console-template for more information

using Allotment;


IotFunctions iot = new();
Console.WriteLine("Press any key to stop");
while (!Console.KeyAvailable)
{
    Console.WriteLine("trying...");
    var getIotSuccess = await iot.TryGetTempDetailsAsync(tempDetails =>
    {
        Console.WriteLine($"Temp={tempDetails.Temperature} Humidity={tempDetails.Humidity}");
    });
    if (getIotSuccess)
    {
        await iot.OpenDoorsAsync();
        await iot.CloseDoorsAsync();
    }
    else
    {
        Console.WriteLine("FAILED");
    };
    await Task.Delay(3000);
}

