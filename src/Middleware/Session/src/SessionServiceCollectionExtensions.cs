// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding session services to the DI container.
/// </summary>
public static class SessionServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required for application session state.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    [RequiresUnreferencedCode("Session State middleware does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IServiceCollection AddSession(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<ISessionStore, DistributedSessionStore>();
        services.AddDataProtection();
        return services;
    }

    /// <summary>
    /// Adds services required for application session state.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">The session options to configure the middleware with.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    [RequiresUnreferencedCode("Session State middleware does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IServiceCollection AddSession(this IServiceCollection services, Action<SessionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddSession();

        return services;
    }
}
