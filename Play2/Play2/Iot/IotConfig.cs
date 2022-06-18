using Allotmen.Iot.Monitoring;
using Microsoft.Extensions.DependencyInjection;

namespace Allotment.Iot
{
    public static class IotConfig
    {
        public static IServiceCollection AddIot(this IServiceCollection services)
        {
            services.AddSingleton<TempMonitor>();
            services.AddSingleton<ITempMonitor>(sp=>sp.GetRequiredService<TempMonitor>());
            services.AddSingleton<IIotControlService, IotControlService>();
            services.AddSingleton<IIotFunctions, IotFunctions>();
            return services;
        }
    }
}
