using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OpeningsTracker.Core
{
    // TODO - there's got to be a more standard way to do this
    public interface IJobPostingServiceConfiguration
    {
        public IServiceCollection ConfigureServices(IServiceCollection services, HostBuilderContext hostContext = null);
    }
}
