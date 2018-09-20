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
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    internal class DefaultHealthCheckService : HealthCheckService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<HealthCheckServiceOptions> _options;
        private readonly ILogger<DefaultHealthCheckService> _logger;

        public DefaultHealthCheckService(
            IServiceScopeFactory scopeFactory,
            IOptions<HealthCheckServiceOptions> options,
            ILogger<DefaultHealthCheckService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // We're specifically going out of our way to do this at startup time. We want to make sure you
            // get any kind of health-check related error as early as possible. Waiting until someone
            // actually tries to **run** health checks would be real baaaaad.
             ValidateRegistrations(_options.Value.Registrations);
        }
        public override async Task<HealthReport> CheckHealthAsync(
            Func<HealthCheckRegistration, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            var registrations = _options.Value.Registrations;

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = new HealthCheckContext();
                var entries = new Dictionary<string, HealthReportEntry>(StringComparer.OrdinalIgnoreCase);

                foreach (var registration in registrations)
                {
                    if (predicate != null && !predicate(registration))
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var healthCheck = registration.Factory(scope.ServiceProvider);

                    // If the health check does things like make Database queries using EF or backend HTTP calls,
                    // it may be valuable to know that logs it generates are part of a health check. So we start a scope.
                    using (_logger.BeginScope(new HealthCheckLogScope(registration.Name)))
                    {
                        var stopwatch = ValueStopwatch.StartNew();
                        context.Registration = registration;

                        Log.HealthCheckBegin(_logger, registration);
                        
                        HealthReportEntry entry;
                        try
                        {
                            var result = await healthCheck.CheckHealthAsync(context, cancellationToken);

                            entry = new HealthReportEntry(
                                result.Result ? HealthStatus.Healthy : registration.FailureStatus,
                                result.Description,
                                result.Exception,
                                result.Data);

                            Log.HealthCheckEnd(_logger, registration, entry, stopwatch.GetElapsedTime());
                        }

                        // Allow cancellation to propagate.
                        catch (Exception ex) when (ex as OperationCanceledException == null)
                        {
                            entry = new HealthReportEntry(HealthStatus.Failed, ex.Message, ex, data: null);
                            Log.HealthCheckError(_logger, registration, ex, stopwatch.GetElapsedTime());
                        }

                        entries[registration.Name] = entry;
                    }
                }

                return new HealthReport(entries);
            }
        }

        private static void ValidateRegistrations(IEnumerable<HealthCheckRegistration> registrations)
        {
            // Scan the list for duplicate names to provide a better error if there are duplicates.
            var duplicateNames = registrations
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                throw new ArgumentException($"Duplicate health checks were registered with the name(s): {string.Join(", ", duplicateNames)}", nameof(registrations));
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

            private static readonly Action<ILogger, string, double, HealthStatus, Exception> _healthCheckEnd = LoggerMessage.Define<string, double, HealthStatus>(
                LogLevel.Debug,
                EventIds.HealthCheckEnd,
                "Health check {HealthCheckName} completed after {ElapsedMilliseconds}ms with status {HealthCheckStatus}");

            private static readonly Action<ILogger, string, double, Exception> _healthCheckError = LoggerMessage.Define<string, double>(
                LogLevel.Error,
                EventIds.HealthCheckError,
                "Health check {HealthCheckName} threw an unhandled exception after {ElapsedMilliseconds}ms");

            public static void HealthCheckBegin(ILogger logger, HealthCheckRegistration registration)
            {
                _healthCheckBegin(logger, registration.Name, null);
            }

            public static void HealthCheckEnd(ILogger logger, HealthCheckRegistration registration, HealthReportEntry entry, TimeSpan duration)
            {
                _healthCheckEnd(logger, registration.Name, duration.TotalMilliseconds, entry.Status, null);
            }

            public static void HealthCheckError(ILogger logger, HealthCheckRegistration registration, Exception exception, TimeSpan duration)
            {
                _healthCheckError(logger, registration.Name, duration.TotalMilliseconds, exception);
            }
        }
    }
}
