// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RoutingSandbox.Framework;

public static class FrameworkEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapFramework(this IEndpointRouteBuilder endpoints, Action<FrameworkConfigurationBuilder> configure)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var dataSource = endpoints.ServiceProvider.GetRequiredService<FrameworkEndpointDataSource>();

        var configurationBuilder = new FrameworkConfigurationBuilder(dataSource);
        configure(configurationBuilder);

        endpoints.DataSources.Add(dataSource);

        return dataSource;
    }
}
