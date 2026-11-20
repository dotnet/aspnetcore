// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class RemoteNavigationManagerTest
{
    [Theory]
    [InlineData(false, "NavigationTimedOut")]
    [InlineData(true, "NavigationStoppedSessionEnded")]
    public async Task NavigateTo_WhenInteropCannotComplete_DoesNotRaiseUnhandledException(bool disconnected, string expectedEventName)
    {
        var circuitOptions = new CircuitOptions
        {
            JSInteropDefaultCallTimeout = TimeSpan.FromMilliseconds(100),
        };

        var client = new Mock<ISingleClientProxy>();
        // A dropped interop call is never answered.
        client.Setup(c => c.SendCoreAsync("JS.BeginInvokeJS", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var jsRuntime = new RemoteJSRuntime(
            Options.Create(circuitOptions),
            Options.Create(new HubOptions<ComponentHub>()),
            NullLogger<RemoteJSRuntime>.Instance);
        jsRuntime.Initialize(new CircuitClientProxy(client.Object, "connection-id"));

        if (disconnected)
        {
            jsRuntime.MarkPermanentlyDisconnected();
        }

        var logger = new NavigationOutcomeLogger();
        var navigationManager = new RemoteNavigationManager(logger);
        navigationManager.Initialize("https://localhost/", "https://localhost/");
        navigationManager.AttachJsRuntime(jsRuntime);

        var unhandledException = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        navigationManager.UnhandledException += (sender, exception) => unhandledException.TrySetResult(exception);

        navigationManager.NavigateTo("/done");
        var outcome = await logger.Outcome.WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(expectedEventName, outcome.EventId.Name);
        Assert.Equal(LogLevel.Debug, outcome.Level);
        Assert.False(unhandledException.Task.IsCompleted);
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
