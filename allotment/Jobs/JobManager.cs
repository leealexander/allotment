using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Allotment.Jobs
{
    public interface IJobManager: IDisposable
    {
        void RunJob(JobHandler job, DateTime startAtUtc, CancellationToken cancellationToken = default);
        void RunJobIn(JobHandler job, TimeSpan when, CancellationToken cancellationToken = default);
    }

    internal class JobManager : IJobManager, IHostedService
    {
        private readonly IServiceProvider _providor;
        private readonly ILogger<JobManager> _logger;
        private readonly List<QueuedJob> _jobsToAdd = new();
        private readonly List<QueuedJob> _queuedJobs = new();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _jobProcessorTask;

        public JobManager(JobOptions options, IServiceProvider providor, ILogger<JobManager> logger)
        {
            _providor = providor;
            _logger = logger;
            foreach (var serviceType in options.StartupList)
            {
                var job = new ScopedJobRun(serviceType, providor);
                _jobsToAdd.Add(new QueuedJob(DateTime.UtcNow, job.RunAsync, _cancellationTokenSource.Token));
            }
        }

        public void RunJobIn(JobHandler job, TimeSpan when, CancellationToken cancellationToken = default) => RunJob(job, DateTime.UtcNow + when, cancellationToken);


        public void RunJob(JobHandler job, DateTime startAtUtc, CancellationToken cancellationToken = default)
        {
            var qj = new QueuedJob(startAtUtc, job, cancellationToken);
            if (DateTime.UtcNow >= startAtUtc)
            {
                RunJobNow(qj);
            }
            else
            {
                AddJobToQueue(qj);
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

        private void RunJobNow(QueuedJob queuedJob)
        {
            Task.Run(() =>
            {
                try
                {
                    if (queuedJob.CancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation($"Not starting job {queuedJob.Job} as it's cancelled");
                    }
                    else
                    {
                        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, queuedJob.CancellationToken);
                        queuedJob.Job(new RunContext(this, queuedJob, cancellationSource.Token));
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, $"Failed to start job {queuedJob.Job}");
                }
            });
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
                RunJobNow(currentQueuedJob);
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
            private readonly QueuedJob _oldQueuedJob;
            private QueuedJob? _queuedJob = null;

            public RunContext(JobManager jobManager, QueuedJob oldQueuedJob, CancellationToken cancellationToken)
            {
                _jobManager = jobManager;
                _oldQueuedJob = oldQueuedJob;
                CancellationToken = cancellationToken;  
            }


            public CancellationToken CancellationToken { get; }

            public void RunAgainIn(TimeSpan duration, CancellationToken cancellationToken = default) => RunAgainAt(DateTime.UtcNow + duration, cancellationToken);


            public void RunAgainAt(DateTime nextRunUtc, CancellationToken cancellationToken = default)
            {
                if (_queuedJob != null)
                {
                    throw new NotSupportedException("Once set you cannot change the next run");
                }
                if(cancellationToken == default)
                {
                    cancellationToken = _oldQueuedJob.CancellationToken;
                }
                _queuedJob = new QueuedJob(nextRunUtc, _oldQueuedJob.Job, cancellationToken);
                _jobManager.AddJobToQueue(_queuedJob);
            }
        }

        private class ScopedJobRun : IJobService
        {
            private readonly Type _typeToCreate;
            private readonly IServiceProvider _serviceProvider;

            public override string ToString()
            {
                return $"Job for {_typeToCreate.Name}";
            }

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
        private class QueuedJob
        {
            public QueuedJob(DateTime startTimeUtc, JobHandler job, CancellationToken cancellationToken)
            {
                StartTimeUtc = startTimeUtc;
                Job = job;
                CancellationToken = cancellationToken;
            }

            public CancellationToken CancellationToken { get; set; }
            public DateTime StartTimeUtc { get; }
            public JobHandler Job { get; }
        }
    }
}
