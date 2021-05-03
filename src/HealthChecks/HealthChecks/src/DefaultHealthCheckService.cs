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
    internal partial class DefaultHealthCheckService : HealthCheckService
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
            Func<HealthCheckRegistration, bool>? predicate,
            CancellationToken cancellationToken = default)
        {
            var registrations = _options.Value.Registrations;
            if (predicate != null)
            {
                registrations = registrations.Where(predicate).ToArray();
            }

            var totalTime = ValueStopwatch.StartNew();
            HealthCheckProcessingBegin();

            var tasks = new Task<HealthReportEntry>[registrations.Count];
            var index = 0;

            foreach (var registration in registrations)
            {
                tasks[index++] = Task.Run(() => RunCheckAsync(registration, cancellationToken), cancellationToken);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);


            index = 0;
            var entries = new Dictionary<string, HealthReportEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var registration in registrations)
            {
                entries[registration.Name] = tasks[index++].Result;
            }

            var totalElapsedTime = totalTime.GetElapsedTime();
            var report = new HealthReport(entries, totalElapsedTime);
            HealthCheckProcessingEnd(report.Status, totalElapsedTime);
            return report;
        }

        private async Task<HealthReportEntry> RunCheckAsync(HealthCheckRegistration registration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var scope = _scopeFactory.CreateScope())
            {
                var healthCheck = registration.Factory(scope.ServiceProvider);

                // If the health check does things like make Database queries using EF or backend HTTP calls,
                // it may be valuable to know that logs it generates are part of a health check. So we start a scope.
                using (_logger.BeginScope(new HealthCheckLogScope(registration.Name)))
                {
                    var stopwatch = ValueStopwatch.StartNew();
                    var context = new HealthCheckContext { Registration = registration };

                    HealthCheckBegin(registration.Name);

                    HealthReportEntry entry;
                    CancellationTokenSource? timeoutCancellationTokenSource = null;
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

                        HealthCheckEnd(registration, entry, duration);
                        HealthCheckData(registration, entry);
                    }
                    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        var duration = stopwatch.GetElapsedTime();
                        entry = new HealthReportEntry(
                            status: registration.FailureStatus,
                            description: "A timeout occurred while running check.",
                            duration: duration,
                            exception: ex,
                            data: null,
                            tags: registration.Tags);

                        HealthCheckError(registration, ex, duration);
                    }

                    // Allow cancellation to propagate if it's not a timeout.
                    catch (Exception ex) when (ex as OperationCanceledException == null)
                    {
                        var duration = stopwatch.GetElapsedTime();
                        entry = new HealthReportEntry(
                            status: registration.FailureStatus,
                            description: ex.Message,
                            duration: duration,
                            exception: ex,
                            data: null,
                            tags: registration.Tags);

                        HealthCheckError(registration, ex, duration);
                    }

                    finally
                    {
                        timeoutCancellationTokenSource?.Dispose();
                    }

                    return entry;
                }
            }
        }

        private static void ValidateRegistrations(IEnumerable<HealthCheckRegistration> registrations)
        {
            // Scan the list for duplicate names to provide a better error if there are duplicates.

            StringBuilder? builder = null;
            var distinctRegistrations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var registration in registrations)
            {
                if (!distinctRegistrations.Add(registration.Name))
                {
                    builder ??= new StringBuilder("Duplicate health checks were registered with the name(s): ");

                    builder.Append(registration.Name).Append(", ");
                }
            }

            if (builder is not null)
            {
                throw new ArgumentException(builder.ToString(0, builder.Length - 2), nameof(registrations));
            }
        }

        internal static class EventIds
        {
            public const int HealthCheckProcessingBeginId = 100;
            public const int HealthCheckProcessingEndId = 101;
            public const int HealthCheckBeginId = 102;
            public const int HealthCheckEndId = 103;
            public const int HealthCheckErrorId = 104;
            public const int HealthCheckDataId = 105;

            public static readonly EventId HealthCheckProcessingBegin = new EventId(HealthCheckProcessingBeginId, nameof(HealthCheckProcessingBegin));
            public static readonly EventId HealthCheckProcessingEnd = new EventId(HealthCheckProcessingEndId, nameof(HealthCheckProcessingEnd));

            public static readonly EventId HealthCheckBegin = new EventId(HealthCheckBeginId, nameof(HealthCheckBegin));
            public static readonly EventId HealthCheckEnd = new EventId(HealthCheckEndId, nameof(HealthCheckEnd));
            public static readonly EventId HealthCheckError = new EventId(HealthCheckErrorId, nameof(HealthCheckError));
            public static readonly EventId HealthCheckData = new EventId(HealthCheckDataId, nameof(HealthCheckData));
        }

        [LoggerMessage(EventId = EventIds.HealthCheckProcessingBeginId, Level = LogLevel.Debug, Message = "Running health checks")]
        private partial void HealthCheckProcessingBegin();

        private void HealthCheckProcessingEnd(HealthStatus status, TimeSpan duration) =>
            HealthCheckProcessingEnd(status, duration.TotalMilliseconds);

        [LoggerMessage(EventId = EventIds.HealthCheckProcessingEndId, Level = LogLevel.Debug, Message = "Health check processing with combined status {HealthStatus} completed after {ElapsedMilliseconds}ms")]
        private partial void HealthCheckProcessingEnd(HealthStatus HealthStatus, double ElapsedMilliseconds);

        [LoggerMessage(EventId = EventIds.HealthCheckBeginId, Level = LogLevel.Debug, Message = "Running health check {HealthCheckName}")]
        private partial void HealthCheckBegin(string HealthCheckName);

        // These are separate so they can have different log levels
        private const string HealthCheckEndText = "Health check {HealthCheckName} with status {HealthStatus} completed after {ElapsedMilliseconds}ms with message '{HealthCheckDescription}'";

