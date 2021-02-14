using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace OpeningsTracker.Runners.BackgroundJob
{
    public static class ServiceConfigurationExtensions
    {
        public static IServiceCollection AddIf(this IServiceCollection services, Func<IServiceCollection, IServiceCollection> adderFunc, Func<bool> ifFunc)
        {
            return ifFunc() ? adderFunc(services) : services;
        }
    }
}
