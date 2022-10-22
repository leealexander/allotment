using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Allotment.Jobs
{
    public static class JobsConfig
    {
        public static JobOptions AddJobs(this IServiceCollection services)
        {
            services.AddSingleton<IJobManager, JobManager>();
            services.AddSingleton<IHostedService>(x => (IHostedService)x.GetRequiredService<IJobManager>());
            var options = new JobOptions();
            services.AddSingleton(options);
            return options;
        }
    }

    public sealed class JobOptions
    {
        public List<Type> TypesStartupList { get; } = new();
        public List<(JobHandler JobHandler, TimeSpan TimeSpan)> DelegateStartupList { get; } = new();


        public JobOptions StartWith<TImplementation>() where TImplementation : class, IJobService
        {
            TypesStartupList.Add(typeof(TImplementation));
            return this;
        }
        public JobOptions StartWith(JobHandler handler, TimeSpan when)
        {
            DelegateStartupList.Add((handler,when));
            return this;
        }
    }
}