#pragma warning disable SYSLIB1006
        [LoggerMessage(EventId = EventIds.HealthCheckEndId, Level = LogLevel.Debug, Message = HealthCheckEndText)]
        private partial void HealthCheckEndHealthy(string HealthCheckName, HealthStatus HealthStatus, double ElapsedMilliseconds, string? HealthCheckDescription);

        [LoggerMessage(EventId = EventIds.HealthCheckEndId, Level = LogLevel.Warning, Message = HealthCheckEndText)]
        private partial void HealthCheckEndDegraded(string HealthCheckName, HealthStatus HealthStatus, double ElapsedMilliseconds, string? HealthCheckDescription);

        [LoggerMessage(EventId = EventIds.HealthCheckEndId, Level = LogLevel.Error, Message = HealthCheckEndText)]
        private partial void HealthCheckEndUnhealthy(string HealthCheckName, HealthStatus HealthStatus, double ElapsedMilliseconds, string? HealthCheckDescription, Exception? exception);
#pragma warning restore SYSLIB1006

        private void HealthCheckEnd(HealthCheckRegistration registration, HealthReportEntry entry, TimeSpan duration)
        {
            switch (entry.Status)
            {
                case HealthStatus.Healthy:
                    HealthCheckEndHealthy(registration.Name, entry.Status, duration.TotalMilliseconds, entry.Description);
                    break;

                case HealthStatus.Degraded:
                    HealthCheckEndDegraded(registration.Name, entry.Status, duration.TotalMilliseconds, entry.Description);
                    break;

                case HealthStatus.Unhealthy:
                    HealthCheckEndUnhealthy(registration.Name, entry.Status, duration.TotalMilliseconds, entry.Description, entry.Exception);
                    break;
            }
        }

        [LoggerMessage(EventId = EventIds.HealthCheckErrorId, Level = LogLevel.Error, Message = "Health check {HealthCheckName} threw an unhandled exception after {ElapsedMilliseconds}ms")]
        private partial void HealthCheckError(string HealthCheckName, double ElapsedMilliseconds, Exception exception);

        private void HealthCheckError(HealthCheckRegistration registration, Exception exception, TimeSpan duration) =>
            HealthCheckError(registration.Name, duration.TotalMilliseconds, exception);

        private void HealthCheckData(HealthCheckRegistration registration, HealthReportEntry entry)
        {
            if (entry.Data.Count > 0 && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.Log(
                    LogLevel.Debug,
                    EventIds.HealthCheckData,
                    new HealthCheckDataLogValue(registration.Name, entry.Data),
                    null,
                    (state, ex) => state.ToString());
            }
        }

        internal class HealthCheckDataLogValue : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly string _name;
            private readonly List<KeyValuePair<string, object>> _values;

            private string? _formatted;

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
