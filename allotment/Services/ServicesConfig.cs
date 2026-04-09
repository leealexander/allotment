namespace Allotment.Services
{
    public static class ServicesConfig
    {
        public static IServiceCollection AddAllotmentServices(this IServiceCollection services)
        {
            services.AddTransient<IWaterLevelService, WaterLevelService>();
            services.AddTransient<ICurrentTempService, CurrentTempService>();
            services.AddSingleton<IVersionService, VersionService>();
            return services;
        }
    }
}
