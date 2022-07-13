// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed partial class HealthCheckPublisherHostedService : IHostedService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IOptions<HealthCheckPublisherOptions> _healthCheckPublisherOptions;
    private readonly IOptions<HealthCheckServiceOptions> _healthCheckServiceOptions;
    private readonly ILogger _logger;
    private readonly IHealthCheckPublisher[] _publishers;
    private readonly IDictionary<string, HealthCheckRegistration> _healthCheckRegistrationsDictionary;
    private readonly Queue<HealthReport> _healthReportQueue;

    private readonly CancellationTokenSource _stopping;
    private Timer? _healthCheckPublisherTimer;
    private ICollection<Timer> _healthCheckRegistrationTimers;
    private CancellationTokenSource? _runTokenSource;

    public HealthCheckPublisherHostedService(
        HealthCheckService healthCheckService,
        IOptions<HealthCheckPublisherOptions> healthCheckPublisherOptions,
        IOptions<HealthCheckServiceOptions> healthCheckServiceOptions,
        ILogger<HealthCheckPublisherHostedService> logger,
        IEnumerable<IHealthCheckPublisher> publishers)
    {
        if (healthCheckService == null)
        {
            throw new ArgumentNullException(nameof(healthCheckService));
        }

        if (healthCheckPublisherOptions == null)
        {
            throw new ArgumentNullException(nameof(healthCheckPublisherOptions));
        }

        if (healthCheckServiceOptions == null)
        {
            throw new ArgumentNullException(nameof(healthCheckServiceOptions));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (publishers == null)
        {
            throw new ArgumentNullException(nameof(publishers));
        }

        _healthCheckService = healthCheckService;
        _healthCheckPublisherOptions = healthCheckPublisherOptions;
        _healthCheckServiceOptions = healthCheckServiceOptions;
        _logger = logger;
        _publishers = publishers.ToArray();

        _healthCheckRegistrationsDictionary =
            healthCheckServiceOptions.Value.Registrations.ToDictionary(r => r.Name, r =>
            {
                // For healthchecks with no individual period, default to publisher period
                if (r.Period == Timeout.InfiniteTimeSpan)
                {
                    r.Period = _healthCheckPublisherOptions.Value.Period;
                }

                return r;
            });

        _healthReportQueue = new Queue<HealthReport>();

        _stopping = new CancellationTokenSource();
    }

    internal bool IsStopping => _stopping.IsCancellationRequested;

    internal bool IsTimerRunning => _healthCheckPublisherTimer != null;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_publishers.Length == 0)
        {
            return Task.CompletedTask;
        }

        _healthCheckRegistrationTimers =
            _healthCheckRegistrationsDictionary
                .Select(r =>
                    NonCapturingTimer.Create(
                        async (state) => await CheckHealthAsync(r.Value.Name).ConfigureAwait(false),
                        null,
                        dueTime: _healthCheckPublisherOptions.Value.Delay,
                        period: r.Value.Period)).ToList();

        // IMPORTANT - make sure this is the last thing that happens in this method. The timer can
        // fire before other code runs.
        _healthCheckPublisherTimer = NonCapturingTimer.Create(Timer_Tick, null, dueTime: _healthCheckPublisherOptions.Value.Delay, period: _healthCheckPublisherOptions.Value.Period);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _stopping.Cancel();
        }
        catch
        {
            // Ignore exceptions thrown as a result of a cancellation.
        }

        if (_publishers.Length == 0)
        {
            return Task.CompletedTask;
        }

        _healthCheckPublisherTimer?.Dispose();
        _healthCheckPublisherTimer = null;

        foreach (var timer in _healthCheckRegistrationTimers)
        {
            timer.Dispose();
        }
        _healthCheckRegistrationTimers = default;

        return Task.CompletedTask;
    }

    // Yes, async void. We need to be async. We need to be void. We handle the exceptions in RunAsync
    private async void Timer_Tick(object? state)
    {
        await RunAsync().ConfigureAwait(false);
    }

    // Internal for testing
    internal void CancelToken()
    {
        _runTokenSource!.Cancel();
    }

    private async Task CheckHealthAsync(string healthCheckName)
    {
        // Concatenate with exposed predicate
        var runHealthCheckPredicate = (HealthCheckRegistration hcr) =>
        {
            var runHealthCheck = hcr.Name == healthCheckName;

            if (_healthCheckPublisherOptions.Value.Predicate == null)
            {
                return runHealthCheck;
            }

            return _healthCheckPublisherOptions.Value.Predicate(hcr)
                && runHealthCheck;
        };

        // The health checks service does it's own logging, and doesn't throw exceptions.
        var report = await _healthCheckService.CheckHealthAsync(runHealthCheckPredicate).ConfigureAwait(false);

        _healthReportQueue.Enqueue(report);
    }

    // Internal for testing
    internal async Task RunAsync()
    {
        var duration = ValueStopwatch.StartNew();
        Logger.HealthCheckPublisherProcessingBegin(_logger);

        CancellationTokenSource? cancellation = null;
        try
        {
            var timeout = _healthCheckPublisherOptions.Value.Timeout;

            cancellation = CancellationTokenSource.CreateLinkedTokenSource(_stopping.Token);
            _runTokenSource = cancellation;
            cancellation.CancelAfter(timeout);

            await RunAsyncCore(cancellation.Token).ConfigureAwait(false);

            Logger.HealthCheckPublisherProcessingEnd(_logger, duration.GetElapsedTime());
        }
        catch (OperationCanceledException) when (IsStopping)
        {
            // This is a cancellation - if the app is shutting down we want to ignore it. Otherwise, it's
            // a timeout and we want to log it.
        }
        catch (Exception ex)
        {
            // This is an error, publishing failed.
            Logger.HealthCheckPublisherProcessingEnd(_logger, duration.GetElapsedTime(), ex);
        }
        finally
        {
            cancellation?.Dispose();
        }
    }

    private async Task RunAsyncCore(CancellationToken cancellationToken)
    {
        // Forcibly yield - we want to unblock the timer thread.
        await Task.Yield();

        // The health checks service does it's own logging, and doesn't throw exceptions.
        var publishers = _publishers;
        var tasks = new List<Task>(publishers.Length * _healthReportQueue.Count);

        while (_healthReportQueue.Any())
        {
            var report = _healthReportQueue.Dequeue();
            foreach (var publisher in publishers)
            {
                tasks.Add(RunPublisherAsync(publisher, report, cancellationToken));
            }
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task RunPublisherAsync(IHealthCheckPublisher publisher, HealthReport report, CancellationToken cancellationToken)
    {
        var duration = ValueStopwatch.StartNew();

        try
        {
            Logger.HealthCheckPublisherBegin(_logger, publisher);

            await publisher.PublishAsync(report, cancellationToken).ConfigureAwait(false);
            Logger.HealthCheckPublisherEnd(_logger, publisher, duration.GetElapsedTime());
        }
        catch (OperationCanceledException) when (IsStopping)
        {
            // This is a cancellation - if the app is shutting down we want to ignore it. Otherwise, it's
            // a timeout and we want to log it.
        }
        catch (OperationCanceledException)
        {
            Logger.HealthCheckPublisherTimeout(_logger, publisher, duration.GetElapsedTime());
            throw;
        }
        catch (Exception ex)
        {
            Logger.HealthCheckPublisherError(_logger, publisher, duration.GetElapsedTime(), ex);
            throw;
        }
    }

    internal static class EventIds
    {
        public const int HealthCheckPublisherProcessingBeginId = 100;
        public const int HealthCheckPublisherProcessingEndId = 101;
        public const int HealthCheckPublisherBeginId = 102;
        public const int HealthCheckPublisherEndId = 103;
        public const int HealthCheckPublisherErrorId = 104;
        public const int HealthCheckPublisherTimeoutId = 104;

        // Hard code the event names to avoid breaking changes. Even if the methods are renamed, these hard-coded names shouldn't change.
        public const string HealthCheckPublisherProcessingBeginName = "HealthCheckPublisherProcessingBegin";
        public const string HealthCheckPublisherProcessingEndName = "HealthCheckPublisherProcessingEnd";
        public const string HealthCheckPublisherBeginName = "HealthCheckPublisherBegin";
        public const string HealthCheckPublisherEndName = "HealthCheckPublisherEnd";
        public const string HealthCheckPublisherErrorName = "HealthCheckPublisherError";
        public const string HealthCheckPublisherTimeoutName = "HealthCheckPublisherTimeout";
    }

    private static partial class Logger
    {
        [LoggerMessage(EventIds.HealthCheckPublisherProcessingBeginId, LogLevel.Debug, "Running health check publishers", EventName = EventIds.HealthCheckPublisherProcessingBeginName)]
        public static partial void HealthCheckPublisherProcessingBegin(ILogger logger);

        public static void HealthCheckPublisherProcessingEnd(ILogger logger, TimeSpan duration, Exception? exception = null) =>
            HealthCheckPublisherProcessingEnd(logger, duration.TotalMilliseconds, exception);

        [LoggerMessage(EventIds.HealthCheckPublisherProcessingEndId, LogLevel.Debug, "Health check publisher processing completed after {ElapsedMilliseconds}ms", EventName = EventIds.HealthCheckPublisherProcessingEndName)]
        private static partial void HealthCheckPublisherProcessingEnd(ILogger logger, double ElapsedMilliseconds, Exception? exception = null);

        [LoggerMessage(EventIds.HealthCheckPublisherBeginId, LogLevel.Debug, "Running health check publisher '{HealthCheckPublisher}'", EventName = EventIds.HealthCheckPublisherBeginName)]
        public static partial void HealthCheckPublisherBegin(ILogger logger, IHealthCheckPublisher HealthCheckPublisher);

        public static void HealthCheckPublisherEnd(ILogger logger, IHealthCheckPublisher HealthCheckPublisher, TimeSpan duration) =>
            HealthCheckPublisherEnd(logger, HealthCheckPublisher, duration.TotalMilliseconds);

        [LoggerMessage(EventIds.HealthCheckPublisherEndId, LogLevel.Debug, "Health check '{HealthCheckPublisher}' completed after {ElapsedMilliseconds}ms", EventName = EventIds.HealthCheckPublisherEndName)]
        private static partial void HealthCheckPublisherEnd(ILogger logger, IHealthCheckPublisher HealthCheckPublisher, double ElapsedMilliseconds);

        public static void HealthCheckPublisherError(ILogger logger, IHealthCheckPublisher publisher, TimeSpan duration, Exception exception) =>
            HealthCheckPublisherError(logger, publisher, duration.TotalMilliseconds, exception);

#pragma warning disable SYSLIB1006
        [LoggerMessage(EventIds.HealthCheckPublisherErrorId, LogLevel.Error, "Health check {HealthCheckPublisher} threw an unhandled exception after {ElapsedMilliseconds}ms", EventName = EventIds.HealthCheckPublisherErrorName)]
        private static partial void HealthCheckPublisherError(ILogger logger, IHealthCheckPublisher HealthCheckPublisher, double ElapsedMilliseconds, Exception exception);

        public static void HealthCheckPublisherTimeout(ILogger logger, IHealthCheckPublisher publisher, TimeSpan duration) =>
            HealthCheckPublisherTimeout(logger, publisher, duration.TotalMilliseconds);

        [LoggerMessage(EventIds.HealthCheckPublisherTimeoutId, LogLevel.Error, "Health check {HealthCheckPublisher} was canceled after {ElapsedMilliseconds}ms", EventName = EventIds.HealthCheckPublisherTimeoutName)]
        private static partial void HealthCheckPublisherTimeout(ILogger logger, IHealthCheckPublisher HealthCheckPublisher, double ElapsedMilliseconds);
#pragma warning restore SYSLIB1006
    }
}
