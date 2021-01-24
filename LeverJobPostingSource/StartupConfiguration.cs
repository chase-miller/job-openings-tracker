using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpeningsTracker.Core;

namespace LeverJobPostingSource
{
    public class StartupConfiguration : IJobPostingServiceConfiguration
    {
        public IServiceCollection ConfigureServices(IServiceCollection services, HostBuilderContext hostContext = null)
        {
            return services
                .AddHttpClient()
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
