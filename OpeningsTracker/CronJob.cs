using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace OpeningsTracker
{
    class CronJobConfig
    {
        public TimeSpan DelayTimeSpan { get; set; }
        public bool OnlyRunOnce { get; set; }
    }

    class CronJob : BackgroundService
    {
        private readonly OpeningsTrackerScript _script;
        private readonly CronJobConfig _config;

        public CronJob(OpeningsTrackerScript script, CronJobConfig config)
        {
            _script = script;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_config.OnlyRunOnce)
            {
                await _script.StartAsync(stoppingToken);
                Console.WriteLine();
                Console.WriteLine("End of script (OnlyRunOnce is set to true)");
                return;
            }

            do
            {
                await _script.StartAsync(stoppingToken);
                Console.WriteLine();
                Console.WriteLine($"Waiting {_config.DelayTimeSpan:g} until next run...");
                await Task.Delay(_config.DelayTimeSpan, stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }
    }
}