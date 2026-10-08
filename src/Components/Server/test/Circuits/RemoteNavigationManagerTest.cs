// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class RemoteNavigationManagerTest
{
    [Theory]
    [InlineData(NavigationFailure.TimedOutOnLiveCircuit)]
    [InlineData(NavigationFailure.SessionAlreadyEnded)]
    [InlineData(NavigationFailure.SessionEndedDuringInvocation)]
    [InlineData(NavigationFailure.UnexpectedError)]
    [InlineData(NavigationFailure.None)]
    public async Task NavigateTo_WhenInteropCompletes_RaisesUnhandledExceptionOnlyForUnexpectedErrors(NavigationFailure failure)
    {
        var circuitOptions = new CircuitOptions();
        if (failure is NavigationFailure.TimedOutOnLiveCircuit or NavigationFailure.SessionEndedDuringInvocation)
        {
            // Stands in for the default one-minute interop timeout, so the lost call times out in milliseconds.
            circuitOptions.JSInteropDefaultCallTimeout = TimeSpan.FromMilliseconds(
                failure == NavigationFailure.TimedOutOnLiveCircuit ? 100 : 1000);
        }

        var invocationStarted = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sendFailure = new InvalidOperationException("Simulated interop failure.");
        var client = new Mock<ISingleClientProxy>();
        var sendJSCall = client.Setup(c => c.SendCoreAsync("JS.BeginInvokeJS", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((method, args, cancellationToken) =>
                invocationStarted.TrySetResult((long)args[0]));
        if (failure == NavigationFailure.UnexpectedError)
        {
            sendJSCall.Throws(sendFailure);
        }
        else
        {
            // Completing without delivering is what the hub does for a connection that has gone away,
            // so the navigateTo call is never answered.
            sendJSCall.Returns(Task.CompletedTask);
        }

        var jsRuntime = new RemoteJSRuntime(
            Options.Create(circuitOptions),
            Options.Create(new HubOptions<ComponentHub>()),
            NullLogger<RemoteJSRuntime>.Instance);
        jsRuntime.Initialize(new CircuitClientProxy(client.Object, "connection-id"));

        if (failure == NavigationFailure.SessionAlreadyEnded)
        {
            // CircuitHost.DisposeAsync does this when the circuit is disposed.
            jsRuntime.MarkPermanentlyDisconnected();
        }

        var logger = new NavigationOutcomeLogger();
        var navigationManager = new RemoteNavigationManager(logger);
        navigationManager.Initialize("https://localhost/", "https://localhost/");
        navigationManager.AttachJsRuntime(jsRuntime);

        var unhandledException = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        navigationManager.UnhandledException += (sender, exception) => unhandledException.TrySetResult(exception);

        navigationManager.NavigateTo("/done");
        if (failure == NavigationFailure.SessionEndedDuringInvocation)
        {
            await invocationStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
            jsRuntime.MarkPermanentlyDisconnected();
        }
        else if (failure == NavigationFailure.None)
        {
            var invocationId = await invocationStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
            DotNetDispatcher.EndInvokeJS(jsRuntime, FormattableString.Invariant($"[{invocationId},true,null]"));
        }

        var outcome = await logger.Outcome.WaitAsync(TimeSpan.FromSeconds(30));

        var (expectedId, expectedName, expectedLevel, expectsUnhandledException) = failure switch
        {
            NavigationFailure.TimedOutOnLiveCircuit => (8, "NavigationTimedOut", LogLevel.Warning, false),
            NavigationFailure.SessionAlreadyEnded or NavigationFailure.SessionEndedDuringInvocation =>
                (7, "NavigationStoppedSessionEnded", LogLevel.Debug, false),
            NavigationFailure.UnexpectedError => (4, "NavigationFailed", LogLevel.Error, true),
            NavigationFailure.None => (6, "NavigationCompleted", LogLevel.Debug, false),
            _ => throw new ArgumentOutOfRangeException(nameof(failure)),
        };

        Assert.Equal(expectedId, outcome.EventId.Id);
        Assert.Equal(expectedName, outcome.EventId.Name);
        Assert.Equal(expectedLevel, outcome.Level);

        if (expectsUnhandledException)
        {
            var exception = await unhandledException.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Same(sendFailure, exception);
        }
        else
        {
            // Logging the outcome is the last thing the navigation does, so nothing can raise the event after it.
            Assert.False(unhandledException.Task.IsCompleted);
        }
    }

    public enum NavigationFailure
    {
        TimedOutOnLiveCircuit,
        SessionAlreadyEnded,
        SessionEndedDuringInvocation,
        UnexpectedError,
        None,
    }

    private sealed class NavigationOutcomeLogger : ILogger<RemoteNavigationManager>
    {
        private readonly TaskCompletionSource<(EventId EventId, LogLevel Level)> _outcome =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Completes with the first log entry that ends a navigation.
        public Task<(EventId EventId, LogLevel Level)> Outcome => _outcome.Task;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Name is "NavigationCompleted" or "NavigationCanceled" or "NavigationFailed"
                or "NavigationStoppedSessionEnded" or "NavigationTimedOut")
            {
                _outcome.TrySetResult((eventId, logLevel));
            }
        }
    }
}
