// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

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

        var newClient = Mock.Of<ISingleClientProxy>();
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

        var newClient = Mock.Of<ISingleClientProxy>();
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

        var newClient = Mock.Of<ISingleClientProxy>();
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

        var newClient = Mock.Of<ISingleClientProxy>();
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

        var newClient = Mock.Of<ISingleClientProxy>();
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
        registry.DisconnectedCircuits.Set(circuitHost.CircuitId.Secret, circuitHost, new MemoryCacheEntryOptions { Size = 1 });

        // Act
        await registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId);

        // Assert
        Assert.Empty(registry.ConnectedCircuits.Values);
        Assert.True(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId.Secret, out _));
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
        var tcs = new TaskCompletionSource();

        var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId());
        registry.Register(circuitHost);
        var client = Mock.Of<ISingleClientProxy>();
        var newId = "new-connection";

        // Act
        var disconnect = Task.Run(() =>
        {
            var task = registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId);
            tcs.SetResult();
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

        Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId.Secret, out _));
    }

    [Fact]
    public async Task Connect_WhilePersistingEvictedCircuit_IsInProgress()
    {
        // Arrange
        var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
        var options = new CircuitOptions
        {
            DisconnectedCircuitMaxRetained = 0, // This will automatically trigger eviction.
        };

        var persistenceCompletionSource = new TaskCompletionSource();
        var circuitPersistenceProvider = new TestCircuitPersistenceProvider()
        {
            Persisting = persistenceCompletionSource.Task,
        };

        var registry = new TestCircuitRegistry(circuitIdFactory, options, circuitPersistenceProvider);
        registry.BeforeDisconnect = new ManualResetEventSlim();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(sp => new ComponentStatePersistenceManager(
            NullLoggerFactory.Instance.CreateLogger<ComponentStatePersistenceManager>(),
            sp));
        serviceCollection.AddSingleton(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId(), serviceProvider.CreateAsyncScope());
        registry.Register(circuitHost);
        var client = Mock.Of<ISingleClientProxy>();
        var newId = "new-connection";

        // Act
        var disconnect = Task.Run(() =>
        {
            var task = registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId);
            return task;
        });

        var connect = Task.Run(async () =>
        {
            var connectCore = registry.ConnectAsync(circuitHost.CircuitId, client, newId, default);
            await connectCore;
        });

        registry.BeforeDisconnect.Set();

        await Task.WhenAll(disconnect, connect);
        persistenceCompletionSource.SetResult();
        circuitPersistenceProvider.AfterPersist.Wait(TimeSpan.FromSeconds(10));
        // Assert
        // We expect the reconnect to fail since the circuit has already been evicted and persisted.
        Assert.Empty(registry.ConnectedCircuits.Values);
        Assert.True(circuitPersistenceProvider.PersistCalled);
        Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId.Secret, out _));
    }

    [Fact]
    public async Task Disconnect_DoesNotPersistCircuits_WithPendingState()
    {
        // Arrange
        var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
        var options = new CircuitOptions
        {
            DisconnectedCircuitMaxRetained = 0, // This will automatically trigger eviction.
        };

        var circuitPersistenceProvider = new TestCircuitPersistenceProvider();

        var registry = new TestCircuitRegistry(circuitIdFactory, options, circuitPersistenceProvider);
        registry.BeforeDisconnect = new ManualResetEventSlim();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(sp => new ComponentStatePersistenceManager(
            NullLoggerFactory.Instance.CreateLogger<ComponentStatePersistenceManager>(),
            sp));
        serviceCollection.AddSingleton(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId(), serviceProvider.CreateAsyncScope());
        registry.Register(circuitHost);
        circuitHost.AttachPersistedState(new PersistedCircuitState());
        var client = Mock.Of<ISingleClientProxy>();
        var newId = "new-connection";

        // Act
        var disconnect = Task.Run(() =>
        {
            var task = registry.DisconnectAsync(circuitHost, circuitHost.Client.ConnectionId);
            return task;
        });

        var connect = Task.Run(async () =>
        {
            var connectCore = registry.ConnectAsync(circuitHost.CircuitId, client, newId, default);
            await connectCore;
        });

        registry.BeforeDisconnect.Set();

        await Task.WhenAll(disconnect, connect);
        circuitPersistenceProvider.AfterPersist.Wait(TimeSpan.FromSeconds(10));

        // Assert
        // We expect the reconnect to fail since the circuit has already been evicted and persisted.
        Assert.Empty(registry.ConnectedCircuits.Values);
        Assert.False(circuitPersistenceProvider.PersistCalled);
        Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId.Secret, out _));
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
        var client = Mock.Of<ISingleClientProxy>();
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

        Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId.Secret, out _));
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
        var tcs = new TaskCompletionSource();

        registry.OnAfterEntryEvicted = () =>
        {
            tcs.TrySetResult();
        };
        var circuitHost = TestCircuitHost.Create();

        registry.RegisterDisconnectedCircuit(circuitHost);

        // Act
        // Verify it's present in the dictionary.
        Assert.True(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId.Secret, out var _));
        await Task.Run(() => tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10)));
        Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId.Secret, out var _));
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
        var tcs = new TaskCompletionSource();

        registry.OnAfterEntryEvicted = () =>
        {
            tcs.TrySetResult();
        };
        var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId());

        registry.RegisterDisconnectedCircuit(circuitHost);
        await registry.ConnectAsync(circuitHost.CircuitId, Mock.Of<ISingleClientProxy>(), "new-connection", default);

        // Act
        await Task.Run(() => tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10)));

        // Verify it's still connected
        Assert.True(registry.ConnectedCircuits.TryGetValue(circuitHost.CircuitId, out var cacheValue));
        Assert.Same(circuitHost, cacheValue);
        // Nothing should be disconnected.
        Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId.Secret, out var _));
    }

    [Fact]
    public async Task PauseCircuitAsync_DoesNothing_IfCircuitIsDisconnected()
    {
        // Arrange
        var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
        var (registry, persistenceProvider) = CreateRegistryWithProvider(circuitIdFactory);
        var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId());

        registry.RegisterDisconnectedCircuit(circuitHost);

        // Act
        await registry.PauseCircuitAsync(circuitHost, circuitHost.Client.ConnectionId);

        // Assert
        Assert.Empty(registry.ConnectedCircuits);
        Assert.True(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId.Secret, out _));
        Assert.False(persistenceProvider.PersistCalled);
    }

    [Fact]
    public async Task ConnectAsync_ReturnsNull_ForPausedCircuit()
    {
        // Arrange
        var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
        var circuitOptions = new CircuitOptions { DisconnectedCircuitMaxRetained = 0 }; // Ensure eviction
        var persistenceProvider = new TestCircuitPersistenceProvider();
        var registry = new TestCircuitRegistry(circuitIdFactory, circuitOptions, persistenceProvider)
        {
            BeforePause = new ManualResetEventSlim(),
            PauseInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
            BeforeConnect = new ManualResetEventSlim(),
        };

        var scope = new ServiceCollection()
                .AddSingleton(sp => new ComponentStatePersistenceManager(NullLoggerFactory.Instance.CreateLogger<ComponentStatePersistenceManager>(), sp))
                .AddSingleton(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State)
                .BuildServiceProvider()
                .CreateAsyncScope();

        var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId(), scope);
        registry.Register(circuitHost);
        var originalConnectionId = circuitHost.Client.ConnectionId;

        var newClient = Mock.Of<ISingleClientProxy>();
        var newConnectionId = originalConnectionId;

        // Act
        var pauseTask = Task.Run(() =>
        {
            var pauseTask = registry.PauseCircuitAsync(circuitHost, originalConnectionId);
            return pauseTask;
        });

        var connectTask = Task.Run(async () =>
        {
            await registry.PauseInvoked.Task; // Wait for PauseCore to be entered and waiting on BeforePauseTcs
            // At this point, PauseCircuitAsync holds the main CircuitRegistryLock and its PauseCore is blocked.
            // ConnectAsync will block on the CircuitRegistryLock until PauseCircuitAsync releases it.
            var connectResultAttempt = registry.ConnectAsync(circuitHost.CircuitId, newClient, newConnectionId, default);
            return await connectResultAttempt;
        });

        await registry.PauseInvoked.Task;
        registry.BeforeConnect.Set();
        registry.BeforePause.Set();

        await Task.WhenAll(pauseTask, connectTask);
        var connectResult = await connectTask;

        // Assert
        Assert.True(persistenceProvider.PersistCalled, "Persistence provider should have been called during pause.");
        Assert.Null(connectResult);
        Assert.False(registry.ConnectedCircuits.ContainsKey(circuitHost.CircuitId), "Circuit should not be in connected circuits.");
        Assert.False(registry.DisconnectedCircuits.TryGetValue(circuitHost.CircuitId.Secret, out _), "Circuit should be evicted from disconnected circuits.");
    }

    [Fact]
    public async Task PauseCircuitAsync_DoesNothing_IfConnectionIdIsDifferent()
    {
        // Arrange
        var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
        var (registry, persistenceProvider) = CreateRegistryWithProvider(circuitIdFactory);
        var circuitHost = TestCircuitHost.Create(circuitIdFactory.CreateCircuitId());
        registry.Register(circuitHost);
        var differentConnectionId = "different-connection-id";
        Assert.NotEqual(differentConnectionId, circuitHost.Client.ConnectionId);

        // Act
        await registry.PauseCircuitAsync(circuitHost, differentConnectionId);

        // Assert
        Assert.True(registry.ConnectedCircuits.TryGetValue(circuitHost.CircuitId, out var connectedCircuit));
        Assert.Same(circuitHost, connectedCircuit);
        Assert.False(persistenceProvider.PersistCalled);
    }

    private class TestCircuitRegistry : CircuitRegistry
    {
        public TestCircuitRegistry(
            CircuitIdFactory factory,
            CircuitOptions circuitOptions = null,
            TestCircuitPersistenceProvider persistenceProvider = null)
            : base(
                  Options.Create(circuitOptions ?? new CircuitOptions()),
                  NullLogger<CircuitRegistry>.Instance,
                  factory,
                  CreatePersistenceManager(circuitOptions ?? new CircuitOptions(), persistenceProvider ?? new TestCircuitPersistenceProvider()))
        {
        }

        public ManualResetEventSlim BeforeConnect { get; set; }
        public ManualResetEventSlim BeforeDisconnect { get; set; }
        public ManualResetEventSlim BeforePause { get; set; }

        public Action OnAfterEntryEvicted { get; set; }
        public TaskCompletionSource PauseInvoked { get; internal set; }

        protected override (CircuitHost, bool) ConnectCore(CircuitId circuitId, ISingleClientProxy clientProxy, string connectionId)
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

        // In the actual CircuitRegistry, PauseCore is not virtual. We simulate its behavior for testing concurrency.
        // This method will be called by PauseCircuitAsync in TestCircuitRegistry due to normal method resolution.
        internal override Task PauseCore(CircuitHost circuitHost, string connectionId)
        {
            PauseInvoked.SetResult();
            if (BeforePause != null)
            {
                Assert.True(BeforePause.Wait(TimeSpan.FromSeconds(10)), "BeforePauseTcs failed to be set");
            }
            var result = base.PauseCore(circuitHost, connectionId);
            return result;
        }

        protected override void OnEntryEvicted(object key, object value, EvictionReason reason, object state)
        {
            base.OnEntryEvicted(key, value, reason, state);
            OnAfterEntryEvicted?.Invoke();
        }
    }

    private class TestCircuitPersistenceProvider : ICircuitPersistenceProvider
    {
        public Task Persisting { get; set; }
        public ManualResetEventSlim AfterPersist { get; set; } = new ManualResetEventSlim();
        public bool PersistCalled { get; internal set; }

        public async Task PersistCircuitAsync(CircuitId circuitId, PersistedCircuitState persistedCircuitState, CancellationToken cancellation = default)
        {
            PersistCalled = true;
            if (Persisting != null)
            {
                await Persisting;
            }
            AfterPersist.Set();
        }

        public Task<PersistedCircuitState> RestoreCircuitAsync(CircuitId circuitId, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }
    }

    private static CircuitPersistenceManager CreatePersistenceManager(
        CircuitOptions circuitOptions,
        TestCircuitPersistenceProvider persistenceProvider)
    {
        var manager = new CircuitPersistenceManager(
            Options.Create(circuitOptions),
            new Endpoints.ServerComponentSerializer(new EphemeralDataProtectionProvider()),
            persistenceProvider, // Ensure the passed provider is used
            new EphemeralDataProtectionProvider());

        return manager;
    }

    private static CircuitRegistry CreateRegistry(CircuitIdFactory factory = null)
    {
        return new CircuitRegistry(
            Options.Create(new CircuitOptions()),
            NullLogger<CircuitRegistry>.Instance,
            factory ?? TestCircuitIdFactory.CreateTestFactory(),
            CreatePersistenceManager(new CircuitOptions(), new TestCircuitPersistenceProvider()));
    }

    private static (CircuitRegistry Registry, TestCircuitPersistenceProvider Provider) CreateRegistryWithProvider(CircuitIdFactory factory = null, CircuitOptions circuitOptions = null)
    {
        var options = circuitOptions ?? new CircuitOptions();
        var provider = new TestCircuitPersistenceProvider();
        var persistenceManager = CreatePersistenceManager(options, provider);
        var registry = new CircuitRegistry(
            Options.Create(options),
            NullLogger<CircuitRegistry>.Instance,
            factory ?? TestCircuitIdFactory.CreateTestFactory(),
            persistenceManager);
        return (registry, provider);
    }
}
