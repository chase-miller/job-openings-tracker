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
                .AddTransient((sp) => new LeverClient(
                    sp.GetService<IHttpClientFactory>().CreateClient($"{typeof(LeverClient)}")
                ))
                .AddTransient<IJobPostingSource>(sp => new LeverService(
                    sp.GetService<LeverClient>(),
                    sp.GetService<IConfiguration>().GetSection("leverConfig").Get<LeverConfig>() ?? new LeverConfig()
                ))
                ;
        }
    }
}
