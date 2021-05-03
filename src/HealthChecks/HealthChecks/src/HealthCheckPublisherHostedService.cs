// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    internal sealed partial class HealthCheckPublisherHostedService : IHostedService
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly IOptions<HealthCheckPublisherOptions> _options;
        private readonly ILogger _logger;
        private readonly IHealthCheckPublisher[] _publishers;

        private CancellationTokenSource _stopping;
        private Timer? _timer;
        private CancellationTokenSource? _runTokenSource;

        public HealthCheckPublisherHostedService(
            HealthCheckService healthCheckService,
            IOptions<HealthCheckPublisherOptions> options,
            ILogger<HealthCheckPublisherHostedService> logger,
            IEnumerable<IHealthCheckPublisher> publishers)
        {
            if (healthCheckService == null)
            {
                throw new ArgumentNullException(nameof(healthCheckService));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
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
            _options = options;
            _logger = logger;
            _publishers = publishers.ToArray();

            _stopping = new CancellationTokenSource();
        }

        internal bool IsStopping => _stopping.IsCancellationRequested;

        internal bool IsTimerRunning => _timer != null;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_publishers.Length == 0)
            {
                return Task.CompletedTask;
            }

            // IMPORTANT - make sure this is the last thing that happens in this method. The timer can
            // fire before other code runs.
            _timer = NonCapturingTimer.Create(Timer_Tick, null, dueTime: _options.Value.Delay, period: _options.Value.Period);

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

            _timer?.Dispose();
            _timer = null;


            return Task.CompletedTask;
        }

        // Yes, async void. We need to be async. We need to be void. We handle the exceptions in RunAsync
        private async void Timer_Tick(object? state)
        {
            await RunAsync();
        }

        // Internal for testing
        internal void CancelToken()
        {
            _runTokenSource!.Cancel();
        }

        // Internal for testing
        internal async Task RunAsync()
        {
            var duration = ValueStopwatch.StartNew();
            HealthCheckPublisherProcessingBegin();

            CancellationTokenSource? cancellation = null;
            try
            {
                var timeout = _options.Value.Timeout;

                cancellation = CancellationTokenSource.CreateLinkedTokenSource(_stopping.Token);
                _runTokenSource = cancellation;
                cancellation.CancelAfter(timeout);

                await RunAsyncCore(cancellation.Token);

                HealthCheckPublisherProcessingEnd(duration.GetElapsedTime());
            }
            catch (OperationCanceledException) when (IsStopping)
            {
                // This is a cancellation - if the app is shutting down we want to ignore it. Otherwise, it's
                // a timeout and we want to log it.
            }
            catch (Exception ex)
            {
                // This is an error, publishing failed.
                HealthCheckPublisherProcessingEnd(duration.GetElapsedTime(), ex);
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
            var report = await _healthCheckService.CheckHealthAsync(_options.Value.Predicate, cancellationToken);

            var publishers = _publishers;
            var tasks = new Task[publishers.Length];
            for (var i = 0; i < publishers.Length; i++)
            {
                tasks[i] = RunPublisherAsync(publishers[i], report, cancellationToken);
            }

            await Task.WhenAll(tasks);
        }

        private async Task RunPublisherAsync(IHealthCheckPublisher publisher, HealthReport report, CancellationToken cancellationToken)
        {
            var duration = ValueStopwatch.StartNew();

            try
            {
                HealthCheckPublisherBegin(publisher);

                await publisher.PublishAsync(report, cancellationToken);
                HealthCheckPublisherEnd(publisher, duration.GetElapsedTime());
            }
            catch (OperationCanceledException) when (IsStopping)
            {
                // This is a cancellation - if the app is shutting down we want to ignore it. Otherwise, it's
                // a timeout and we want to log it.
            }
            catch (OperationCanceledException)
            {
                HealthCheckPublisherTimeout(publisher, duration.GetElapsedTime());
                throw;
            }
            catch (Exception ex)
            {
                HealthCheckPublisherError(publisher, duration.GetElapsedTime(), ex);
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

            public static readonly EventId HealthCheckPublisherProcessingBegin = new EventId(HealthCheckPublisherProcessingBeginId, nameof(HealthCheckPublisherProcessingBegin));
            public static readonly EventId HealthCheckPublisherProcessingEnd = new EventId(HealthCheckPublisherProcessingEndId, nameof(HealthCheckPublisherProcessingEnd));
            public static readonly EventId HealthCheckPublisherBegin = new EventId(HealthCheckPublisherBeginId, nameof(HealthCheckPublisherBegin));
            public static readonly EventId HealthCheckPublisherEnd = new EventId(HealthCheckPublisherEndId, nameof(HealthCheckPublisherEnd));
            public static readonly EventId HealthCheckPublisherError = new EventId(HealthCheckPublisherErrorId, nameof(HealthCheckPublisherError));
            public static readonly EventId HealthCheckPublisherTimeout = new EventId(HealthCheckPublisherTimeoutId, nameof(HealthCheckPublisherTimeout));
        }

        [LoggerMessage(EventId = EventIds.HealthCheckPublisherProcessingBeginId, Level = LogLevel.Debug, Message = "Running health check publishers")]
        private partial void HealthCheckPublisherProcessingBegin();

        private void HealthCheckPublisherProcessingEnd(TimeSpan duration, Exception? exception = null) =>
            HealthCheckPublisherProcessingEnd(duration.TotalMilliseconds, exception);

        [LoggerMessage(EventId = EventIds.HealthCheckPublisherProcessingEndId, Level = LogLevel.Debug, Message = "Health check publisher processing completed after {ElapsedMilliseconds}ms")]
        private partial void HealthCheckPublisherProcessingEnd(double ElapsedMilliseconds, Exception? exception = null);

        [LoggerMessage(EventId = EventIds.HealthCheckPublisherBeginId, Level = LogLevel.Debug, Message = "Running health check publisher '{HealthCheckPublisher}'")]
        private partial void HealthCheckPublisherBegin(IHealthCheckPublisher HealthCheckPublisher);

        private void HealthCheckPublisherEnd(IHealthCheckPublisher HealthCheckPublisher, TimeSpan duration) =>
            HealthCheckPublisherEnd(HealthCheckPublisher, duration.TotalMilliseconds);

        [LoggerMessage(EventId = EventIds.HealthCheckPublisherEndId, Level = LogLevel.Debug, Message = "Health check '{HealthCheckPublisher}' completed after {ElapsedMilliseconds}ms")]
        private partial void HealthCheckPublisherEnd(IHealthCheckPublisher HealthCheckPublisher, double ElapsedMilliseconds);

        private void HealthCheckPublisherError(IHealthCheckPublisher publisher, TimeSpan duration, Exception exception) =>
            HealthCheckPublisherError(publisher, duration.TotalMilliseconds, exception);

#pragma warning disable SYSLIB1006
        [LoggerMessage(EventId = EventIds.HealthCheckPublisherErrorId, Level = LogLevel.Error, Message = "Health check {HealthCheckPublisher} threw an unhandled exception after {ElapsedMilliseconds}ms")]
        private partial void HealthCheckPublisherError(IHealthCheckPublisher HealthCheckPublisher, double ElapsedMilliseconds, Exception exception);

        private void HealthCheckPublisherTimeout(IHealthCheckPublisher publisher, TimeSpan duration) =>
            HealthCheckPublisherTimeout(publisher, duration.TotalMilliseconds);

        [LoggerMessage(EventId = EventIds.HealthCheckPublisherTimeoutId, Level = LogLevel.Error, Message = "Health check {HealthCheckPublisher} was canceled after {ElapsedMilliseconds}ms")]
        private partial void HealthCheckPublisherTimeout(IHealthCheckPublisher HealthCheckPublisher, double ElapsedMilliseconds);
#pragma warning restore SYSLIB1006
    }
}
