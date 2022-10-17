namespace Allotment.DataStores
{
    public static class DataStoresConfig
    {
        public static IServiceCollection AddDataStores(this IServiceCollection services)
        {
            services.AddSingleton<ITempStore, TempStore>();
            services.AddSingleton<ISettingsStore, SettingsStore>();
            services.AddSingleton<ILogsStore, LogsStore>();
            services.AddSingleton<IWaterLevelStore, WaterLevelStore>();
            services.AddSingleton(typeof(IStateStore<>), typeof(StateStore<>));
            
            return new ServiceCollection(); 
        }
    }
}
