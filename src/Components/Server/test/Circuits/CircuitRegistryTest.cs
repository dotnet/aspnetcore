// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    public class CircuitRegistryTest
    {
        [Fact]
        public void Register_AddsCircuit()
        {
            // Arrange
            var registry = CreateRegistry();
            var circuitHost = TestCircuitHost.Create();

            // Act
            registry.Register(circuitHost);

            // Assert
            var actual = Assert.Single(registry.ConnectedCircuits.Values);
            Assert.Same(circuitHost, actual);
        }

        [Fact]
        public async Task ConnectAsync_TransfersClientOnActiveCircuit()
        {
            // Arrange
            var registry = CreateRegistry();
            var circuitHost = TestCircuitHost.Create();
            registry.Register(circuitHost);

            var newClient = Mock.Of<IClientProxy>();
            var newConnectionId = "new-id";

            // Act
            var result = await registry.ConnectAsync(circuitHost.CircuitId, newClient, newConnectionId, default);

            // Assert
            Assert.Same(circuitHost, result);
            Assert.Same(newClient, circuitHost.Client.Client);
            Assert.Same(newConnectionId, circuitHost.Client.ConnectionId);

            var actual = Assert.Single(registry.ConnectedCircuits.Values);
            Assert.Same(circuitHost, actual);
        }

        [Fact]
        public async Task ConnectAsync_MakesInactiveCircuitActive()
        {
            // Arrange
            var registry = CreateRegistry();
            var circuitHost = TestCircuitHost.Create();
            registry.DisconnectedCircuits.Set(circuitHost.CircuitId, circuitHost, new MemoryCacheEntryOptions { Size = 1 });

            var newClient = Mock.Of<IClientProxy>();
            var newConnectionId = "new-id";

            // Act
            var result = await registry.ConnectAsync(circuitHost.CircuitId, newClient, newConnectionId, default);

            // Assert
            Assert.Same(circuitHost, result);
            Assert.Same(newClient, circuitHost.Client.Client);
            Assert.Same(newConnectionId, circuitHost.Client.ConnectionId);

            var actual = Assert.Single(registry.ConnectedCircuits.Values);
            Assert.Same(circuitHost, actual);
            Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId, out _));
        }

        [Fact]
        public async Task ConnectAsync_InvokesCircuitHandlers_WhenCircuitWasPreviouslyDisconnected()
        {
            // Arrange
            var registry = CreateRegistry();
            var handler = new Mock<CircuitHandler> { CallBase = true };
            var circuitHost = TestCircuitHost.Create(handlers: new[] { handler.Object });
            registry.DisconnectedCircuits.Set(circuitHost.CircuitId, circuitHost, new MemoryCacheEntryOptions { Size = 1 });

            var newClient = Mock.Of<IClientProxy>();
            var newConnectionId = "new-id";

            // Act
            var result = await registry.ConnectAsync(circuitHost.CircuitId, newClient, newConnectionId, default);

            // Assert
            Assert.NotNull(result);
            handler.Verify(v => v.OnCircuitOpenedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnConnectionUpAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Once());
            handler.Verify(v => v.OnConnectionDownAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnCircuitClosedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task ConnectAsync_InvokesCircuitHandlers_WhenCircuitWasConsideredConnected()
        {
            // Arrange
            var registry = CreateRegistry();
            var handler = new Mock<CircuitHandler> { CallBase = true };
            var circuitHost = TestCircuitHost.Create(handlers: new[] { handler.Object });
            registry.Register(circuitHost);

            var newClient = Mock.Of<IClientProxy>();
            var newConnectionId = "new-id";

            // Act
            var result = await registry.ConnectAsync(circuitHost.CircuitId, newClient, newConnectionId, default);

            // Assert
            Assert.NotNull(result);
            handler.Verify(v => v.OnCircuitOpenedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnConnectionUpAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Once());
            handler.Verify(v => v.OnConnectionDownAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnCircuitClosedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task DisconnectAsync_DoesNothing_IfCircuitIsInactive()
        {
            // Arrange
            var registry = CreateRegistry();
            var circuitHost = TestCircuitHost.Create();
            registry.DisconnectedCircuits.Set(circuitHost.CircuitId, circuitHost, new MemoryCacheEntryOptions { Size = 1 });

            // Act
            await registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId);

            // Assert
            Assert.Empty(registry.ConnectedCircuits.Values);
            Assert.True(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId, out _));
        }

        [Fact]
        public async Task DisconnectAsync_InvokesCircuitHandlers_WhenCircuitWasDisconnected()
        {
            // Arrange
            var registry = CreateRegistry();
            var handler = new Mock<CircuitHandler> { CallBase = true };
            var circuitHost = TestCircuitHost.Create(handlers: new[] { handler.Object });
            registry.Register(circuitHost);

            // Act
            await registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId);

            // Assert
            handler.Verify(v => v.OnCircuitOpenedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnConnectionUpAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnConnectionDownAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Once());
            handler.Verify(v => v.OnCircuitClosedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task DisconnectAsync_DoesNotInvokeCircuitHandlers_WhenCircuitReconnected()
        {
            // Arrange
            var registry = CreateRegistry();
            var handler = new Mock<CircuitHandler> { CallBase = true };
            var circuitHost = TestCircuitHost.Create(handlers: new[] { handler.Object });
            registry.Register(circuitHost);

            // Act
            await registry.DisconnectAsync(circuitHost, "old-connection");

            // Assert
            handler.Verify(v => v.OnCircuitOpenedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnConnectionUpAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnConnectionDownAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnCircuitClosedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task DisconnectAsync_DoesNotInvokeCircuitHandlers_WhenCircuitWasNotFound()
        {
            // Arrange
            var registry = CreateRegistry();
            var handler = new Mock<CircuitHandler> { CallBase = true };
            var circuitHost = TestCircuitHost.Create(handlers: new[] { handler.Object });

            // Act
            await registry.DisconnectAsync(circuitHost, "old-connection");

            // Assert
            handler.Verify(v => v.OnCircuitOpenedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnConnectionUpAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnConnectionDownAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnCircuitClosedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task Connect_WhileDisconnectIsInProgress()
        {
            // Arrange
            var registry = new TestCircuitRegistry();
            registry.BeforeDisconnect = new ManualResetEventSlim();
            var circuitHost = TestCircuitHost.Create();
            registry.Register(circuitHost);
            var client = Mock.Of<IClientProxy>();
            var newId = "new-connection";

            // Act
            var disconnect = Task.Run(() => registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId));
            var connect = Task.Run(() => registry.ConnectAsync(circuitHost.CircuitId, client, newId, default));
            registry.BeforeDisconnect.Set();
            await Task.WhenAll(disconnect, connect);

            // Assert
            // We expect the disconnect to finish followed by a reconnect
            var actual = Assert.Single(registry.ConnectedCircuits.Values);
            Assert.Same(circuitHost, actual);
            Assert.Same(client, circuitHost.Client.Client);
            Assert.Equal(newId, circuitHost.Client.ConnectionId);

            Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId, out _));
        }

        [Fact]
        public async Task DisconnectWhenAConnectIsInProgress()
        {
            // Arrange
            var registry = new TestCircuitRegistry();
            registry.BeforeConnect = new ManualResetEventSlim();
            var circuitHost = TestCircuitHost.Create();
            registry.Register(circuitHost);
            var client = Mock.Of<IClientProxy>();
            var oldId = circuitHost.Client.ConnectionId;
            var newId = "new-connection";

            // Act
            var connect = Task.Run(() => registry.ConnectAsync(circuitHost.CircuitId, client, newId, default));
            var disconnect = Task.Run(() => registry.DisconnectAsync(circuitHost, oldId));
            registry.BeforeConnect.Set();
            await Task.WhenAll(connect, disconnect);

            // Assert
            // We expect the disconnect to fail since the client identifier has changed.
            var actual = Assert.Single(registry.ConnectedCircuits.Values);
            Assert.Same(circuitHost, actual);
            Assert.Same(client, circuitHost.Client.Client);
            Assert.Equal(newId, circuitHost.Client.ConnectionId);

            Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId, out _));
        }

        private class TestCircuitRegistry : CircuitRegistry
        {
            public TestCircuitRegistry()
                : base(Options.Create(new CircuitOptions()), NullLogger<CircuitRegistry>.Instance)
            {
            }

            public ManualResetEventSlim BeforeConnect { get; set; }
            public ManualResetEventSlim BeforeDisconnect { get; set; }

            protected override CircuitHost ConnectCore(string circuitId, IClientProxy clientProxy, string connectionId)
            {
                if (BeforeConnect != null)
                {
                    Assert.True(BeforeConnect?.Wait(TimeSpan.FromSeconds(10)), "BeforeConnect failed to be set");
                }

                return base.ConnectCore(circuitId, clientProxy, connectionId);
            }

            protected override bool DisconnectCore(CircuitHost circuitHost, string connectionId)
            {
                if (BeforeDisconnect != null)
                {
                    Assert.True(BeforeDisconnect?.Wait(TimeSpan.FromSeconds(10)), "BeforeDisconnect failed to be set");
                }

                return base.DisconnectCore(circuitHost, connectionId);
            }
        }

        private static CircuitRegistry CreateRegistry()
        {
            return new CircuitRegistry(
                Options.Create(new CircuitOptions()),
                NullLogger<CircuitRegistry>.Instance);
        }

    }
}
