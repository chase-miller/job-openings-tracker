using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DynamicPluginLoader
{
    // TODO - there's got to be a more standard way to do this
    public interface IJobPostingServiceConfiguration
    {
        IServiceCollection ConfigureServices(IServiceCollection services, HostBuilderContext hostContext = null);
    }
}
