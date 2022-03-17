// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Fakes;

public class StartupConfigureServicesThrows
{
    public void ConfigureServices(IServiceCollection services)
    {
        throw new Exception("Exception from ConfigureServices");
    }

    public void Configure(IApplicationBuilder builder)
    {

    }
}
