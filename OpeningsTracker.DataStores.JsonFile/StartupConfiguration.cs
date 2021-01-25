using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpeningsTracker.Core;

namespace OpeningsTracker.DataStores.JsonFile
{
    public static class StartupConfiguration
    {
        public static IServiceCollection AddOpeningsJsonDataStore(this IServiceCollection services)
        {
            return services
                .AddTransient<IDataStore>(sp => new DataStore(
                    sp.GetService<IConfiguration>().GetValue<string>("openingsTrackerDatabaseFile")
                ));
        }
        
    }
}
