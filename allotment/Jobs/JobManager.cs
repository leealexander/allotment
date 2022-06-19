using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Allotment.Jobs
{
    public interface IJobManager: IDisposable
    {
        void RunJob(JobHandler job, DateTime? startAtUtc = null);
        void RunJobIn(JobHandler job, TimeSpan when);
    }

    internal class JobManager : IJobManager, IHostedService
    {
        private readonly IServiceProvider _providor;
        private readonly List<QueuedJob> _jobsToAdd = new();
        private readonly List<QueuedJob> _queuedJobs = new();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _jobProcessorTask;

        public JobManager(JobOptions options, IServiceProvider providor)
        {
            _providor = providor;
            foreach (var serviceType in options.StartupList)
            {
                var job = new ScopedJobRun(serviceType, providor);
                _jobsToAdd.Add(new QueuedJob(DateTime.UtcNow, job.RunAsync));
            }
        }

        public void RunJobIn(JobHandler job, TimeSpan when) => RunJob(job, DateTime.UtcNow + when);


        public void RunJob(JobHandler job, DateTime? startAtUtc = null)
        {
            if (startAtUtc is null)
            {
                RunJobNow(job);
            }
        }


        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _jobProcessorTask = Task.Factory.StartNew(
                function: DoWorkAsync,
                cancellationToken: _cancellationTokenSource.Token,
                creationOptions: TaskCreationOptions.LongRunning,
                scheduler: TaskScheduler.Default
                );
            return Task.CompletedTask;
        }

        async Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            if (_jobProcessorTask != null)
            {
                _cancellationTokenSource.Cancel();
                await Task.WhenAny(_jobProcessorTask, Task.Delay(-1, cancellationToken));
                _jobProcessorTask = null;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }

        private void AddJobToQueue(QueuedJob job)
        {
            lock (_jobsToAdd)
            {
                _jobsToAdd.Add(job);
            }
        }

        private void RunJobNow(JobHandler job)
        {
            Task.Run(() => job(new RunContext(this, job)));
        }

        private async Task DoWorkAsync()
        {
            var token = _cancellationTokenSource.Token;
            try
            {
                while (true)
                {
                    ProcessAdditions();
                    ProcessJobsToRun();

                    await Task.Delay(100, token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignore as we are now going to shutdown
            }
        }

        private void ProcessJobsToRun()
        {
            for (int i = 0; i < _queuedJobs.Count; i++)
            {
                var currentQueuedJob = _queuedJobs[i];
                if (currentQueuedJob.StartTimeUtc > DateTime.UtcNow)
                {
                    break;
                }
                _queuedJobs.RemoveAt(i);
                RunJobNow(currentQueuedJob.Job);
            }
        }

        private void ProcessAdditions()
        {
            lock (_jobsToAdd)
            {
                if (_jobsToAdd.Count > 0)
                {
                    var newCapacity = _jobsToAdd.Count + _queuedJobs.Count;
                    if (newCapacity > _queuedJobs.Capacity)
                    {
                        _queuedJobs.Capacity = newCapacity;
                    }
                    foreach (var qj in _jobsToAdd)
                    {
                        _queuedJobs.Add(qj);
                    }
                    _queuedJobs.Sort((x, y) => DateTime.Compare(x.StartTimeUtc,y.StartTimeUtc));
                }
                _jobsToAdd.Clear();
            }
        }

        private class RunContext : IRunContext
        {
            private readonly JobManager _jobManager;
            private readonly JobHandler _job;
            private QueuedJob? _queuedJob = null;

            public RunContext(JobManager jobManager, JobHandler job)
            {
                _jobManager = jobManager;
                _job = job;
            }

            public void RunAgainIn(TimeSpan duration) => RunAgainAt(DateTime.UtcNow + duration);


            public void RunAgainAt(DateTime nextRunUtc)
            {
                if (_queuedJob != null)
                {
                    throw new NotSupportedException("Once set you cannot change the next run");
                }
                _queuedJob = new QueuedJob(nextRunUtc, _job);
                _jobManager.AddJobToQueue(_queuedJob);
            }
        }

        private record QueuedJob
        {
            public QueuedJob(DateTime startTimeUtc, JobHandler job)
            {
                StartTimeUtc = startTimeUtc;
                Job = job;
            }

            public DateTime StartTimeUtc { get; set; }
            public JobHandler Job { get; set; }
        }

        private class ScopedJobRun : IJobService
        {
            private readonly Type _typeToCreate;
            private readonly IServiceProvider _serviceProvider;

            public ScopedJobRun(Type typeToCreate, IServiceProvider serviceProvider)
            {
                _typeToCreate = typeToCreate;
                _serviceProvider = serviceProvider;
            }
            
            public async Task RunAsync(IRunContext ctx)
            {
                using var scope = _serviceProvider.CreateScope();
                var jobService = (IJobService)scope.ServiceProvider.GetRequiredService(_typeToCreate);
                await jobService.RunAsync(ctx);
            }
        }
    }
}
