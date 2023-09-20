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

namespace Microsoft.AspNetCore.Components;

public class ComponentStatePersistenceManagerTest
{
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
        var lifetime = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);

        // Act
        await lifetime.RestoreStateAsync(store);

        // Assert
        Assert.True(lifetime.State.TryTakeFromJson<byte[]>("MyState", out var retrieved));
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
        var lifetime = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);

        await lifetime.RestoreStateAsync(store);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => lifetime.RestoreStateAsync(store));
    }

    [Fact]
    public async Task PersistStateAsync_ThrowsWhenCallbackRenerModeCannotBeInferred()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new CompositeTestStore(state);
        var lifetime = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);

        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };

        lifetime.State.RegisterOnPersisting(() =>
        {
            lifetime.State.PersistAsJson("MyState", new byte[] { 1, 2, 3, 4 });
            return Task.CompletedTask;
        });

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => lifetime.PersistStateAsync(store, renderer));
    }

    [Fact]
    public async Task PersistStateAsync_SavesPersistedStateToTheStore()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);

        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };

        lifetime.State.RegisterOnPersisting(() =>
        {
            lifetime.State.PersistAsJson("MyState", new byte[] { 1, 2, 3, 4 });
            return Task.CompletedTask;
        }, new TestRenderMode());

        // Act
        await lifetime.PersistStateAsync(store, renderer);

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
        var lifetime = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };
        var invoked = false;

        lifetime.State.RegisterOnPersisting(() => { invoked = true; return default; }, new TestRenderMode());

        // Act
        await lifetime.PersistStateAsync(store, renderer);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task PersistStateAsync_FiresCallbacksInParallel()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var renderer = new TestRenderer();

        var sequence = new List<int> { };

        var tcs = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        lifetime.State.RegisterOnPersisting(async () => { sequence.Add(1); await tcs.Task; sequence.Add(3); }, new TestRenderMode());
        lifetime.State.RegisterOnPersisting(async () => { sequence.Add(2); await tcs2.Task; sequence.Add(4); }, new TestRenderMode());

        // Act
        var persistTask = lifetime.PersistStateAsync(store, renderer);
        tcs.SetResult();
        tcs2.SetResult();

        await persistTask;

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4 }, sequence);
    }

    [Fact]
    public async Task PersistStateAsync_CallbacksAreRemovedWhenSubscriptionsAreDisposed()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var renderer = new TestRenderer();

        var sequence = new List<int> { };

        var tcs = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        var subscription1 = lifetime.State.RegisterOnPersisting(async () => { sequence.Add(1); await tcs.Task; sequence.Add(3); });
        var subscription2 = lifetime.State.RegisterOnPersisting(async () => { sequence.Add(2); await tcs2.Task; sequence.Add(4); });

        // Act
        subscription1.Dispose();
        subscription2.Dispose();

        var persistTask = lifetime.PersistStateAsync(store, renderer);
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
        var lifetime = new ComponentStatePersistenceManager(logger);
        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };
        var invoked = false;

        lifetime.State.RegisterOnPersisting(() => throw new InvalidOperationException(), new TestRenderMode());
        lifetime.State.RegisterOnPersisting(() => { invoked = true; return Task.CompletedTask; }, new TestRenderMode());

        // Act
        await lifetime.PersistStateAsync(store, renderer);

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
        var lifetime = new ComponentStatePersistenceManager(logger);
        var renderer = new TestRenderer();
        var invoked = false;
        var tcs = new TaskCompletionSource();

        lifetime.State.RegisterOnPersisting(async () => { await tcs.Task; throw new InvalidOperationException(); }, new TestRenderMode());
        lifetime.State.RegisterOnPersisting(() => { invoked = true; return Task.CompletedTask; }, new TestRenderMode());

        // Act
        var persistTask = lifetime.PersistStateAsync(store, renderer);
        tcs.SetResult();

        await persistTask;

        // Assert
        Assert.True(invoked);
        var log = Assert.Single(sink.Writes);
        Assert.Equal(LogLevel.Error, log.LogLevel);
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

    private class TestStore : IPersistentComponentStateStore
    {
        public TestStore(IDictionary<string, byte[]> initialState)
        {
            State = initialState;
        }

        public IDictionary<string, byte[]> State { get; set; }

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

    private class CompositeTestStore : IPersistentComponentStateStore,  IEnumerable<IPersistentComponentStateStore>
    {
        public CompositeTestStore(IDictionary<string, byte[]> initialState)
        {
            State = initialState;
        }

        public IDictionary<string, byte[]> State { get; set; }

        public IEnumerator<IPersistentComponentStateStore> GetEnumerator()
        {
            yield return new TestStore(State);
            yield return new TestStore(State);
        }

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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class TestRenderMode : IComponentRenderMode
    {

    }
}
