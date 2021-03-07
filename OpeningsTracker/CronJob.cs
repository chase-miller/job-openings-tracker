using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpeningsTracker.Core;

namespace OpeningsTracker.Runners.BackgroundJob
{
    public class CronJobConfig
    {
        public TimeSpan JobFrequencyTimespan { get; set; }
        public bool OnlyRunOnce { get; set; }
    }

    public class CronJob : BackgroundService
    {
        private readonly OpeningsTrackerPoller _poller;
        private readonly CronJobConfig _config;
        private readonly ILogger<CronJob> _logger;
        private readonly Random _random = new Random();

        public CronJob(OpeningsTrackerPoller poller, CronJobConfig config, ILogger<CronJob> logger)
        {
            _poller = poller;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_config.OnlyRunOnce)
            {
                await _poller.ExecuteAsync(stoppingToken);
                _logger.LogWarning("End of poller (OnlyRunOnce is set to true)");
                return;
            }

            do
            {
                await _poller.ExecuteAsync(stoppingToken);
                
                var waitTime = NextExecution();
                _logger.LogInformation($"Waiting {waitTime:g} until next run...");
                await Task.Delay(waitTime, stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);

            TimeSpan NextExecution()
            {
                var randomSeconds = _random.Next(0, 120);
                return _config.JobFrequencyTimespan.Add(TimeSpan.FromSeconds(randomSeconds));
            }
        }
    }
}