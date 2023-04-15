// See https://aka.ms/new-console-template for more information

using Allotmen.Iot.Monitoring;
using Allotment.Iot;
using Allotment.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddIot();
        services.AddScoped<MyService>();
        services.AddJobs()
            .StartWith<MyService>()
            .StartWith<TempMonitor>();
    })
    .Build()
    .RunAsync();


public class MyService : IJobService
{
    private readonly IIotFunctions _iot;

    public MyService(IIotFunctions iot)
    {
        _iot = iot;
    }
    public async Task RunAsync(IRunContext ctx)
    {
        bool end = false;
        while (!end)
        {
            Console.WriteLine("Enter command:");
            var command = Console.ReadLine();
            switch (command?.ToLower())
            {
                case "temp":
                    Console.WriteLine(" Reading temp..");
                    var result = await _iot.TryGetTempDetailsAsync(r =>
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
                    await _iot.DoorsOpenAsync();
                    Console.WriteLine(" done!");
                    break;
                case "door close":
                    Console.WriteLine(" closing..");
                    await _iot.DoorsCloseAsync();
                    Console.WriteLine(" done!");
                    break;
                case "water on":
                    Console.WriteLine(" water on for 10 secs..");
                    await _iot.WaterOnAsync();
                    break;
                case "water on?":
                    Console.WriteLine($" Is water on? anwser={_iot.IsWaterOn}");
                    break;
                case "h":
                case "help":
                    Console.WriteLine($"Commands:");
                    Console.WriteLine($"temp");
                    Console.WriteLine($"door open");
                    Console.WriteLine($"door close");
                    Console.WriteLine($"water on");
                    Console.WriteLine($"water on?");
                    break;
                case "exit":
                case "quit":
                case "e":
                case "q":
                    end = true;
                    break;
            }
        }
    }
}

