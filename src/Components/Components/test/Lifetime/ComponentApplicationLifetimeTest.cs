// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Components;

public class ComponentApplicationLifetimeTest
{
    [Theory]
    [InlineData(PersistedStateSerializationMode.Infer)]
    [InlineData(PersistedStateSerializationMode.Server)]
    [InlineData(PersistedStateSerializationMode.WebAssembly)]
    public async Task RestoreStateAsync_InitializesStateWithDataFromTheProvidedStore(PersistedStateSerializationMode serializationMode)
    {
        // Arrange
        var data = new byte[] { 0, 1, 2, 3, 4 };
        var state = new Dictionary<string, byte[]>
        {
            ["MyState"] = JsonSerializer.SerializeToUtf8Bytes(data)
        };
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new TestComponentSerializationModeHandler(serializationMode));

        // Act
        await lifetime.RestoreStateAsync(store);

        // Assert
        Assert.True(lifetime.State.TryTakeFromJson<byte[]>("MyState", out var retrieved));
        Assert.Empty(state);
        Assert.Equal(data, retrieved);
    }

    [Theory]
    [InlineData(PersistedStateSerializationMode.Server)]
    [InlineData(PersistedStateSerializationMode.WebAssembly)]
    public async Task PersistStateAsync_SavesPersistedStateToTheStore(PersistedStateSerializationMode serializationMode)
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new TestComponentSerializationModeHandler(serializationMode));

        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };

        lifetime.State.RegisterOnPersisting(() =>
        {
            lifetime.State.PersistAsJson("MyState", new byte[] { 1, 2, 3, 4 });
            return Task.CompletedTask;
        }, serializationMode);

        // Act
        await lifetime.PersistStateAsync(store, renderer);

        // Assert
        Assert.True(store.State.TryGetValue("MyState", out var persisted));
        Assert.Equal(data, JsonSerializer.Deserialize<byte[]>(persisted.ToArray()));
    }

    [Theory]
    [InlineData(PersistedStateSerializationMode.Server)]
    [InlineData(PersistedStateSerializationMode.WebAssembly)]
    public async Task PersistStateAsync_InvokesPauseCallbacksDuringPersist(PersistedStateSerializationMode serializationMode)
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new TestComponentSerializationModeHandler(serializationMode));
        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };
        var invoked = false;

        lifetime.State.RegisterOnPersisting(() => { invoked = true; return default; }, serializationMode);

        // Act
        await lifetime.PersistStateAsync(store, renderer);

        // Assert
        Assert.True(invoked);
    }

    [Theory]
    [InlineData(PersistedStateSerializationMode.Server)]
    [InlineData(PersistedStateSerializationMode.WebAssembly)]
    public async Task PersistStateAsync_FiresCallbacksInParallel(PersistedStateSerializationMode serializationMode)
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new TestComponentSerializationModeHandler(serializationMode));
        var renderer = new TestRenderer();

        var sequence = new List<int> { };

        var tcs = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        lifetime.State.RegisterOnPersisting(async () => { sequence.Add(1); await tcs.Task; sequence.Add(3); }, serializationMode);
        lifetime.State.RegisterOnPersisting(async () => { sequence.Add(2); await tcs2.Task; sequence.Add(4); }, serializationMode);

        // Act
        var persistTask = lifetime.PersistStateAsync(store, renderer);
        tcs.SetResult();
        tcs2.SetResult();

        await persistTask;

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4 }, sequence);
    }

    [Theory]
    [InlineData(PersistedStateSerializationMode.Server)]
    [InlineData(PersistedStateSerializationMode.WebAssembly)]
    public async Task PersistStateAsync_CallbacksAreRemovedWhenSubscriptionsAreDisposed(PersistedStateSerializationMode serializationMode)
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new TestComponentSerializationModeHandler(serializationMode));
        var renderer = new TestRenderer();

        var sequence = new List<int> { };

        var tcs = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        var subscription1 = lifetime.State.RegisterOnPersisting(async () => { sequence.Add(1); await tcs.Task; sequence.Add(3); }, serializationMode);
        var subscription2 = lifetime.State.RegisterOnPersisting(async () => { sequence.Add(2); await tcs2.Task; sequence.Add(4); }, serializationMode);

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

    [Theory]
    [InlineData(PersistedStateSerializationMode.Server)]
    [InlineData(PersistedStateSerializationMode.WebAssembly)]
    public async Task PersistStateAsync_ContinuesInvokingCallbacksDuringPersistIfACallbackThrows(PersistedStateSerializationMode serializationMode)
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, true);
        var logger = loggerFactory.CreateLogger<ComponentStatePersistenceManager>();
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(logger, new TestComponentSerializationModeHandler(serializationMode));
        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };
        var invoked = false;

        lifetime.State.RegisterOnPersisting(() => throw new InvalidOperationException(), serializationMode);
        lifetime.State.RegisterOnPersisting(() => { invoked = true; return Task.CompletedTask; }, serializationMode);

        // Act
        await lifetime.PersistStateAsync(store, renderer);

        // Assert
        Assert.True(invoked);
        var log = Assert.Single(sink.Writes);
        Assert.Equal(LogLevel.Error, log.LogLevel);
    }

    [Theory]
    [InlineData(PersistedStateSerializationMode.Server)]
    [InlineData(PersistedStateSerializationMode.WebAssembly)]
    public async Task PersistStateAsync_ContinuesInvokingCallbacksDuringPersistIfACallbackThrowsAsynchonously(PersistedStateSerializationMode serializationMode)
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, true);
        var logger = loggerFactory.CreateLogger<ComponentStatePersistenceManager>();
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(logger, new TestComponentSerializationModeHandler(serializationMode));
        var renderer = new TestRenderer();
        var invoked = false;
        var tcs = new TaskCompletionSource();

        lifetime.State.RegisterOnPersisting(async () => { await tcs.Task; throw new InvalidOperationException(); }, serializationMode);
        lifetime.State.RegisterOnPersisting(() => { invoked = true; return Task.CompletedTask; }, serializationMode);

        // Act
        var persistTask = lifetime.PersistStateAsync(store, renderer);
        tcs.SetResult();

        await persistTask;

        // Assert
        Assert.True(invoked);
        var log = Assert.Single(sink.Writes);
        Assert.Equal(LogLevel.Error, log.LogLevel);
    }

    [Theory]
    [InlineData(PersistedStateSerializationMode.Server)]
    [InlineData(PersistedStateSerializationMode.WebAssembly)]
    public async Task PersistStateAsync_ThrowsWhenDeveloperTriesToPersistStateMultipleTimes(PersistedStateSerializationMode serializationMode)
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var lifetime = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new TestComponentSerializationModeHandler(serializationMode));

        var renderer = new TestRenderer();
        var data = new byte[] { 1, 2, 3, 4 };

        lifetime.State.RegisterOnPersisting(() =>
        {
            lifetime.State.PersistAsJson<byte[]>("MyState", new byte[] { 1, 2, 3, 4 });
            return Task.CompletedTask;
        }, serializationMode);

        // Act
        await lifetime.PersistStateAsync(store, renderer);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => lifetime.PersistStateAsync(store, renderer));
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

    private class TestComponentSerializationModeHandler : ISerializationModeHandler
    {
        private PersistedStateSerializationMode _serializationMode;

        public TestComponentSerializationModeHandler(PersistedStateSerializationMode serializationMode)
        {
            _serializationMode = serializationMode;
        }

        public PersistedStateSerializationMode GetCallbackTargetSerializationMode(object callbackTarget)
        {
            return _serializationMode;
        }
    }
}
