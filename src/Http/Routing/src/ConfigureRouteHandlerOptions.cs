// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

internal sealed class ConfigureRouteHandlerOptions : IConfigureOptions<RouteHandlerOptions>
{
    private readonly IHostEnvironment? _environment;

    public ConfigureRouteHandlerOptions(IHostEnvironment? environment = null)
    {
        _environment = environment;
    }

    public void Configure(RouteHandlerOptions options)
    {
        if (_environment?.IsDevelopment() ?? false)
        {
            options.ThrowOnBadRequest = true;
        }
    }
}
