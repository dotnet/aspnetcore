// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// Default implementation of <see cref="IHealthCheckService"/>.
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        private readonly ILogger<HealthCheckService> _logger;

        /// <summary>
        /// A <see cref="IReadOnlyDictionary{TKey, T}"/> containing all the health checks registered in the application.
        /// </summary>
        /// <remarks>
        /// The key maps to the <see cref="IHealthCheck.Name"/> property of the health check, and the value is the <see cref="IHealthCheck"/>
        /// instance itself.
        /// </remarks>
        public IReadOnlyDictionary<string, IHealthCheck> Checks { get; }

        /// <summary>
        /// Constructs a <see cref="HealthCheckService"/> from the provided collection of <see cref="IHealthCheck"/> instances.
        /// </summary>
        /// <param name="healthChecks">The <see cref="IHealthCheck"/> instances that have been registered in the application.</param>
        public HealthCheckService(IEnumerable<IHealthCheck> healthChecks) : this(healthChecks, NullLogger<HealthCheckService>.Instance) { }

        /// <summary>
        /// Constructs a <see cref="HealthCheckService"/> from the provided collection of <see cref="IHealthCheck"/> instances, and the provided logger.
        /// </summary>
        /// <param name="healthChecks">The <see cref="IHealthCheck"/> instances that have been registered in the application.</param>
        /// <param name="logger">A <see cref="ILogger{T}"/> that can be used to log events that occur during health check operations.</param>
        public HealthCheckService(IEnumerable<IHealthCheck> healthChecks, ILogger<HealthCheckService> logger)
        {
            healthChecks = healthChecks ?? throw new ArgumentNullException(nameof(logger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Scan the list for duplicate names to provide a better error if there are duplicates.
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicates = new List<string>();
            foreach (var check in healthChecks)
            {
                if (!names.Add(check.Name))
                {
                    duplicates.Add(check.Name);
                }
            }

            if (duplicates.Count > 0)
            {
                throw new ArgumentException($"Duplicate health checks were registered with the name(s): {string.Join(", ", duplicates)}", nameof(healthChecks));
            }

            Checks = healthChecks.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var check in Checks)
                {
                    _logger.LogDebug("Health check '{healthCheckName}' has been registered", check.Key);
                }
            }
        }

        /// <summary>
        /// Runs all the health checks in the application and returns the aggregated status.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the health checks.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> which will complete when all the health checks have been run,
        /// yielding a <see cref="CompositeHealthCheckResult"/> containing the results.
        /// </returns>
        public Task<CompositeHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default) =>
            CheckHealthAsync(Checks.Values, cancellationToken);
        
        /// <summary>
        /// Runs the provided health checks and returns the aggregated status
        /// </summary>
        /// <param name="checks">The <see cref="IHealthCheck"/> instances to be run.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the health checks.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> which will complete when all the health checks have been run,
        /// yielding a <see cref="CompositeHealthCheckResult"/> containing the results.
        /// </returns>
        public async Task<CompositeHealthCheckResult> CheckHealthAsync(IEnumerable<IHealthCheck> checks, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, HealthCheckResult>(Checks.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var check in checks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // If the health check does things like make Database queries using EF or backend HTTP calls,
                // it may be valuable to know that logs it generates are part of a health check. So we start a scope.
                using (_logger.BeginScope(new HealthCheckLogScope(check.Name)))
                {
                    HealthCheckResult result;
                    try
                    {
                        _logger.LogTrace("Running health check: {healthCheckName}", check.Name);
                        result = await check.CheckHealthAsync(cancellationToken);
                        _logger.LogTrace("Health check '{healthCheckName}' completed with status '{healthCheckStatus}'", check.Name, result.Status);
                    }
                    catch (Exception ex)
                    {
                        // We don't log this as an error because a health check failing shouldn't bring down the active task.
                        _logger.LogError(ex, "Health check '{healthCheckName}' threw an unexpected exception", check.Name);
                        result = new HealthCheckResult(HealthCheckStatus.Failed, ex, ex.Message, data: null);
                    }

                    // This can only happen if the result is default(HealthCheckResult)
                    if (result.Status == HealthCheckStatus.Unknown)
                    {
                        // This is different from the case above. We throw here because a health check is doing something specifically incorrect.
                        var exception = new InvalidOperationException($"Health check '{check.Name}' returned a result with a status of Unknown");
                        _logger.LogError(exception, "Health check '{healthCheckName}' returned a result with a status of Unknown", check.Name);
                        throw exception;
                    }

                    results[check.Name] = result;
                }
            }
            return new CompositeHealthCheckResult(results);
        }
    }
}
