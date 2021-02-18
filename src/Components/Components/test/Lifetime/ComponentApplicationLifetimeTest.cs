// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
            var lifetime = new ComponentApplicationLifetime();

            // Act
            await lifetime.RestoreStateAsync(store);

            // Assert
            Assert.True(lifetime.State.TryRetrievePersistedState("MyState", out var retrieved));
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
            var lifetime = new ComponentApplicationLifetime();

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
            var lifetime = new ComponentApplicationLifetime();

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
            var lifetime = new ComponentApplicationLifetime();
            var renderer = new TestRenderer();
            var data = new byte[] { 1, 2, 3, 4 };
            var invoked = false;

            lifetime.State.RegisterOnPersistingCallback(() => { invoked = true; return Task.CompletedTask; });

            // Act
            await lifetime.PersistStateAsync(store, renderer);

            // Assert
            Assert.True(invoked);
        }

        [Fact]
        public async Task PersistStateAsync_ThrowsWhenDeveloperTriesToPersistStateMultipleTimes()
        {
            // Arrange
            var state = new Dictionary<string, byte[]>();
            var store = new TestStore(state);
            var lifetime = new ComponentApplicationLifetime();

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
