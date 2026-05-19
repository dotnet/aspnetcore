// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// A service which can be used to check the status of <see cref="IHealthCheck"/> instances
/// registered in the application.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation of <see cref="HealthCheckService"/> is registered in the dependency
/// injection container as a singleton service by calling
/// <see cref="HealthCheckServiceCollectionExtensions.AddHealthChecks(IServiceCollection)"/>.
/// </para>
/// <para>
/// The <see cref="IHealthChecksBuilder"/> returned by
/// <see cref="HealthCheckServiceCollectionExtensions.AddHealthChecks(IServiceCollection)"/>
/// provides a convenience API for registering health checks.
/// </para>
/// <para>
/// <see cref="IHealthCheck"/> implementations can be registered through extension methods provided by
/// <see cref="IHealthChecksBuilder"/>.
/// </para>
/// </remarks>
public abstract class HealthCheckService
{
    /// <summary>
    /// Runs all the health checks in the application and returns the aggregated status.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the health checks.</param>
    /// <returns>
    /// A <see cref="Task{T}"/> which will complete when all the health checks have been run,
    /// yielding a <see cref="HealthReport"/> containing the results.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return CheckHealthAsync(predicate: null, cancellationToken);
    }

    /// <summary>
    /// Runs the provided health checks and returns the aggregated status
    /// </summary>
    /// <param name="predicate">
    /// A predicate that can be used to include health checks based on user-defined criteria.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the health checks.</param>
    /// <returns>
    /// A <see cref="Task{T}"/> which will complete when all the health checks have been run,
    /// yielding a <see cref="HealthReport"/> containing the results.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public abstract Task<HealthReport> CheckHealthAsync(
        Func<HealthCheckRegistration, bool>? predicate,
        CancellationToken cancellationToken = default);
}
