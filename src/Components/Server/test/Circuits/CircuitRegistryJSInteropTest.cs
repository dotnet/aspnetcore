// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class CircuitRegistryJSInteropTest
{
    [Fact]
    public async Task DisconnectAsync_FailsPendingJSInteropCalls()
    {
        var registry = CreateRegistry();
        var client = Mock.Of<ISingleClientProxy>(
            c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()) == Task.CompletedTask);
        var circuitHost = TestCircuitHost.Create(clientProxy: new CircuitClientProxy(client, "connection"));
        circuitHost.JSRuntime.Initialize(circuitHost.Client);
        registry.Register(circuitHost);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pendingInteropCall = circuitHost.JSRuntime.InvokeAsync<string>("test", cts.Token, Array.Empty<object>());

        await registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId);

        await Assert.ThrowsAsync<JSDisconnectedException>(async () => await pendingInteropCall);
    }

    [Fact]
    public async Task ConnectAsync_FailsOnlyPendingJSInteropCallsFromPreviousConnection()
    {
        var registry = CreateRegistry();
        var handler = new Mock<CircuitHandler> { CallBase = true };
        var client = Mock.Of<ISingleClientProxy>(
            c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()) == Task.CompletedTask);
        var circuitHost = TestCircuitHost.Create(
            handlers: [handler.Object],
            clientProxy: new CircuitClientProxy(client, "old-connection"));
        circuitHost.JSRuntime.Initialize(circuitHost.Client);
        registry.Register(circuitHost);

        var pendingCallFromPreviousConnection = circuitHost.JSRuntime.InvokeAsync<string>("old-call").AsTask();
        Task<string> pendingCallFromReplacementConnection = null;
        handler
            .Setup(h => h.OnConnectionUpAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                pendingCallFromReplacementConnection = circuitHost.JSRuntime.InvokeAsync<string>("new-call").AsTask();
                return Task.CompletedTask;
            });

        var replacementClient = Mock.Of<ISingleClientProxy>(
            c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()) == Task.CompletedTask);
        var result = await registry.ConnectAsync(circuitHost.CircuitId, replacementClient, "new-connection", default);

        Assert.Same(circuitHost, result);
        await Assert.ThrowsAsync<JSDisconnectedException>(() => pendingCallFromPreviousConnection);
        Assert.NotNull(pendingCallFromReplacementConnection);
        Assert.False(pendingCallFromReplacementConnection.IsCompleted);

        circuitHost.JSRuntime.MarkDisconnected();
        await Assert.ThrowsAsync<JSDisconnectedException>(() => pendingCallFromReplacementConnection);
    }

    [Fact]
    public async Task ConnectAsync_DoesNotDispatchInFlightInvocationToReplacementConnection()
    {
        var registry = CreateRegistry();
        var logger = new PausingRemoteJSRuntimeLogger();
        var oldClient = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        oldClient
            .Setup(c => c.SendCoreAsync("JS.BeginInvokeJS", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var replacementClient = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        var clientProxy = new CircuitClientProxy(oldClient.Object, "old-connection");
        var jsRuntime = new PausingRemoteJSRuntime(logger);
        var circuitHost = TestCircuitHost.Create(clientProxy: clientProxy, jsRuntime: jsRuntime);
        jsRuntime.Initialize(clientProxy);
        registry.Register(circuitHost);

        Task<string> pendingCall = null;
        var invokeTask = Task.Run(() =>
        {
            pendingCall = jsRuntime.InvokeAsync<string>("in-flight-call").AsTask();
        });
        await logger.InvocationReachedDispatch.WaitAsync(TimeSpan.FromSeconds(10));

        var reconnectTask = Task.Run(() => registry.ConnectAsync(
            circuitHost.CircuitId,
            replacementClient.Object,
            "new-connection",
            default));

        await jsRuntime.PendingTaskCaptureStarted.WaitAsync(TimeSpan.FromSeconds(10));

        try
        {
            Assert.False(reconnectTask.IsCompleted);
        }
        finally
        {
            logger.ResumeDispatch();
        }

        await invokeTask;
        Assert.Same(circuitHost, await reconnectTask);
        await Assert.ThrowsAsync<JSDisconnectedException>(() => pendingCall);
        oldClient.Verify(
            c => c.SendCoreAsync("JS.BeginInvokeJS", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        replacementClient.Verify(
            c => c.SendCoreAsync("JS.BeginInvokeJS", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static CircuitRegistry CreateRegistry()
    {
        return new CircuitRegistry(
            Options.Create(new CircuitOptions()),
            NullLogger<CircuitRegistry>.Instance,
            TestCircuitIdFactory.CreateTestFactory(),
            CreatePersistenceManager());
    }

    private static CircuitPersistenceManager CreatePersistenceManager()
    {
        return new CircuitPersistenceManager(
            Options.Create(new CircuitOptions()),
            new Endpoints.ServerComponentSerializer(new EphemeralDataProtectionProvider()),
            new TestCircuitPersistenceProvider(),
            new EphemeralDataProtectionProvider());
    }

    private class TestCircuitPersistenceProvider : ICircuitPersistenceProvider
    {
        public Task PersistCircuitAsync(CircuitId circuitId, PersistedCircuitState persistedCircuitState, CancellationToken cancellation = default)
            => Task.CompletedTask;

        public Task<PersistedCircuitState> RestoreCircuitAsync(CircuitId circuitId, CancellationToken cancellation = default)
            => throw new NotImplementedException();
    }

    private sealed class PausingRemoteJSRuntimeLogger : ILogger<RemoteJSRuntime>
    {
        private readonly TaskCompletionSource _invocationReachedDispatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _resumeDispatch = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task InvocationReachedDispatch => _invocationReachedDispatch.Task;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Id == 1)
            {
                _invocationReachedDispatch.TrySetResult();
                if (!_resumeDispatch.Task.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("Timed out waiting to resume JS dispatch.");
                }
            }
        }

        public void ResumeDispatch() => _resumeDispatch.TrySetResult();

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class PausingRemoteJSRuntime : RemoteJSRuntime
    {
        private readonly TaskCompletionSource _pendingTaskCaptureStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PausingRemoteJSRuntime(ILogger<RemoteJSRuntime> logger)
            : base(
                  Options.Create(new CircuitOptions()),
                  Options.Create(new HubOptions<ComponentHub>()),
                  logger)
        {
        }

        public Task PendingTaskCaptureStarted => _pendingTaskCaptureStarted.Task;

        internal override Action CapturePendingTasksForDisconnect()
        {
            _pendingTaskCaptureStarted.TrySetResult();
            return base.CapturePendingTasksForDisconnect();
        }
    }
}
