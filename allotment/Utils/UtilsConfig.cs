
namespace Allotment.Utils
{
    public static class UtilsConfig
    {
        public static IServiceCollection AddUtils(this IServiceCollection services)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            return services;
        }
    }
}
