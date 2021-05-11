// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// A simple implementation of <see cref="IHealthCheck"/> which uses a provided delegate to
    /// implement the check.
    /// </summary>
    internal sealed class DelegateHealthCheck : IHealthCheck
    {
        private readonly Func<CancellationToken, Task<HealthCheckResult>> _check;

        /// <summary>
        /// Create an instance of <see cref="DelegateHealthCheck"/> from the specified delegate.
        /// </summary>
        /// <param name="check">A delegate which provides the code to execute when the health check is run.</param>
        public DelegateHealthCheck(Func<CancellationToken, Task<HealthCheckResult>> check)
        {
            _check = check ?? throw new ArgumentNullException(nameof(check));
        }

        /// <summary>
        /// Runs the health check, returning the status of the component being checked.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task{HealthCheckResult}"/> that completes when the health check has finished, yielding the status of the component being checked.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => _check(cancellationToken);
    }
}
