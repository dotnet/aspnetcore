// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Fakes;

public class StartupBase
{
    public void ConfigureBaseClassServices(IServiceCollection services)
    {
        services.AddOptions();
        services.Configure<FakeOptions>(o =>
        {
            o.Configured = true;
            o.Environment = "BaseClass";
        });
    }
}
