using Allotment.Machine.Monitoring;
using Allotment.Machine.Readers;

namespace Allotment.Machine
{
    public static class MachineConfig
    {
        public static IServiceCollection AddMachine(this IServiceCollection services, bool isDevelopment)
        {
            services.AddSingleton<TempMonitor>();
            services.AddSingleton<SolarMonitor>();
            services.AddSingleton<AutoPilot>();
            services.AddSingleton<IMachineControlService, MachineControlService>();
            services.AddSingleton<ISolarReader, SolarReader>();
            services.AddScoped<IPressureReader, PressureReader>();
            
            services.AddSingleton<WaterLevelMonitor>();
            

            if (isDevelopment)
            {
                services.AddSingleton<IMachine, FakeMachine>();
            }
            else
            {
                services.AddSingleton<IMachine, PiMachine>();
            }
            services.AddTransient<MachineStartup>();
            
            return services;
        }
    }
}
