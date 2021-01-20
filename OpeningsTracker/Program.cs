using System.Collections;
using System.Net.Http;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;
using NCrontab;

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
                        .SetMinimumLevel(LogLevel.Error)
                )
                .ConfigureServices((hostContext, services) =>
                    services
                        .AddHostedService<CronJob>(sp => new CronJob(
                            sp.GetService<OpeningsTrackerScript>(), 
                            sp.GetService<IConfiguration>().GetSection("cronConfig").Get<CronJobConfig>() ?? new CronJobConfig()
                        ))
                        .AddTransient<OpeningsTrackerScript>(sp => new OpeningsTrackerScript(
                            sp.GetService<LeverClient>(), 
                            sp.GetService<Database>(), 
                            sp.GetService<IConfiguration>().GetSection("scriptConfig").Get<OpeningsTrackerScriptConfig>() ?? new OpeningsTrackerScriptConfig()
                        ))
                        .AddHttpClient()
                        .AddTransient<Database>(sp => new Database(
                            sp.GetService<IConfiguration>().GetValue<string>("openingsTrackerDatabaseFile")
                        ))
                        .AddTransient<LeverClient>((sp) => new LeverClient(
                            sp.GetService<IHttpClientFactory>().CreateClient($"{typeof(LeverClient)}"),
                            sp.GetService<IConfiguration>().GetSection("leverClientConfig").Get<LeverClientConfig>() ?? new LeverClientConfig()
                        ))
                );
    }
}
