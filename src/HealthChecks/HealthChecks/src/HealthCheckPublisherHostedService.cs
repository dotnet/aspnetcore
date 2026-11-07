// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed partial class HealthCheckPublisherHostedService : IHostedService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IOptions<HealthCheckServiceOptions> _healthCheckServiceOptions;
    private readonly IOptions<HealthCheckPublisherOptions> _healthCheckPublisherOptions;
    private readonly ILogger _logger;
    private readonly IHealthCheckPublisher[] _publishers;
    private List<Timer>? _timers;

    private readonly object _runningTasksLock = new object();
    private readonly HashSet<Task> _runningTasks = new HashSet<Task>();

    private readonly CancellationTokenSource _stopping;
    private CancellationTokenSource? _runTokenSource;

    public HealthCheckPublisherHostedService(
        HealthCheckService healthCheckService,
        IOptions<HealthCheckServiceOptions> healthCheckServiceOptions,
        IOptions<HealthCheckPublisherOptions> healthCheckPublisherOptions,
        ILogger<HealthCheckPublisherHostedService> logger,
        IEnumerable<IHealthCheckPublisher> publishers)
    {
        ArgumentNullThrowHelper.ThrowIfNull(healthCheckService);
        ArgumentNullThrowHelper.ThrowIfNull(healthCheckServiceOptions);
        ArgumentNullThrowHelper.ThrowIfNull(healthCheckPublisherOptions);
        ArgumentNullThrowHelper.ThrowIfNull(logger);
        ArgumentNullThrowHelper.ThrowIfNull(publishers);

        _healthCheckService = healthCheckService;
        _healthCheckServiceOptions = healthCheckServiceOptions;
        _healthCheckPublisherOptions = healthCheckPublisherOptions;
        _logger = logger;
        _publishers = publishers.ToArray();

        _stopping = new CancellationTokenSource();
    }

    private (TimeSpan Delay, TimeSpan Period) GetTimerOptions(HealthCheckRegistration registration)
    {
        return (registration?.Delay ?? _healthCheckPublisherOptions.Value.Delay, registration?.Period ?? _healthCheckPublisherOptions.Value.Period);
    }

    internal bool IsStopping => _stopping.IsCancellationRequested;

    internal bool IsTimerRunning => _timers != null;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_publishers.Length == 0)
        {
            return Task.CompletedTask;
        }

        // IMPORTANT - make sure this is the last thing that happens in this method. The timers can
        // fire before other code runs.
        _timers = CreateTimers();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
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
            return;
        }

        if (_timers != null)
        {
            foreach (var timer in _timers)
            {
                timer.Dispose();
            }

            _timers = null;
        }

        // Disposing the timers above doesn't wait for a publish that is already running. Wait here for
        // any in-flight publish operations to finish, bounded by the caller's cancellation token (the
        // host links this to its shutdown timeout). Publishers that observe cancellation were already
        // signaled via _stopping above. RunAsync handles all of its own exceptions, so awaiting these
        // tasks can't throw.
        Task[] runningTasks;
        lock (_runningTasksLock)
        {
            runningTasks = _runningTasks.ToArray();
        }

        if (runningTasks.Length == 0)
        {
            return;
        }

        var runningTask = Task.WhenAll(runningTasks);
#if NET
        await runningTask.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
        var canceledTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(static state => ((TaskCompletionSource<object?>)state!).TrySetResult(null), canceledTcs))
        {
            await Task.WhenAny(runningTask, canceledTcs.Task).ConfigureAwait(false);
        }
