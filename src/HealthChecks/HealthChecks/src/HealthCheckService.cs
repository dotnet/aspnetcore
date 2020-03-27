// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
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
        public abstract Task<HealthReport> CheckHealthAsync(
            Func<HealthCheckRegistration, bool> predicate,
            CancellationToken cancellationToken = default);
    }
}
