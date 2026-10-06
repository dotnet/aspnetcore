// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class RemoteNavigationManagerTest
{
    [Fact]
    public async Task NavigateTo_WhenInvocationTimesOut_LogsTimeoutWithoutInvokingUnhandledException()
    {
        var (navigationManager, jsRuntime, logger) = CreateNavigationManager(TimeSpan.FromMilliseconds(10));
        var unhandledException = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        navigationManager.UnhandledException += (_, exception) => unhandledException.TrySetResult(exception);

        navigationManager.NavigateTo("https://example.com/page");

        var timeoutLogged = logger.WaitForEventAsync(8);
        var completedTask = await Task.WhenAny(timeoutLogged, unhandledException.Task).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Same(timeoutLogged, completedTask);
        Assert.False(unhandledException.Task.IsCompleted);
        Assert.IsType<TaskCanceledException>(logger.Records.Single(record => record.EventId.Id == 8).Exception);
        Assert.DoesNotContain(logger.Records, record => record.EventId.Id == 4);
    }

    [Fact]
    public async Task NavigateTo_WhenPermanentlyDisconnectedDuringInvocation_LogsSessionEndedWithoutInvokingUnhandledException()
    {
        var (navigationManager, jsRuntime, logger) = CreateNavigationManager(TimeSpan.FromMilliseconds(100));
        Exception unhandledException = null;
        navigationManager.UnhandledException += (_, exception) => unhandledException = exception;

        navigationManager.NavigateTo("https://example.com/page");
        await jsRuntime.InvocationStarted;
        jsRuntime.MarkPermanentlyDisconnected();

        await logger.WaitForEventAsync(7);
        Assert.Null(unhandledException);
        Assert.DoesNotContain(logger.Records, record => record.EventId.Id is 4 or 8);
    }

    [Fact]
    public async Task NavigateTo_WhenAlreadyPermanentlyDisconnected_LogsSessionEndedWithoutInvokingUnhandledException()
    {
        var (navigationManager, jsRuntime, logger) = CreateNavigationManager();
        Exception unhandledException = null;
        navigationManager.UnhandledException += (_, exception) => unhandledException = exception;
        jsRuntime.UseProductionBeginInvoke = true;
        jsRuntime.MarkPermanentlyDisconnected();

        navigationManager.NavigateTo("https://example.com/page");

        await logger.WaitForEventAsync(7);
        Assert.Null(unhandledException);
        Assert.DoesNotContain(logger.Records, record => record.EventId.Id is 4 or 8);
    }

    [Fact]
    public async Task NavigateTo_WhenInvocationFails_LogsFailureAndInvokesUnhandledException()
    {
        var (navigationManager, jsRuntime, logger) = CreateNavigationManager();
        var expectedException = new InvalidOperationException("Test exception");
        var unhandledException = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        navigationManager.UnhandledException += (_, exception) => unhandledException.SetResult(exception);
        jsRuntime.ExceptionToThrow = expectedException;

        navigationManager.NavigateTo("https://example.com/page");

        Assert.Same(expectedException, await unhandledException.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        var record = Assert.Single(logger.Records, record => record.EventId.Id == 4);
        Assert.Same(expectedException, record.Exception);
    }

    [Fact]
    public async Task NavigateTo_WhenInvocationCompletes_LogsCompletionWithoutInvokingUnhandledException()
    {
        var (navigationManager, jsRuntime, logger) = CreateNavigationManager();
        Exception unhandledException = null;
        navigationManager.UnhandledException += (_, exception) => unhandledException = exception;

        navigationManager.NavigateTo("https://example.com/page");
        await jsRuntime.CompleteInvocationAsync();

        await logger.WaitForEventAsync(6);
        Assert.Null(unhandledException);
        Assert.DoesNotContain(logger.Records, record => record.EventId.Id == 4);
    }

    private static (RemoteNavigationManager NavigationManager, TestRemoteJSRuntime JSRuntime, TestLogger Logger) CreateNavigationManager(
        TimeSpan? defaultAsyncTimeout = null)
    {
        var logger = new TestLogger();
        var navigationManager = new RemoteNavigationManager(logger);
        navigationManager.Initialize("https://example.com/", "https://example.com/");
        var circuitOptions = new CircuitOptions();
        if (defaultAsyncTimeout is not null)
        {
            circuitOptions.JSInteropDefaultCallTimeout = defaultAsyncTimeout.Value;
        }

        var jsRuntime = new TestRemoteJSRuntime(
            Options.Create(circuitOptions),
            Options.Create(new HubOptions<ComponentHub>()),
            NullLogger<RemoteJSRuntime>.Instance);
        navigationManager.AttachJsRuntime(jsRuntime);

        return (navigationManager, jsRuntime, logger);
    }

    private sealed class TestRemoteJSRuntime(
        IOptions<CircuitOptions> circuitOptions,
        IOptions<HubOptions<ComponentHub>> hubOptions,
        ILogger<RemoteJSRuntime> logger)
        : RemoteJSRuntime(circuitOptions, hubOptions, logger)
    {
        private readonly TaskCompletionSource<long> _invocationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Exception ExceptionToThrow { get; set; }

        public Task InvocationStarted => _invocationStarted.Task;

        public bool UseProductionBeginInvoke { get; set; }

        public async Task CompleteInvocationAsync()
        {
            var asyncHandle = await _invocationStarted.Task;
            DotNetDispatcher.EndInvokeJS(this, $"[{asyncHandle},true,null]");
        }

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            if (UseProductionBeginInvoke)
            {
                base.BeginInvokeJS(invocationInfo);
                return;
            }

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            _invocationStarted.SetResult(invocationInfo.AsyncHandle);
        }
    }

    private sealed class TestLogger : ILogger<RemoteNavigationManager>
    {
        private readonly ConcurrentQueue<LogRecord> _records = new();
        private readonly ConcurrentDictionary<int, TaskCompletionSource> _eventSources = new();

        public IReadOnlyCollection<LogRecord> Records => _records;

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            _records.Enqueue(new LogRecord(eventId, exception));
            _eventSources.GetOrAdd(
                eventId.Id,
                static _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();
        }

        public Task WaitForEventAsync(int eventId)
        {
            if (_records.Any(record => record.EventId.Id == eventId))
            {
                return Task.CompletedTask;
            }

            return _eventSources.GetOrAdd(
                eventId,
                static _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    private sealed record LogRecord(EventId EventId, Exception Exception);
}
