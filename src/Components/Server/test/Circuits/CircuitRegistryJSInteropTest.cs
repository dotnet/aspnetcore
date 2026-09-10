// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
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
}
