// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides options for configuring Server-Side Blazor.
/// </summary>
public static class ServerSideBlazorBuilderExtensions
{
    /// <summary>
    /// Adds options to configure circuits.
    /// </summary>
    /// <param name="builder">The <see cref="IServerSideBlazorBuilder"/>.</param>
    /// <param name="configure">A callback to configure <see cref="CircuitOptions"/>.</param>
    /// <returns>The <see cref="IServerSideBlazorBuilder"/>.</returns>
    public static IServerSideBlazorBuilder AddCircuitOptions(this IServerSideBlazorBuilder builder, Action<CircuitOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure<CircuitOptions>(configure);

        return builder;
    }

    /// <summary>
    /// Adds hub options for the configuration of the SignalR Hub used by Server-Side Blazor.
    /// </summary>
    /// <param name="builder">The <see cref="IServerSideBlazorBuilder"/>.</param>
    /// <param name="configure">A callback to configure the hub options.</param>
    /// <returns>The <see cref="IServerSideBlazorBuilder"/>.</returns>
    public static IServerSideBlazorBuilder AddHubOptions(this IServerSideBlazorBuilder builder, Action<HubOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure<HubOptions<ComponentHub>>(configure);

        return builder;
    }
}
