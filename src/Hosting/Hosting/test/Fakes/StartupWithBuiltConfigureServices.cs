// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Fakes;

public class StartupWithBuiltConfigureServices
{
    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        return null;
    }

    public void Configure(IApplicationBuilder app) { }
}
