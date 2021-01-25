using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpeningsTracker.Core;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Crmf;


namespace OpeningsTracker.Notifiers.EmailNotifier
{
    public static class StartupConfiguration
    {
        public static IServiceCollection AddOpeningsTrackerEmailNotifier(this IServiceCollection services)
        {
            return services
                    .AddLogging()
                    .AddTransient<IJobPostingNotifier>(sp => new EmailNotifier(
                        sp.GetService<ILoggerFactory>().CreateLogger<EmailNotifier>(),
                        sp.GetService<IConfiguration>().GetSection("emailNotifier").Get<EmailNotifierConfig>()
                    ))
                ;
        }
    }
}
