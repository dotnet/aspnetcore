// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up authorization services in an <see cref="IServiceCollection" />.
/// </summary>
public static class PolicyServiceCollectionExtensions
{
    /// <summary>
    /// Adds authorization services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="AuthorizationBuilder"/> so that additional calls can be chained.</returns>
    public static AuthorizationBuilder AddAuthorizationBuilder(this IServiceCollection services)
        => new AuthorizationBuilder(services.AddAuthorization());

    /// <summary>
    /// Adds the authorization policy evaluator service to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAuthorizationPolicyEvaluator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<AuthorizationPolicyMarkerService>();
        services.TryAddTransient<IPolicyEvaluator, PolicyEvaluator>();
        services.TryAddTransient<IAuthorizationMiddlewareResultHandler, AuthorizationMiddlewareResultHandler>();
        return services;
    }

    /// <summary>
    /// Adds authorization policy services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorizationCore();
        services.AddAuthorizationPolicyEvaluator();
        services.TryAddSingleton<AuthorizationPolicyCache>();
        return services;
    }

    /// <summary>
    /// Adds authorization policy services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configure">An action delegate to configure the provided <see cref="AuthorizationOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAuthorization(this IServiceCollection services, Action<AuthorizationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorizationCore(configure);
        services.AddAuthorizationPolicyEvaluator();
        services.TryAddSingleton<AuthorizationPolicyCache>();
        return services;
    }
}
