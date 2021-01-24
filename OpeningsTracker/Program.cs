using System.Collections;
using System.Net.Http;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;
using NCrontab;
using OpeningsTracker.Core;

namespace OpeningsTracker
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
                .ConfigureServices((hostContext, services) =>
                    services
                        .AddLogging()
                        .AddHttpClient()
                        .ScanForPlugins(hostContext)
                        .AddHostedService<CronJob>(sp => new CronJob(
                            sp.GetService<OpeningsTrackerScript>(), 
                            sp.GetService<IConfiguration>().GetSection("cronConfig").Get<CronJobConfig>() ?? new CronJobConfig(),
                            sp.GetService<ILoggerFactory>().CreateLogger<CronJob>()
                        ))
                        .AddTransient<OpeningsTrackerScript>(sp => new OpeningsTrackerScript(
                            sp.GetService<IJobPostingSource>(), 
                            sp.GetService<Database>(), 
                            sp.GetService<IConfiguration>().GetSection("scriptConfig").Get<OpeningsTrackerScriptConfig>() ?? new OpeningsTrackerScriptConfig(),
                            sp.GetService<ILoggerFactory>().CreateLogger<OpeningsTrackerScript>()
                        ))
                        .AddTransient<Database>(sp => new Database(
                            sp.GetService<IConfiguration>().GetValue<string>("openingsTrackerDatabaseFile")
                        ))
                );
    }
}
