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
    internal sealed class HealthCheckPublisherHostedService : IHostedService
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly IOptions<HealthCheckPublisherOptions> _options;
        private readonly ILogger _logger;
        private readonly IHealthCheckPublisher[] _publishers;

        private CancellationTokenSource _stopping;
        private Timer _timer;

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
        private async void Timer_Tick(object state)
        {
            await RunAsync();
        }

        // Internal for testing
        internal async Task RunAsync()
        {
            var duration = ValueStopwatch.StartNew();
            Logger.HealthCheckPublisherProcessingBegin(_logger);

            CancellationTokenSource cancellation = null;
            try
            {
                var timeout = _options.Value.Timeout;

                cancellation = CancellationTokenSource.CreateLinkedTokenSource(_stopping.Token);
                cancellation.CancelAfter(timeout);

                await RunAsyncCore(cancellation.Token);

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
                cancellation.Dispose();
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
                Logger.HealthCheckPublisherBegin(_logger, publisher);

                await publisher.PublishAsync(report, cancellationToken);
                Logger.HealthCheckPublisherEnd(_logger, publisher, duration.GetElapsedTime());
            }
            catch (OperationCanceledException) when (IsStopping)
            {
                // This is a cancellation - if the app is shutting down we want to ignore it. Otherwise, it's
                // a timeout and we want to log it.
            }
            catch (OperationCanceledException ocex)
            {
                Logger.HealthCheckPublisherTimeout(_logger, publisher, duration.GetElapsedTime());
                throw ocex;
            }
            catch (Exception ex)
            {
                Logger.HealthCheckPublisherError(_logger, publisher, duration.GetElapsedTime(), ex);
                throw ex;
            }
        }

        internal static class EventIds
        {
            public static readonly EventId HealthCheckPublisherProcessingBegin = new EventId(100, "HealthCheckPublisherProcessingBegin");
            public static readonly EventId HealthCheckPublisherProcessingEnd = new EventId(101, "HealthCheckPublisherProcessingEnd");
            public static readonly EventId HealthCheckPublisherProcessingError = new EventId(101, "HealthCheckPublisherProcessingError");

            public static readonly EventId HealthCheckPublisherBegin = new EventId(102, "HealthCheckPublisherBegin");
            public static readonly EventId HealthCheckPublisherEnd = new EventId(103, "HealthCheckPublisherEnd");
            public static readonly EventId HealthCheckPublisherError = new EventId(104, "HealthCheckPublisherError");
            public static readonly EventId HealthCheckPublisherTimeout = new EventId(104, "HealthCheckPublisherTimeout");
        }

        private static class Logger
        {
            private static readonly Action<ILogger, Exception> _healthCheckPublisherProcessingBegin = LoggerMessage.Define(
                LogLevel.Debug,
                EventIds.HealthCheckPublisherProcessingBegin,
                "Running health check publishers");

            private static readonly Action<ILogger, double, Exception> _healthCheckPublisherProcessingEnd = LoggerMessage.Define<double>(
                LogLevel.Debug,
                EventIds.HealthCheckPublisherProcessingEnd,
                "Health check publisher processing completed after {ElapsedMilliseconds}ms");

            private static readonly Action<ILogger, IHealthCheckPublisher, Exception> _healthCheckPublisherBegin = LoggerMessage.Define<IHealthCheckPublisher>(
                LogLevel.Debug,
                EventIds.HealthCheckPublisherBegin,
                "Running health check publisher '{HealthCheckPublisher}'");

            private static readonly Action<ILogger, IHealthCheckPublisher, double, Exception> _healthCheckPublisherEnd = LoggerMessage.Define<IHealthCheckPublisher, double>(
                LogLevel.Debug,
                EventIds.HealthCheckPublisherEnd,
                "Health check '{HealthCheckPublisher}' completed after {ElapsedMilliseconds}ms");

            private static readonly Action<ILogger, IHealthCheckPublisher, double, Exception> _healthCheckPublisherError = LoggerMessage.Define<IHealthCheckPublisher, double>(
                LogLevel.Error,
                EventIds.HealthCheckPublisherError,
                "Health check {HealthCheckPublisher} threw an unhandled exception after {ElapsedMilliseconds}ms");

            private static readonly Action<ILogger, IHealthCheckPublisher, double, Exception> _healthCheckPublisherTimeout = LoggerMessage.Define<IHealthCheckPublisher, double>(
                LogLevel.Error,
                EventIds.HealthCheckPublisherTimeout,
                "Health check {HealthCheckPublisher} was canceled after {ElapsedMilliseconds}ms");

            public static void HealthCheckPublisherProcessingBegin(ILogger logger)
            {
                _healthCheckPublisherProcessingBegin(logger, null);
            }

            public static void HealthCheckPublisherProcessingEnd(ILogger logger, TimeSpan duration, Exception exception = null)
            {
                _healthCheckPublisherProcessingEnd(logger, duration.TotalMilliseconds, exception);
            }

            public static void HealthCheckPublisherBegin(ILogger logger, IHealthCheckPublisher publisher)
            {
                _healthCheckPublisherBegin(logger, publisher, null);
            }

            public static void HealthCheckPublisherEnd(ILogger logger, IHealthCheckPublisher publisher, TimeSpan duration)
            {
                _healthCheckPublisherEnd(logger, publisher, duration.TotalMilliseconds, null);
            }

            public static void HealthCheckPublisherError(ILogger logger, IHealthCheckPublisher publisher, TimeSpan duration, Exception exception)
            {
                _healthCheckPublisherError(logger, publisher, duration.TotalMilliseconds, exception);
            }

            public static void HealthCheckPublisherTimeout(ILogger logger, IHealthCheckPublisher publisher, TimeSpan duration)
            {
                _healthCheckPublisherTimeout(logger, publisher, duration.TotalMilliseconds, null);
            }
        }
    }
}
