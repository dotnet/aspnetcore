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
        registry.Register(circuitHost);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pendingInteropCall = circuitHost.JSRuntime.InvokeAsync<string>("test", cts.Token, Array.Empty<object>());

        await registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId);

        await Assert.ThrowsAsync<JSDisconnectedException>(async () => await pendingInteropCall);
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
