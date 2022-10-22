namespace Allotment.AppSettingsConfig
{
    public static class SettingsServices
    {
        public static AllotmentConfig AddAllotmentConfig(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            var options = new AllotmentConfig();
            configurationManager.GetSection("AllotmentOptions").Bind(options);

            services.AddSingleton(Guard.Validate(options));
            services.AddSingleton(options);

            return options;
        }
    }
}
