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
        private readonly OpeningsTrackerScript _script;
        private readonly CronJobConfig _config;
        private readonly ILogger<CronJob> _logger;

        public CronJob(OpeningsTrackerScript script, CronJobConfig config, ILogger<CronJob> logger)
        {
            _script = script;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_config.OnlyRunOnce)
            {
                await _script.ExecuteAsync(stoppingToken);
                _logger.LogWarning("End of script (OnlyRunOnce is set to true)");
                return;
            }

            do
            {
                await _script.ExecuteAsync(stoppingToken);
                _logger.LogInformation($"Waiting {_config.JobFrequencyTimespan:g} until next run...");
                await Task.Delay(_config.JobFrequencyTimespan, stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }
    }
}