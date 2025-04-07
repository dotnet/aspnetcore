// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.Components;

public class ComponentStatePersistenceManagerTest
{
    [Fact]
    public void Constructor_InitializesPersistentServicesRegistry()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddScoped(sp => new TestStore([]))
            .AddPersistentService<TestStore>(Mock.Of<IComponentRenderMode>())
            .BuildServiceProvider();

        // Act
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            serviceProvider);
        persistenceManager.SetPlatformRenderMode(new TestRenderMode());

        // Assert
        Assert.NotNull(persistenceManager.ServicesRegistry);
        Assert.Empty(persistenceManager.RegisteredCallbacks);
    }

    [Fact]
    public async Task RestoreStateAsync_InitializesStateWithDataFromTheProvidedStore()
    {
        // Arrange
        var data = new byte[] { 0, 1, 2, 3, 4 };
        var state = new Dictionary<string, byte[]>
        {
            ["MyState"] = JsonSerializer.SerializeToUtf8Bytes(data)
        };
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            CreateServiceProvider());

        // Act
        await persistenceManager.RestoreStateAsync(store);

        // Assert
        Assert.True(persistenceManager.State.TryTakeFromJson<byte[]>("MyState", out var retrieved));
        Assert.Empty(state);
        Assert.Equal(data, retrieved);
    }

    [Fact]
    public async Task RestoreStateAsync_ThrowsOnDoubleInitialization()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>
        {
            ["MyState"] = [0, 1, 2, 3, 4]
        };
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            CreateServiceProvider());

        await persistenceManager.RestoreStateAsync(store);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => persistenceManager.RestoreStateAsync(store));
    }

    private IServiceProvider CreateServiceProvider() =>
        new ServiceCollection().BuildServiceProvider();

    [Fact]
    public async Task PersistStateAsync_ThrowsWhenCallbackRenerModeCannotBeInferred()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new CompositeTestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            CreateServiceProvider());

        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };

        persistenceManager.State.RegisterOnPersisting(() =>
        {
            persistenceManager.State.PersistAsJson("MyState", new byte[] { 1, 2, 3, 4 });
            return Task.CompletedTask;
        });

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => persistenceManager.PersistStateAsync(store, renderer));
    }

    [Fact]
    public async Task PersistStateAsync_PersistsRegistry()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddScoped(sp => new TestStore([]))
            .AddPersistentService<TestStore>(new TestRenderMode())
            .BuildServiceProvider();

        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            serviceProvider);
        persistenceManager.SetPlatformRenderMode(new TestRenderMode());
        var testStore = new TestStore([]);

        // Act
        await persistenceManager.PersistStateAsync(testStore, new TestRenderer());

        // Assert
        var persisted = Assert.Single(testStore.State);
        Assert.True(testStore.State.TryGetValue(typeof(PersistentServicesRegistry).FullName, out var registrations));
        var registration = Assert.Single(JsonSerializer.Deserialize<PersistentService[]>(registrations, JsonSerializerOptions.Web));
        Assert.Equal(typeof(TestStore).Assembly.GetName().Name, registration.Assembly);
        Assert.Equal(typeof(TestStore).FullName, registration.FullTypeName);
    }

    [Fact]
    public async Task PersistStateAsync_SavesPersistedStateToTheStore()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            CreateServiceProvider());

        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };

        persistenceManager.State.RegisterOnPersisting(() =>
        {
            persistenceManager.State.PersistAsJson("MyState", new byte[] { 1, 2, 3, 4 });
            return Task.CompletedTask;
        }, new TestRenderMode());

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.True(store.State.TryGetValue("MyState", out var persisted));
        Assert.Equal(data, JsonSerializer.Deserialize<byte[]>(persisted.ToArray()));
    }

    [Fact]
    public async Task PersistStateAsync_InvokesPauseCallbacksDuringPersist()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            CreateServiceProvider());
        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };
        var invoked = false;

        persistenceManager.State.RegisterOnPersisting(() => { invoked = true; return default; }, new TestRenderMode());

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task PersistStateAsync_FiresCallbacksInParallel()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            CreateServiceProvider());
        var renderer = new TestRenderer();

        var sequence = new List<int> { };

        var tcs = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        persistenceManager.State.RegisterOnPersisting(async () => { sequence.Add(1); await tcs.Task; sequence.Add(3); }, new TestRenderMode());
        persistenceManager.State.RegisterOnPersisting(async () => { sequence.Add(2); await tcs2.Task; sequence.Add(4); }, new TestRenderMode());

        // Act
        var persistTask = persistenceManager.PersistStateAsync(store, renderer);
        tcs.SetResult();
        tcs2.SetResult();

        await persistTask;

        // Assert
        Assert.Equal(new[] { 2, 1, 3, 4 }, sequence);
    }

    [Fact]
    public async Task PersistStateAsync_CallbacksAreRemovedWhenSubscriptionsAreDisposed()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            CreateServiceProvider());
        var renderer = new TestRenderer();

        var sequence = new List<int> { };

        var tcs = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        var subscription1 = persistenceManager.State.RegisterOnPersisting(async () => { sequence.Add(1); await tcs.Task; sequence.Add(3); });
        var subscription2 = persistenceManager.State.RegisterOnPersisting(async () => { sequence.Add(2); await tcs2.Task; sequence.Add(4); });

        // Act
        subscription1.Dispose();
        subscription2.Dispose();

        var persistTask = persistenceManager.PersistStateAsync(store, renderer);
        tcs.SetResult();
        tcs2.SetResult();

        await persistTask;

        // Assert
        Assert.Empty(sequence);
    }

    [Fact]
    public async Task PersistStateAsync_ContinuesInvokingPauseCallbacksDuringPersistIfACallbackThrows()
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, true);
        var logger = loggerFactory.CreateLogger<ComponentStatePersistenceManager>();
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            logger,
            CreateServiceProvider());
        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };
        var invoked = false;

        persistenceManager.State.RegisterOnPersisting(() => throw new InvalidOperationException(), new TestRenderMode());
        persistenceManager.State.RegisterOnPersisting(() => { invoked = true; return Task.CompletedTask; }, new TestRenderMode());

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.True(invoked);
        var log = Assert.Single(sink.Writes);
        Assert.Equal(LogLevel.Error, log.LogLevel);
    }

    [Fact]
    public async Task PersistStateAsync_ContinuesInvokingPauseCallbacksDuringPersistIfACallbackThrowsAsynchonously()
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, true);
        var logger = loggerFactory.CreateLogger<ComponentStatePersistenceManager>();
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            logger,
            CreateServiceProvider());
        var renderer = new TestRenderer();
        var invoked = false;
        var tcs = new TaskCompletionSource();

        persistenceManager.State.RegisterOnPersisting(async () => { await tcs.Task; throw new InvalidOperationException(); }, new TestRenderMode());
        persistenceManager.State.RegisterOnPersisting(() => { invoked = true; return Task.CompletedTask; }, new TestRenderMode());

        // Act
        var persistTask = persistenceManager.PersistStateAsync(store, renderer);
        tcs.SetResult();

        await persistTask;

        // Assert
        Assert.True(invoked);
        var log = Assert.Single(sink.Writes);
        Assert.Equal(LogLevel.Error, log.LogLevel);
    }

    [Fact]
    public async Task PersistStateAsync_InvokesAllCallbacksEvenIfACallbackIsRemovedAsPartOfRunningIt()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            CreateServiceProvider());
        var renderer = new TestRenderer();

        var executionSequence = new List<int>();

        persistenceManager.State.RegisterOnPersisting(() =>
        {
            executionSequence.Add(1);
            return Task.CompletedTask;
        }, new TestRenderMode());

        PersistingComponentStateSubscription subscription2 = default;
        subscription2 = persistenceManager.State.RegisterOnPersisting(() =>
        {
            executionSequence.Add(2);
            subscription2.Dispose();
            return Task.CompletedTask;
        }, new TestRenderMode());

        var tcs = new TaskCompletionSource();
        persistenceManager.State.RegisterOnPersisting(async () =>
        {
            executionSequence.Add(3);
            await tcs.Task;
            executionSequence.Add(4);
        }, new TestRenderMode());

        // Act
        var persistTask = persistenceManager.PersistStateAsync(store, renderer);
        tcs.SetResult(); // Allow the async callback to complete
        await persistTask;

        // Assert
        Assert.Contains(3, executionSequence);
        Assert.Contains(2, executionSequence);
        Assert.Contains(1, executionSequence);
        Assert.Contains(4, executionSequence);

        Assert.Equal(4, executionSequence.Count);
    }

    private class TestRenderer : Renderer
    {
        public TestRenderer() : base(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance)
        {
        }

        private readonly Dispatcher _dispatcher = Dispatcher.CreateDefault();

        public override Dispatcher Dispatcher => _dispatcher;

        protected override void HandleException(Exception exception)
        {
            throw new NotImplementedException();
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            throw new NotImplementedException();
        }
    }

    private class TestStore(Dictionary<string, byte[]> initialState) : IPersistentComponentStateStore
    {
        public IDictionary<string, byte[]> State { get; set; } = initialState;

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
        {
            return Task.FromResult(State);
        }

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            // We copy the data here because it's no longer available after this call completes.
            State = state.ToDictionary(k => k.Key, v => v.Value);
            return Task.CompletedTask;
        }
    }

    private class CompositeTestStore(Dictionary<string, byte[]> initialState)
        : IPersistentComponentStateStore, IEnumerable<IPersistentComponentStateStore>
    {
        public Dictionary<string, byte[]> State { get; set; } = initialState;

        public IEnumerator<IPersistentComponentStateStore> GetEnumerator()
        {
            yield return new TestStore(State);
            yield return new TestStore(State);
        }

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
        {
            return Task.FromResult(State as IDictionary<string, byte[]>);
        }

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            // We copy the data here because it's no longer available after this call completes.
            State = state.ToDictionary(k => k.Key, v => v.Value);
            return Task.CompletedTask;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class TestRenderMode : IComponentRenderMode
    {
    }

    private class PersistentService : IPersistentServiceRegistration
    {
        public string Assembly { get; set; }

        public string FullTypeName { get; set; }

        public IComponentRenderMode GetRenderModeOrDefault() => null;
    }
}

static file class ComponentStatePersistenceManagerExtensions
{
    public static IServiceCollection AddPersistentService<TPersistentService>(this IServiceCollection services, IComponentRenderMode renderMode)
    {
        RegisterPersistentComponentStateServiceCollectionExtensions.AddPersistentServiceRegistration<TPersistentService>(
            services,
            renderMode);
        return services;
    }
}
