// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes;

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
