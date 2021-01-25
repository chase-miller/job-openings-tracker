using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DynamicPluginLoader
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddPlugins(this IServiceCollection services, HostBuilderContext hostContext = null, params string[] additionalDirectories)
        {
            var assemblies = (additionalDirectories ?? new string[0])
                .Concat(new[] {System.AppDomain.CurrentDomain.BaseDirectory})
                .SelectMany(directory => Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories))
                .Select(path => System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path))
                .SelectMany(a => a.DefinedTypes, (a, definedType) => (a, definedType));

            foreach (var (assembly, typeInfo) in assemblies)
            {
                if (!typeInfo.ImplementedInterfaces.Contains(typeof(IJobPostingServiceConfiguration))) 
                    continue;

                var instance = assembly.CreateInstance(typeInfo.FullName) as IJobPostingServiceConfiguration;
                
                instance?.ConfigureServices(services, hostContext);
            }

            return services;
        }
    }
}
