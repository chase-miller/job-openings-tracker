using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpeningsTracker.Core;
using OpeningsTracker.DataStores.JsonFile;
using OpeningsTracker.JobPostingSources.Lever;
using OpeningsTracker.Notifiers.EmailNotifier;

namespace OpeningsTracker.Runners.BackgroundJob
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host
                .CreateDefaultBuilder(args)
                .ConfigureLogging(logger =>
                    logger
                        .SetMinimumLevel(LogLevel.Information)
                        .AddConsole()
                )
                .ConfigureAppConfiguration(builder =>
                {
                    builder
                        .AddJsonFile(Environment.GetEnvironmentVariable("OpeningsTracker_SettingsFileLocation") ?? "/dev/null/fake.json", optional: true)
                        .AddEnvironmentVariables(prefix: "OpeningsTracker_")
                        .AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var myConfig = hostContext.Configuration.GetSection("backgroundJob").Get<Config>() ?? new Config();

                    services
                        .AddLogging()
                        .AddHttpClient()
                        .AddIf(
                            services => services.AddOpeningsTrackerEmailNotifier(),
                            () => myConfig.ActiveNotifiers.Contains("EmailNotifier"))
                        .AddIf(
                            services => services.AddLeverOpeningsTracker(),
                            () => myConfig.ActiveSources.Contains("LeverSource"))
                        .AddOpeningsJsonDataStore()
                        .AddHostedService(sp => new CronJob(
                            sp.GetService<OpeningsTrackerPoller>(),
                            sp.GetService<IConfiguration>().GetSection("cronConfig").Get<CronJobConfig>() ?? new CronJobConfig(),
                            sp.GetService<ILoggerFactory>().CreateLogger<CronJob>()
                        ))
                        .AddTransient(sp => new OpeningsTrackerPoller(
                            sp.GetServices<IJobPostingSource>().ToList(),
                            sp.GetServices<IJobPostingNotifier>().ToList(),
                            sp.GetService<IDataStore>(),
                            sp.GetService<ILoggerFactory>().CreateLogger<OpeningsTrackerPoller>()
                        ));
                });
    }
}