#endif
    }

    private List<Timer> CreateTimers()
    {
        var delayPeriodGroups = new HashSet<(TimeSpan Delay, TimeSpan Period)>();
        foreach (var hc in _healthCheckServiceOptions.Value.Registrations)
        {
            var timerOptions = GetTimerOptions(hc);
            delayPeriodGroups.Add(timerOptions);
        }

        var timers = new List<Timer>(delayPeriodGroups.Count);
        foreach (var group in delayPeriodGroups)
        {
            var timer = CreateTimer(group);
            timers.Add(timer);
        }

        return timers;
    }

    private Timer CreateTimer((TimeSpan Delay, TimeSpan Period) timerOptions)
    {
        return
            NonCapturingTimer.Create(
            (state) =>
            {
                _ = RunTimerCallbackAsync(timerOptions);
            },
            null,
            dueTime: timerOptions.Delay,
            period: timerOptions.Period);
    }

    // The timer callback can't be awaited by the caller, so track the RunAsync task here to let
    // StopAsync wait for a publish that is in flight when shutdown starts (https://github.com/dotnet/aspnetcore/issues/67304).
    private async Task RunTimerCallbackAsync((TimeSpan Delay, TimeSpan Period) timerOptions)
    {
        Task runTask;
        lock (_runningTasksLock)
        {
            // If shutdown has already started, don't begin a new publish. StopAsync signals
            // _stopping before it snapshots _runningTasks, so a callback that reaches this point
            // after that snapshot would otherwise start work that outlives StopAsync.
            if (_stopping.IsCancellationRequested)
            {
                return;
            }

            runTask = RunAsync(timerOptions);
            _runningTasks.Add(runTask);
        }

        try
        {
            await runTask.ConfigureAwait(false);
        }
        finally
        {
            lock (_runningTasksLock)
            {
                _runningTasks.Remove(runTask);
            }
        }
    }

    // Internal for testing
    internal void CancelToken()
    {
        _runTokenSource!.Cancel();
    }

    // Internal for testing
    internal async Task RunAsync((TimeSpan Delay, TimeSpan Period) timerOptions)
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

            await RunAsyncCore(timerOptions, cancellation.Token).ConfigureAwait(false);

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

    private async Task RunAsyncCore((TimeSpan Delay, TimeSpan Period) timerOptions, CancellationToken cancellationToken)
    {
        // Forcibly yield - we want to unblock the timer thread.
        await Task.Yield();

        // Concatenate predicates - we only run HCs at the set delay and period
        var withOptionsPredicate = (HealthCheckRegistration r) =>
        {
            // First check whether the current timer options correspond to the current registration,
            // and then check the user-defined predicate if any.
            return (GetTimerOptions(r) == timerOptions) && (_healthCheckPublisherOptions?.Value.Predicate ?? (_ => true))(r);
        };

        // The health checks service does it's own logging, and doesn't throw exceptions.
        var report = await _healthCheckService.CheckHealthAsync(withOptionsPredicate, cancellationToken).ConfigureAwait(false);

        var publishers = _publishers;
        var tasks = new Task[publishers.Length];
        for (var i = 0; i < publishers.Length; i++)
        {
            tasks[i] = RunPublisherAsync(publishers[i], report, cancellationToken);
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

        [LoggerMessage(EventIds.HealthCheckPublisherErrorId, LogLevel.Error, "Health check {HealthCheckPublisher} threw an unhandled exception after {ElapsedMilliseconds}ms", EventName = EventIds.HealthCheckPublisherErrorName)]
        private static partial void HealthCheckPublisherError(ILogger logger, IHealthCheckPublisher HealthCheckPublisher, double ElapsedMilliseconds, Exception exception);

        public static void HealthCheckPublisherTimeout(ILogger logger, IHealthCheckPublisher publisher, TimeSpan duration) =>
            HealthCheckPublisherTimeout(logger, publisher, duration.TotalMilliseconds);

        [LoggerMessage(EventIds.HealthCheckPublisherTimeoutId, LogLevel.Error, "Health check {HealthCheckPublisher} was canceled after {ElapsedMilliseconds}ms", EventName = EventIds.HealthCheckPublisherTimeoutName)]
        private static partial void HealthCheckPublisherTimeout(ILogger logger, IHealthCheckPublisher HealthCheckPublisher, double ElapsedMilliseconds);
    }
}
