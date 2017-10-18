// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// A service which can be used to check the status of <see cref="IHealthCheck"/> instances registered in the application.
    /// </summary>
    public interface IHealthCheckService
    {
        /// <summary>
        /// A <see cref="IReadOnlyDictionary{TKey, T}"/> containing all the health checks registered in the application.
        /// </summary>
        /// <remarks>
        /// The key maps to the <see cref="IHealthCheck.Name"/> property of the health check, and the value is the <see cref="IHealthCheck"/>
        /// instance itself.
        /// </remarks>
        IReadOnlyDictionary<string, IHealthCheck> Checks { get; }

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
        /// <param name="checks">The <see cref="IHealthCheck"/> instances to be run.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the health checks.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> which will complete when all the health checks have been run,
        /// yielding a <see cref="CompositeHealthCheckResult"/> containing the results.
        /// </returns>
        Task<CompositeHealthCheckResult> CheckHealthAsync(IEnumerable<IHealthCheck> checks,
            CancellationToken cancellationToken = default);
    }
}
