// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (predicate != null)
            {
                registrations = registrations.Where(predicate).ToArray();
            }

            var totalTime = ValueStopwatch.StartNew();
            Log.HealthCheckProcessingBegin(_logger);

            var tasks = new Task<HealthReportEntry>[registrations.Count];
            var index = 0;
            using (var scope = _scopeFactory.CreateScope())
            {
                foreach (var registration in registrations)
                {
                    tasks[index++] = Task.Run(() => RunCheckAsync(scope, registration, cancellationToken), cancellationToken);
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            index = 0;
            var entries = new Dictionary<string, HealthReportEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var registration in registrations)
            {
                entries[registration.Name] = tasks[index++].Result;
            }

            var totalElapsedTime = totalTime.GetElapsedTime();
            var report = new HealthReport(entries, totalElapsedTime);
            Log.HealthCheckProcessingEnd(_logger, report.Status, totalElapsedTime);
            return report;
        }

        private async Task<HealthReportEntry> RunCheckAsync(IServiceScope scope, HealthCheckRegistration registration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var healthCheck = registration.Factory(scope.ServiceProvider);

            // If the health check does things like make Database queries using EF or backend HTTP calls,
            // it may be valuable to know that logs it generates are part of a health check. So we start a scope.
            using (_logger.BeginScope(new HealthCheckLogScope(registration.Name)))
            {
                var stopwatch = ValueStopwatch.StartNew();
                var context = new HealthCheckContext { Registration = registration };

                Log.HealthCheckBegin(_logger, registration);

                HealthReportEntry entry;
                CancellationTokenSource timeoutCancellationTokenSource = null;
                try
                {
                    HealthCheckResult result;

                    var checkCancellationToken = cancellationToken;
                    if (registration.Timeout > TimeSpan.Zero)
                    {
                        timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCancellationTokenSource.CancelAfter(registration.Timeout);
                        checkCancellationToken = timeoutCancellationTokenSource.Token;
                    }

                    result = await healthCheck.CheckHealthAsync(context, checkCancellationToken).ConfigureAwait(false);

                    var duration = stopwatch.GetElapsedTime();

                    entry = new HealthReportEntry(
                        status: result.Status,
                        description: result.Description,
                        duration: duration,
                        exception: result.Exception,
                        data: result.Data,
                        tags: registration.Tags);

                    Log.HealthCheckEnd(_logger, registration, entry, duration);
                    Log.HealthCheckData(_logger, registration, entry);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    var duration = stopwatch.GetElapsedTime();
                    entry = new HealthReportEntry(
                        status: HealthStatus.Unhealthy,
                        description: "A timeout occurred while running check.",
                        duration: duration,
                        exception: ex,
                        data: null);

                    Log.HealthCheckError(_logger, registration, ex, duration);
                }

                // Allow cancellation to propagate if it's not a timeout.
                catch (Exception ex) when (ex as OperationCanceledException == null)
                {
                    var duration = stopwatch.GetElapsedTime();
                    entry = new HealthReportEntry(
                        status: HealthStatus.Unhealthy,
                        description: ex.Message,
                        duration: duration,
                        exception: ex,
                        data: null);

                    Log.HealthCheckError(_logger, registration, ex, duration);
                }

                finally
                {
                    timeoutCancellationTokenSource?.Dispose();
                }

                return entry;
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

        internal static class EventIds
        {
            public static readonly EventId HealthCheckProcessingBegin = new EventId(100, "HealthCheckProcessingBegin");
            public static readonly EventId HealthCheckProcessingEnd = new EventId(101, "HealthCheckProcessingEnd");

            public static readonly EventId HealthCheckBegin = new EventId(102, "HealthCheckBegin");
            public static readonly EventId HealthCheckEnd = new EventId(103, "HealthCheckEnd");
            public static readonly EventId HealthCheckError = new EventId(104, "HealthCheckError");
            public static readonly EventId HealthCheckData = new EventId(105, "HealthCheckData");
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _healthCheckProcessingBegin = LoggerMessage.Define(
                LogLevel.Debug,
                EventIds.HealthCheckProcessingBegin,
                "Running health checks");

            private static readonly Action<ILogger, double, HealthStatus, Exception> _healthCheckProcessingEnd = LoggerMessage.Define<double, HealthStatus>(
                LogLevel.Debug,
                EventIds.HealthCheckProcessingEnd,
                "Health check processing completed after {ElapsedMilliseconds}ms with combined status {HealthStatus}");

            private static readonly Action<ILogger, string, Exception> _healthCheckBegin = LoggerMessage.Define<string>(
                LogLevel.Debug,
                EventIds.HealthCheckBegin,
                "Running health check {HealthCheckName}");

            // These are separate so they can have different log levels
            private static readonly string HealthCheckEndText = "Health check {HealthCheckName} completed after {ElapsedMilliseconds}ms with status {HealthStatus} and '{HealthCheckDescription}'";

            private static readonly Action<ILogger, string, double, HealthStatus, string, Exception> _healthCheckEndHealthy = LoggerMessage.Define<string, double, HealthStatus, string>(
                LogLevel.Debug,
                EventIds.HealthCheckEnd,
                HealthCheckEndText);

            private static readonly Action<ILogger, string, double, HealthStatus, string, Exception> _healthCheckEndDegraded = LoggerMessage.Define<string, double, HealthStatus, string>(
                LogLevel.Warning,
                EventIds.HealthCheckEnd,
                HealthCheckEndText);

            private static readonly Action<ILogger, string, double, HealthStatus, string, Exception> _healthCheckEndUnhealthy = LoggerMessage.Define<string, double, HealthStatus, string>(
                LogLevel.Error,
                EventIds.HealthCheckEnd,
                HealthCheckEndText);

            private static readonly Action<ILogger, string, double, HealthStatus, string, Exception> _healthCheckEndFailed = LoggerMessage.Define<string, double, HealthStatus, string>(
                LogLevel.Error,
                EventIds.HealthCheckEnd,
                HealthCheckEndText);

            private static readonly Action<ILogger, string, double, Exception> _healthCheckError = LoggerMessage.Define<string, double>(
                LogLevel.Error,
                EventIds.HealthCheckError,
                "Health check {HealthCheckName} threw an unhandled exception after {ElapsedMilliseconds}ms");

            public static void HealthCheckProcessingBegin(ILogger logger)
            {
                _healthCheckProcessingBegin(logger, null);
            }

            public static void HealthCheckProcessingEnd(ILogger logger, HealthStatus status, TimeSpan duration)
            {
                _healthCheckProcessingEnd(logger, duration.TotalMilliseconds, status, null);
            }

            public static void HealthCheckBegin(ILogger logger, HealthCheckRegistration registration)
            {
                _healthCheckBegin(logger, registration.Name, null);
            }

            public static void HealthCheckEnd(ILogger logger, HealthCheckRegistration registration, HealthReportEntry entry, TimeSpan duration)
            {
                switch (entry.Status)
                {
                    case HealthStatus.Healthy:
                        _healthCheckEndHealthy(logger, registration.Name, duration.TotalMilliseconds, entry.Status, entry.Description, null);
                        break;

                    case HealthStatus.Degraded:
                        _healthCheckEndDegraded(logger, registration.Name, duration.TotalMilliseconds, entry.Status, entry.Description, null);
                        break;

                    case HealthStatus.Unhealthy:
                        _healthCheckEndUnhealthy(logger, registration.Name, duration.TotalMilliseconds, entry.Status, entry.Description, null);
                        break;
                }
            }

            public static void HealthCheckError(ILogger logger, HealthCheckRegistration registration, Exception exception, TimeSpan duration)
            {
                _healthCheckError(logger, registration.Name, duration.TotalMilliseconds, exception);
            }

            public static void HealthCheckData(ILogger logger, HealthCheckRegistration registration, HealthReportEntry entry)
            {
                if (entry.Data.Count > 0 && logger.IsEnabled(LogLevel.Debug))
                {
                    logger.Log(
                        LogLevel.Debug,
                        EventIds.HealthCheckData,
                        new HealthCheckDataLogValue(registration.Name, entry.Data),
                        null,
                        (state, ex) => state.ToString());
                }
            }
        }

        internal class HealthCheckDataLogValue : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly string _name;
            private readonly List<KeyValuePair<string, object>> _values;

            private string _formatted;

            public HealthCheckDataLogValue(string name, IReadOnlyDictionary<string, object> values)
            {
                _name = name;
                _values = values.ToList();

                // We add the name as a kvp so that you can filter by health check name in the logs.
                // This is the same parameter name used in the other logs.
                _values.Add(new KeyValuePair<string, object>("HealthCheckName", name));
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index < 0 || index >= Count)
                    {
                        throw new IndexOutOfRangeException(nameof(index));
                    }

                    return _values[index];
                }
            }

            public int Count => _values.Count;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _values.GetEnumerator();
            }

            public override string ToString()
            {
                if (_formatted == null)
                {
                    var builder = new StringBuilder();
                    builder.AppendLine($"Health check data for {_name}:");

                    var values = _values;
                    for (var i = 0; i < values.Count; i++)
                    {
                        var kvp = values[i];
                        builder.Append("    ");
                        builder.Append(kvp.Key);
                        builder.Append(": ");

                        builder.AppendLine(kvp.Value?.ToString());
                    }

                    _formatted = builder.ToString();
                }

                return _formatted;
            }
        }
    }
}
