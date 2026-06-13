// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering <see cref="HealthCheckService"/> in an <see cref="IServiceCollection"/>.
/// </summary>
public static class HealthCheckServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="HealthCheckService"/> to the container, using the provided delegate to register
    /// health checks.
    /// </summary>
    /// <remarks>
    /// This operation is idempotent - multiple invocations will still only result in a single
    /// <see cref="HealthCheckService"/> instance in the <see cref="IServiceCollection"/>. It can be invoked
    /// multiple times in order to get access to the <see cref="IHealthChecksBuilder"/> in multiple places.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="HealthCheckService"/> to.</param>
    /// <returns>An instance of <see cref="IHealthChecksBuilder"/> from which health checks can be registered.</returns>
    public static IHealthChecksBuilder AddHealthChecks(this IServiceCollection services)
    {
        services.TryAddSingleton<HealthCheckService, DefaultHealthCheckService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, HealthCheckPublisherHostedService>());
        return new HealthChecksBuilder(services);
    }

    /// <summary>
    /// Adds the <see cref="HealthCheckService"/> to the container and configures the
    /// <see cref="HealthCheckServiceOptions"/> used by the registered service.
    /// </summary>
    /// <remarks>
    /// This operation is idempotent - multiple invocations will still only result in a single
    /// <see cref="HealthCheckService"/> instance in the <see cref="IServiceCollection"/>. The
    /// <paramref name="configureOptions"/> delegate is invoked every time the options instance is
    /// constructed, allowing the method to be called repeatedly to compose registrations.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="HealthCheckService"/> to.</param>
    /// <param name="configureOptions">A delegate that configures the <see cref="HealthCheckServiceOptions"/>.</param>
    /// <returns>An instance of <see cref="IHealthChecksBuilder"/> from which health checks can be registered.</returns>
    public static IHealthChecksBuilder AddHealthChecks(this IServiceCollection services, Action<HealthCheckServiceOptions> configureOptions)
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);
        ArgumentNullThrowHelper.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        return services.AddHealthChecks();
    }
}
