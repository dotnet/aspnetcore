using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Fakes
{
    public class StartupWithNullConfigureServices
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return null;
        }

        public void Configure(IApplicationBuilder app) { }
    }
}