namespace Allotment.DataStores
{
    public static class DataStoresConfig
    {
        public static IServiceCollection AddDataStores(this IServiceCollection services)
        {
            services.AddSingleton<ITempStore, TempStore>();
            services.AddSingleton<ISettingsStore, SettingsStore>();
            services.AddSingleton<ILogsStore, LogsStore>();

            return new ServiceCollection(); 
        }
    }
}
