using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Allotment.Iot
{
    public interface IIotControlService
    {
        bool AreDoorsClosing { get; }
        bool AreDoorsOpening { get; }
        bool IsWaterOn { get; }
        Task DoorsCloseAsync();
        Task DoorsOpenAsync();
        Task StopWaterAsync();
        Task WaterOnAsync(TimeSpan duration);
    }

    public class IotControlService : IIotControlService
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _task;
        private TimeSpan? _waterOnDuration;
        private DateTime? _waterTurnedOnAtUtc;


        public bool AreDoorsClosing { get; }
        public bool AreDoorsOpening { get; }
        public bool IsWaterOn { get; }



        public Task DoorsOpenAsync()
        {
            return Task.CompletedTask;
        }
        public Task DoorsCloseAsync()
        {
            return Task.CompletedTask;
        }
        public Task WaterOnAsync(TimeSpan duration)
        {
            _waterOnDuration = duration;
            _waterTurnedOnAtUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }
        public Task StopWaterAsync()
        {
            _waterTurnedOnAtUtc = null;
            _waterOnDuration = null;
            return Task.CompletedTask;
        }

    }
}
