using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes
{
    class StartupCaseInsensitive
    {
        public static IServiceProvider ConfigureCaseInsensitiveServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "ConfigureCaseInsensitiveServices";
            });
            return services.BuildServiceProvider();
        }

        public void ConfigureCaseInsensitive(IApplicationBuilder app)
        {
        }
    }
}
