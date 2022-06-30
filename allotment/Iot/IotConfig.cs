using Allotmen.Iot.Monitoring;
using Allotment.Iot.Machine;
using Microsoft.Extensions.DependencyInjection;

namespace Allotment.Iot
{
    public static class IotConfig
    {
        public static IServiceCollection AddIot(this IServiceCollection services, bool isDevelopment)
        {
            services.AddSingleton<TempMonitor>();
            services.AddSingleton<ITempMonitor>(sp=>sp.GetRequiredService<TempMonitor>());
            services.AddSingleton<IIotControlService, IotControlService>();

            if (isDevelopment)
            {
                services.AddSingleton<IIotMachine, FakeMachine>();
            }
            else
            {
                services.AddSingleton<IIotMachine, PiMachine>();
            }
            services.AddTransient<IotStartup>();
            
            return services;
        }
    }
}
