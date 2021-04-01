// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Lifetime;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public class ComponentApplicationLifetimeTest
    {
        [Fact]
        public async Task RestoreStateAsync_InitializesStateWithDataFromTheProvidedStore()
        {
            // Arrange
            byte[] data = new byte[] { 0, 1, 2, 3, 4 };
            var state = new Dictionary<string, byte[]>
            {
                ["MyState"] = data
            };
            var store = new TestStore(state);
            var lifetime = new ComponentApplicationLifetime(NullLogger<ComponentApplicationLifetime>.Instance);

            // Act
            await lifetime.RestoreStateAsync(store);

            // Assert
            Assert.True(lifetime.State.TryTakePersistedState("MyState", out var retrieved));
            Assert.Empty(state);
            Assert.Equal(data, retrieved);
        }

        [Fact]
        public async Task RestoreStateAsync_ThrowsOnDoubleInitialization()
        {
            // Arrange
            var state = new Dictionary<string, byte[]>
            {
                ["MyState"] = new byte[] { 0, 1, 2, 3, 4 }
            };
            var store = new TestStore(state);
            var lifetime = new ComponentApplicationLifetime(NullLogger<ComponentApplicationLifetime>.Instance);

            await lifetime.RestoreStateAsync(store);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => lifetime.RestoreStateAsync(store));
        }

        [Fact]
        public async Task PersistStateAsync_SavesPersistedStateToTheStore()
        {
            // Arrange
            var state = new Dictionary<string, byte[]>();
            var store = new TestStore(state);
            var lifetime = new ComponentApplicationLifetime(NullLogger<ComponentApplicationLifetime>.Instance);

            var renderer = new TestRenderer();
            var data = new byte[] { 1, 2, 3, 4 };

            lifetime.State.PersistState("MyState", new byte[] { 1, 2, 3, 4 });

            // Act
            await lifetime.PersistStateAsync(store, renderer);

            // Assert
            Assert.True(store.State.TryGetValue("MyState", out var persisted));
            Assert.Equal(data, persisted);
        }

        [Fact]
        public async Task PersistStateAsync_InvokesPauseCallbacksDuringPersist()
        {
            // Arrange
            var state = new Dictionary<string, byte[]>();
            var store = new TestStore(state);
            var lifetime = new ComponentApplicationLifetime(NullLogger<ComponentApplicationLifetime>.Instance);
            var renderer = new TestRenderer();
            var data = new byte[] { 1, 2, 3, 4 };
            var invoked = false;

            lifetime.State.OnPersisting += () => { invoked = true; return default; };

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
            var lifetime = new ComponentApplicationLifetime(NullLogger<ComponentApplicationLifetime>.Instance);
            var renderer = new TestRenderer();

            var sequence = new List<int> { };

            var tcs = new TaskCompletionSource();
            var tcs2 = new TaskCompletionSource();

            lifetime.State.OnPersisting += async () => { sequence.Add(1); await tcs.Task; sequence.Add(3); };
            lifetime.State.OnPersisting += async () => { sequence.Add(2); await tcs2.Task; sequence.Add(4); };

            // Act
            var persistTask = lifetime.PersistStateAsync(store, renderer);
            tcs.SetResult();
            tcs2.SetResult();

            await persistTask;

            // Assert
            Assert.Equal(new[] { 1, 2, 3, 4 }, sequence);
        }

        [Fact]
        public async Task PersistStateAsync_ContinuesInvokingPauseCallbacksDuringPersistIfACallbackThrows()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, true);
            var logger = loggerFactory.CreateLogger<ComponentApplicationLifetime>();
            var state = new Dictionary<string, byte[]>();
            var store = new TestStore(state);
            var lifetime = new ComponentApplicationLifetime(logger);
            var renderer = new TestRenderer();
            var data = new byte[] { 1, 2, 3, 4 };
            var invoked = false;

            lifetime.State.OnPersisting += () => throw new InvalidOperationException();
            lifetime.State.OnPersisting += () => { invoked = true; return Task.CompletedTask; };

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
            var logger = loggerFactory.CreateLogger<ComponentApplicationLifetime>();
            var state = new Dictionary<string, byte[]>();
            var store = new TestStore(state);
            var lifetime = new ComponentApplicationLifetime(logger);
            var renderer = new TestRenderer();
            var invoked = false;
            var tcs = new TaskCompletionSource();

            lifetime.State.OnPersisting += async () => { await tcs.Task; throw new InvalidOperationException(); };
            lifetime.State.OnPersisting += () => { invoked = true; return Task.CompletedTask; };

            // Act
            var persistTask = lifetime.PersistStateAsync(store, renderer);
            tcs.SetResult();

            await persistTask;

            // Assert
            Assert.True(invoked);
            var log = Assert.Single(sink.Writes);
            Assert.Equal(LogLevel.Error, log.LogLevel);
        }

        [Fact]
        public async Task PersistStateAsync_ThrowsWhenDeveloperTriesToPersistStateMultipleTimes()
        {
            // Arrange
            var state = new Dictionary<string, byte[]>();
            var store = new TestStore(state);
            var lifetime = new ComponentApplicationLifetime(NullLogger<ComponentApplicationLifetime>.Instance);

            var renderer = new TestRenderer();
            var data = new byte[] { 1, 2, 3, 4 };

            lifetime.State.PersistState("MyState", new byte[] { 1, 2, 3, 4 });

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

            private Dispatcher _dispatcher = Dispatcher.CreateDefault();

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

        private class TestStore : IComponentApplicationStateStore
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
                State = new Dictionary<string, byte[]>(state);
                return Task.CompletedTask;
            }
        }
    }
}
