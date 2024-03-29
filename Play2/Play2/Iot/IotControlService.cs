﻿using Allotment.Jobs;
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
        private TimeSpan? _waterOnDuration;
        private DateTime? _waterTurnedOnAtUtc;
        private readonly IIotFunctions _functions;
        private readonly IJobManager _jobManager;

        public IotControlService(IIotFunctions functions, IJobManager jobManager)
        {
            _functions = functions;
            _jobManager = jobManager;
        }


        public bool AreDoorsClosing  => _functions.AreDoorsClosing;
        public bool AreDoorsOpening => _functions.AreDoorsOpening;
        public bool IsWaterOn => _functions.IsWaterOn;

        public async Task DoorsOpenAsync()
        {
            await _functions.DoorsOpenAsync();
        }
        public async Task DoorsCloseAsync()
        {
            await _functions.DoorsCloseAsync();
        }
        public async Task WaterOnAsync(TimeSpan duration)
        {
            _waterOnDuration = duration;
            _waterTurnedOnAtUtc = DateTime.UtcNow;

            await _functions.WaterOnAsync();
            _jobManager.RunJobIn(ctx => _functions.WaterOffAsync(), duration);
        }
        public async Task StopWaterAsync()
        {
            await _functions.WaterOffAsync();
        }
    }
}
