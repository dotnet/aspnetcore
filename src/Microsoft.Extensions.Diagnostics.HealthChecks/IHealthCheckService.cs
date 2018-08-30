// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// A service which can be used to check the status of <see cref="IHealthCheck"/> instances 
    /// registered in the application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default implementation of <see cref="IHealthCheckService"/> is registered in the dependency
    /// injection container as a singleton service by calling 
    /// <see cref="DependencyInjection.HealthCheckServiceCollectionExtensions.AddHealthChecks(DependencyInjection.IServiceCollection)"/>.
    /// </para>
    /// <para>
    /// The <see cref="IHealthChecksBuilder"/> returned by 
    /// <see cref="DependencyInjection.HealthCheckServiceCollectionExtensions.AddHealthChecks(DependencyInjection.IServiceCollection)"/>
    /// provides a convenience API for registering health checks.
    /// </para>
    /// <para>
    /// The default implementation of <see cref="IHealthCheckService"/> will use all services
    /// of type <see cref="IHealthCheck"/> registered in the dependency injection container. <see cref="IHealthCheck"/>
    /// implementations may be registered with any service lifetime. The implementation will create a scope
    /// for each aggregate health check operation and use the scope to resolve services. The scope
    /// created for executing health checks is controlled by the health checks service and does not
    /// share scoped services with any other scope in the application.
    /// </para>
    /// </remarks>
    public interface IHealthCheckService
    {
        /// <summary>
        /// Runs all the health checks in the application and returns the aggregated status.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the health checks.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> which will complete when all the health checks have been run,
        /// yielding a <see cref="CompositeHealthCheckResult"/> containing the results.
        /// </returns>
        Task<CompositeHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs the provided health checks and returns the aggregated status
        /// </summary>
        /// <param name="predicate">
        /// A predicate that can be used to include health checks based on user-defined criteria.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the health checks.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> which will complete when all the health checks have been run,
        /// yielding a <see cref="CompositeHealthCheckResult"/> containing the results.
        /// </returns>
        Task<CompositeHealthCheckResult> CheckHealthAsync(Func<IHealthCheck, bool> predicate,
            CancellationToken cancellationToken = default);
    }
}
