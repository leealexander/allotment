// See https://aka.ms/new-console-template for more information

using Allotment;

Console.WriteLine("Starting temp reader, press any key to start...");
Console.ReadLine();

IotFunctions iot = new();
while (true)
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

