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
    private readonly ITempMonitor _tempMonitor;

    public MyService(ITempMonitor tempMonitor)
    {
        _tempMonitor = tempMonitor;
    }
    public Task RunAsync(IRunContext ctx)
    {
        var temp = _tempMonitor.Current;
        if (temp != null)
        {
            Console.WriteLine(temp.ToString());
        }
        else
        {
            Console.WriteLine("No Readings currently...");
        }
        ctx.RunAgainIn(TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }
}

//bool end = false;
//while (!end)
//{
//    Console.WriteLine("Enter command:");
//    var command = Console.ReadLine();
//    switch (command?.ToLower())
//    {
//        case "temp":
//            Console.WriteLine(" Reading temp..");
//            var result = await iot.TryGetTempDetailsAsync(r =>
//            {
//                Console.WriteLine($" T={r.Temperature} H={r.Humidity}");
//            });
//            if (!result)
//            {
//                Console.WriteLine(" failed to read temp");
//            }
//            break;
//        case "door open":
//            Console.WriteLine(" opening..");
//            await iot.DoorsOpenAsync();
//            Console.WriteLine(" done!");
//            break;
//        case "door close":
//            Console.WriteLine(" closing..");
//            await iot.DoorsCloseAsync();
//            Console.WriteLine(" done!");
//            break;
//        case "water on":
//            Console.WriteLine(" water on for 10 secs..");
//            _ =iot.WaterOnAsync(TimeSpan.FromSeconds(10));
//            break;
//        case "water on?":
//            Console.WriteLine($" Is water on? anwser={iot.IsWaterOn()}");
//            break;
//        case "exit":
//        case "quit":
//        case "e":
//        case "q":
//            end = true;
//            break;
//    }
//}

