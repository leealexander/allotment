namespace Allotment.Services
{
    public static class ServicesConfig
    {
        public static IServiceCollection AddAllotmentServices(this IServiceCollection services)
        {
            services.AddScoped<IWaterLevelService, WaterLevelService>();
            return services;
        }
    }
}
