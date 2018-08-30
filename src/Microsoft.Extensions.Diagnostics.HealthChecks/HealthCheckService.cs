// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    internal class HealthCheckService : IHealthCheckService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HealthCheckService> _logger;

        public HealthCheckService(IServiceScopeFactory scopeFactory, ILogger<HealthCheckService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // We're specifically going out of our way to do this at startup time. We want to make sure you
            // get any kind of health-check related error as early as possible. Waiting until someone
            // actually tries to **run** health checks would be real baaaaad.
            using (var scope = _scopeFactory.CreateScope())
            {
                var healthChecks = scope.ServiceProvider.GetRequiredService<IEnumerable<IHealthCheck>>();
                EnsureNoDuplicates(healthChecks);
            }
        }

        public Task<CompositeHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default) =>
            CheckHealthAsync(predicate: null, cancellationToken);

        public async Task<CompositeHealthCheckResult> CheckHealthAsync(
            Func<IHealthCheck, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var healthChecks = scope.ServiceProvider.GetRequiredService<IEnumerable<IHealthCheck>>();

                var results = new Dictionary<string, HealthCheckResult>(StringComparer.OrdinalIgnoreCase);
                foreach (var healthCheck in healthChecks)
                {
                    if (predicate != null && !predicate(healthCheck))
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // If the health check does things like make Database queries using EF or backend HTTP calls,
                    // it may be valuable to know that logs it generates are part of a health check. So we start a scope.
                    using (_logger.BeginScope(new HealthCheckLogScope(healthCheck.Name)))
                    {
                        HealthCheckResult result;
                        try
                        {
                            Log.HealthCheckBegin(_logger, healthCheck);
                            var stopwatch = ValueStopwatch.StartNew();
                            result = await healthCheck.CheckHealthAsync(cancellationToken);
                            Log.HealthCheckEnd(_logger, healthCheck, result, stopwatch.GetElapsedTime());
                        }
                        catch (Exception ex)
                        {
                            Log.HealthCheckError(_logger, healthCheck, ex);
                            result = new HealthCheckResult(HealthCheckStatus.Failed, ex, ex.Message, data: null);
                        }

                        // This can only happen if the result is default(HealthCheckResult)
                        if (result.Status == HealthCheckStatus.Unknown)
                        {
                            // This is different from the case above. We throw here because a health check is doing something specifically incorrect.
                            throw new InvalidOperationException($"Health check '{healthCheck.Name}' returned a result with a status of Unknown");
                        }

                        results[healthCheck.Name] = result;
                    }
                }

                return new CompositeHealthCheckResult(results);
            }
        }

        private static void EnsureNoDuplicates(IEnumerable<IHealthCheck> healthChecks)
        {
            // Scan the list for duplicate names to provide a better error if there are duplicates.
            var duplicateNames = healthChecks
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                throw new ArgumentException($"Duplicate health checks were registered with the name(s): {string.Join(", ", duplicateNames)}", nameof(healthChecks));
            }
        }

        private static class Log
        {
            public static class EventIds
            {
                public static readonly EventId HealthCheckBegin = new EventId(100, "HealthCheckBegin");
                public static readonly EventId HealthCheckEnd = new EventId(101, "HealthCheckEnd");
                public static readonly EventId HealthCheckError = new EventId(102, "HealthCheckError");
            }

            private static readonly Action<ILogger, string, Exception> _healthCheckBegin = LoggerMessage.Define<string>(
                LogLevel.Debug,
                EventIds.HealthCheckBegin,
                "Running health check {HealthCheckName}");

            private static readonly Action<ILogger, string, double, HealthCheckStatus, Exception> _healthCheckEnd = LoggerMessage.Define<string, double, HealthCheckStatus>(
                LogLevel.Debug,
                EventIds.HealthCheckEnd,
                "Health check {HealthCheckName} completed after {ElapsedMilliseconds}ms with status {HealthCheckStatus}");

            private static readonly Action<ILogger, string, Exception> _healthCheckError = LoggerMessage.Define<string>(
                LogLevel.Error,
                EventIds.HealthCheckError,
                "Health check {HealthCheckName} threw an unhandled exception");

            public static void HealthCheckBegin(ILogger logger, IHealthCheck healthCheck)
            {
                _healthCheckBegin(logger, healthCheck.Name, null);
            }

            public static void HealthCheckEnd(ILogger logger, IHealthCheck healthCheck, HealthCheckResult result, TimeSpan duration)
            {
                _healthCheckEnd(logger, healthCheck.Name, duration.TotalMilliseconds, result.Status, null);
            }

            public static void HealthCheckError(ILogger logger, IHealthCheck healthCheck, Exception exception)
            {
                _healthCheckError(logger, healthCheck.Name, exception);
            }
        }
    }
}
