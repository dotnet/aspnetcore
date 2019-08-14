// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
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
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();

            var registry = CreateRegistry(circuitIdFactory);
            var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId());
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
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();

            var registry = CreateRegistry(circuitIdFactory);
            var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId());
            registry.RegisterDisconnectedCircuit(circuitHost);

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
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var registry = CreateRegistry(circuitIdFactory);
            var handler = new Mock<CircuitHandler> { CallBase = true };
            var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId(), handlers: new[] { handler.Object });
            registry.RegisterDisconnectedCircuit(circuitHost);

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
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var registry = CreateRegistry(circuitIdFactory);
            var handler = new Mock<CircuitHandler> { CallBase = true };
            var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId(), handlers: new[] { handler.Object });
            registry.Register(circuitHost);

            var newClient = Mock.Of<IClientProxy>();
            var newConnectionId = "new-id";

            // Act
            var result = await registry.ConnectAsync(circuitHost.CircuitId, newClient, newConnectionId, default);

            // Assert
            Assert.NotNull(result);
            handler.Verify(v => v.OnCircuitOpenedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
            handler.Verify(v => v.OnConnectionUpAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Once());
            handler.Verify(v => v.OnConnectionDownAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Once());
            handler.Verify(v => v.OnCircuitClosedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task ConnectAsync_InvokesCircuitHandlers_DisposesCircuitOnFailure()
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var registry = CreateRegistry(circuitIdFactory);
            var handler = new Mock<CircuitHandler> { CallBase = true };
            handler.Setup(h => h.OnConnectionUpAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>())).Throws(new InvalidTimeZoneException());
            var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId(), handlers: new[] { handler.Object });
            registry.Register(circuitHost);

            var newClient = Mock.Of<IClientProxy>();
            var newConnectionId = "new-id";

            // Act
            var result = await registry.ConnectAsync(circuitHost.CircuitId, newClient, newConnectionId, default);

            // Assert
            Assert.Null(result);
            Assert.Null(circuitHost.Handle.CircuitHost); // Will be null if disposed.
            Assert.Empty(registry.ConnectedCircuits);
            Assert.Equal(0, registry.DisconnectedCircuits.Count);
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
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();

            var registry = new TestCircuitRegistry(circuitIdFactory);
            registry.BeforeDisconnect = new ManualResetEventSlim();
            var tcs = new TaskCompletionSource<int>();

            var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId());
            registry.Register(circuitHost);
            var client = Mock.Of<IClientProxy>();
            var newId = "new-connection";

            // Act
            var disconnect = Task.Run(() =>
            {
                var task = registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId);
                tcs.SetResult(0);
                return task;
            });
            var connect = Task.Run(async () =>
            {
                registry.BeforeDisconnect.Set();
                await tcs.Task;
                await registry.ConnectAsync(circuitHost.CircuitId, client, newId, default);
            });
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
        public async Task Connect_WhileDisconnectIsInProgress_SeriallyExecutesCircuitHandlers()
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();

            var registry = new TestCircuitRegistry(circuitIdFactory);
            registry.BeforeDisconnect = new ManualResetEventSlim();
            // This verifies that connection up \ down events on a circuit handler are always invoked serially.
            var circuitHandler = new SerialCircuitHandler();
            var tcs = new TaskCompletionSource<int>();

            var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId(), handlers: new[] { circuitHandler });
            registry.Register(circuitHost);
            var client = Mock.Of<IClientProxy>();
            var newId = "new-connection";

            // Act
            var disconnect = Task.Run(() =>
            {
                var task = registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId);
                tcs.SetResult(0);
                return task;
            });
            var connect = Task.Run(async () =>
            {
                registry.BeforeDisconnect.Set();
                await tcs.Task;
                await registry.ConnectAsync(circuitHost.CircuitId, client, newId, default);
            });
            await Task.WhenAll(disconnect, connect);

            // Assert
            Assert.Single(registry.ConnectedCircuits.Values);
            Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId, out _));

            Assert.True(circuitHandler.OnConnectionDownExecuted, "OnConnectionDownAsync should have been executed.");
            Assert.True(circuitHandler.OnConnectionUpExecuted, "OnConnectionUpAsync should have been executed.");
        }

        [Fact]
        public async Task DisconnectWhenAConnectIsInProgress()
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();

            var registry = new TestCircuitRegistry(circuitIdFactory);
            registry.BeforeConnect = new ManualResetEventSlim();
            var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId());
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

        [Fact]
        public async Task DisconnectedCircuitIsRemovedAfterConfiguredTimeout()
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var circuitOptions = new CircuitOptions
            {
                DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(3),
            };
            var registry = new TestCircuitRegistry(circuitIdFactory, circuitOptions);
            var tcs = new TaskCompletionSource<object>();

            registry.OnAfterEntryEvicted = () =>
            {
                tcs.TrySetResult(new object());
            };
            var circuitHost = TestCircuitHost.Create();

            registry.RegisterDisconnectedCircuit(circuitHost);

            // Act
            // Verify it's present in the dictionary.
            Assert.True(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId, out var _));
            await Task.Run(() => tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10)));
            Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId, out var _));
        }

        [Fact]
        public async Task ReconnectBeforeTimeoutDoesNotGetEntryToBeEvicted()
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var circuitOptions = new CircuitOptions
            {
                DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(8),
            };
            var registry = new TestCircuitRegistry(circuitIdFactory, circuitOptions);
            var tcs = new TaskCompletionSource<object>();

            registry.OnAfterEntryEvicted = () =>
            {
                tcs.TrySetResult(new object());
            };
            var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId());

            registry.RegisterDisconnectedCircuit(circuitHost);
            await registry.ConnectAsync(circuitHost.CircuitId, Mock.Of<IClientProxy>(), "new-connection", default);

            // Act
            await Task.Run(() => tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10)));

            // Verify it's still connected
            Assert.True(registry.ConnectedCircuits.TryGetValue(circuitHost.CircuitId, out var cacheValue));
            Assert.Same(circuitHost, cacheValue);
            // Nothing should be disconnected.
            Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId, out var _));
        }

        private class TestCircuitRegistry : CircuitRegistry
        {
            public TestCircuitRegistry(CircuitIdFactory factory, CircuitOptions circuitOptions = null)
                : base(Options.Create(circuitOptions ?? new CircuitOptions()), NullLogger<CircuitRegistry>.Instance, factory)
            {
            }

            public ManualResetEventSlim BeforeConnect { get; set; }
            public ManualResetEventSlim BeforeDisconnect { get; set; }

            public Action OnAfterEntryEvicted { get; set; }

            protected override (CircuitHost, bool) ConnectCore(string circuitId, IClientProxy clientProxy, string connectionId)
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

            protected override void OnEntryEvicted(object key, object value, EvictionReason reason, object state)
            {
                base.OnEntryEvicted(key, value, reason, state);
                OnAfterEntryEvicted?.Invoke();
            }
        }

        private static CircuitRegistry CreateRegistry(CircuitIdFactory factory = null)
        {
            return new CircuitRegistry(
                Options.Create(new CircuitOptions()),
                NullLogger<CircuitRegistry>.Instance,
                factory ?? TestCircuitIdFactory.CreateTestFactory());
        }

        private class SerialCircuitHandler : CircuitHandler
        {
            private readonly SemaphoreSlim _sempahore = new SemaphoreSlim(1);

            public bool OnConnectionUpExecuted { get; private set; }
            public bool OnConnectionDownExecuted { get; private set; }

            public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
            {
                Assert.True(await _sempahore.WaitAsync(0), "This should be serialized and consequently without contention");
                await Task.Delay(10);

                Assert.False(OnConnectionUpExecuted);
                Assert.True(OnConnectionDownExecuted);
                OnConnectionUpExecuted = true;

                _sempahore.Release();
            }

            public override async Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
            {
                Assert.True(await _sempahore.WaitAsync(0), "This should be serialized and consequently without contention");
                await Task.Delay(10);

                Assert.False(OnConnectionUpExecuted);
                Assert.False(OnConnectionDownExecuted);
                OnConnectionDownExecuted = true;

                _sempahore.Release();
            }
        }
    }
}
