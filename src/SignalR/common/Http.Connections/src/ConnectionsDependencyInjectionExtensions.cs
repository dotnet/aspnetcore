// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ConnectionsDependencyInjectionExtensions
{
    /// <summary>
    /// Adds required services for ASP.NET Core Connection Handlers to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddConnections(this IServiceCollection services)
    {
        services.AddRoutingCore();
        services.AddAuthorization();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ConnectionOptions>, ConnectionOptionsSetup>());
        services.TryAddSingleton<HttpConnectionDispatcher>();
        services.TryAddSingleton<HttpConnectionManager>();
        services.TryAddSingleton<HttpConnectionsMetrics>();
        return services;
    }

    /// <summary>
    /// Adds required services for ASP.NET Core Connection Handlers to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="options">A callback to configure  <see cref="ConnectionOptions" /></param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddConnections(this IServiceCollection services, Action<ConnectionOptions> options)
    {
        return services.Configure(options)
            .AddConnections();
    }
}
