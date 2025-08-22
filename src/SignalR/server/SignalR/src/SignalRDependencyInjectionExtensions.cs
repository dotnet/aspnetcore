// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up SignalR services in an <see cref="IServiceCollection" />.
/// </summary>
public static class SignalRDependencyInjectionExtensions
{
    /// <summary>
    /// Adds hub specific options to an <see cref="ISignalRServerBuilder"/>.
    /// </summary>
    /// <typeparam name="THub">The hub type to configure.</typeparam>
    /// <param name="signalrBuilder">The <see cref="ISignalRServerBuilder"/>.</param>
    /// <param name="configure">A callback to configure the hub options.</param>
    /// <returns>The same instance of the <see cref="ISignalRServerBuilder"/> for chaining.</returns>
    public static ISignalRServerBuilder AddHubOptions<THub>(this ISignalRServerBuilder signalrBuilder, Action<HubOptions<THub>> configure) where THub : Hub
    {
        ArgumentNullException.ThrowIfNull(signalrBuilder);

        signalrBuilder.Services.AddSingleton<IConfigureOptions<HubOptions<THub>>, HubOptionsSetup<THub>>();
        signalrBuilder.Services.Configure(configure);
        return signalrBuilder;
    }

    /// <summary>
    /// Adds SignalR services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>An <see cref="ISignalRServerBuilder"/> that can be used to further configure the SignalR services.</returns>
    public static ISignalRServerBuilder AddSignalR(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMetrics();
        services.AddConnections();
        // Disable the WebSocket keep alive since SignalR has it's own
        services.Configure<WebSocketOptions>(o => o.KeepAliveInterval = TimeSpan.Zero);
        services.TryAddSingleton<SignalRMarkerService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<HubOptions>, HubOptionsSetup>());
        return services.AddSignalRCore();
    }

    /// <summary>
    /// Adds SignalR services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configure">An <see cref="Action{HubOptions}"/> to configure the provided <see cref="HubOptions"/>.</param>
    /// <returns>An <see cref="ISignalRServerBuilder"/> that can be used to further configure the SignalR services.</returns>
    public static ISignalRServerBuilder AddSignalR(this IServiceCollection services, Action<HubOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        var signalrBuilder = services.AddSignalR();
        // Setup users settings after we've setup ours
        services.Configure(configure);
        return signalrBuilder;
    }
}
