// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class ConfigureRouteOptions : IConfigureOptions<RouteOptions>
{
    private readonly ICollection<EndpointDataSource> _dataSources;

    public ConfigureRouteOptions(ICollection<EndpointDataSource> dataSources)
    {
        ArgumentNullException.ThrowIfNull(dataSources);

        _dataSources = dataSources;
    }

    public void Configure(RouteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.EndpointDataSources = _dataSources;
    }
}
