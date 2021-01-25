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
                .ConfigureHostConfiguration(builder =>
                    builder
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile(Environment.GetEnvironmentVariable("OpeningsTracker_SettingsFileLocation") ?? "/dev/null/fake.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables(prefix: "OpeningsTracker_")
                        .AddCommandLine(args)
                )
                .ConfigureServices((hostContext, services) =>
                    services
                        .AddLogging()
                        .AddHttpClient()
                        .AddOpeningsTrackerEmailNotifier()
                        .AddLeverOpeningsTracker()
                        .AddOpeningsJsonDataStore()
                        .AddHostedService(sp => new CronJob(
                            sp.GetService<OpeningsTrackerScript>(), 
                            sp.GetService<IConfiguration>().GetSection("cronConfig").Get<CronJobConfig>() ?? new CronJobConfig(),
                            sp.GetService<ILoggerFactory>().CreateLogger<CronJob>()
                        ))
                        .AddTransient(sp => new OpeningsTrackerScript(
                            sp.GetServices<IJobPostingSource>().ToList(),
                            sp.GetServices<IJobPostingNotifier>().ToList(),
                            sp.GetService<IDataStore>(), 
                            sp.GetService<ILoggerFactory>().CreateLogger<OpeningsTrackerScript>()
                        ))
                );
    }
}
