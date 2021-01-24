using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OpeningsTracker.Core
{
    public static class StartupExtensions
    {
        public static IServiceCollection ScanForPlugins(this IServiceCollection services, HostBuilderContext hostContext = null, params string[] additionalDirectories)
        {
            var assemblies = (additionalDirectories ?? new string[0])
                .Concat(new[] {System.AppDomain.CurrentDomain.BaseDirectory})
                .SelectMany(d => Directory.GetFiles(d, "*.dll", SearchOption.AllDirectories))
                .Select(f => System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(f));

            var types = assemblies.SelectMany(a => a.DefinedTypes, (a, definedType) => (a, definedType));

            foreach (var (assembly, ti) in types)
            {
                if (!ti.ImplementedInterfaces.Contains(typeof(IJobPostingServiceConfiguration))) 
                    continue;

                var instance = assembly.CreateInstance(ti.FullName) as IJobPostingServiceConfiguration;
                
                instance?.ConfigureServices(services, hostContext);
            }

            return services;
        }
    }
}
