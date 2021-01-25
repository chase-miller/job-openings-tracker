using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpeningsTracker.Core;

namespace OpeningsTracker.JobPostingSources.Lever
{
    public static class StartupConfiguration
    {
        public static IServiceCollection AddLeverOpeningsTracker(this IServiceCollection services, HostBuilderContext hostContext = null)
        {
            return services
                .AddHttpClient()
                .AddLogging()
                .AddTransient<LeverClient>((sp) => new LeverClient(
                    sp.GetService<IHttpClientFactory>().CreateClient($"{typeof(LeverClient)}"),
                    sp.GetService<IConfiguration>().GetSection("leverClientConfig").Get<LeverConfig>() ?? new LeverConfig()
                ))
                .AddTransient<IJobPostingSource>(sp => new LeverService(
                    sp.GetService<LeverClient>(),
                    sp.GetService<IConfiguration>().GetSection("leverClientConfig").Get<LeverConfig>() ?? new LeverConfig()
                ))
                ;
        }
    }
}
