// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class SignalRDependencyInjectionExtensions
{
    /// <summary>
    /// Adds the minimum essential SignalR services to the specified <see cref="IServiceCollection" />. Additional services
    /// must be added separately using the <see cref="ISignalRServerBuilder"/> returned from this method.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>An <see cref="ISignalRServerBuilder"/> that can be used to further configure the SignalR services.</returns>
    public static ISignalRServerBuilder AddSignalRCore(this IServiceCollection services)
    {
        services.TryAddSingleton<SignalRCoreMarkerService>();
        services.TryAddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultHubLifetimeManager<>));
        services.TryAddSingleton(typeof(IHubProtocolResolver), typeof(DefaultHubProtocolResolver));
        services.TryAddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
        AddTypedHubContext(services);
        services.TryAddSingleton(typeof(HubConnectionHandler<>), typeof(HubConnectionHandler<>));
        services.TryAddSingleton(typeof(IUserIdProvider), typeof(DefaultUserIdProvider));
        services.TryAddSingleton(typeof(HubDispatcher<>), typeof(DefaultHubDispatcher<>));
        services.TryAddScoped(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));
        services.AddAuthorization();

        services.TryAddSingleton(new SignalRActivitySource());

        var builder = new SignalRServerBuilder(services);
        builder.AddJsonProtocol();
        return builder;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "HubContext<,>'s ctor creates a HubClients<,> instance, which generates code dynamically. " +
                "The property that accesses the HubClients<,> is annotated as RequiresDynamicCode on IHubContext<,>, so developers will get a warning when using it.")]
        static void AddTypedHubContext(IServiceCollection services)
        {
            services.TryAddSingleton(typeof(IHubContext<,>), typeof(HubContext<,>));
        }
    }
}
