// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.Tests.Circuits;

public class DefaultInMemoryCircuitPersistenceProviderTest
{
    [Fact]
    public async Task PersistCircuitAsync_StoresCircuitState()
    {
        // Arrange
        var clock = new TestSystemClock();
        var circuitId = TestCircuitIdFactory.CreateTestFactory().CreateCircuitId();
        var persistedState = new PersistedCircuitState();
        var provider = CreateProvider(clock);

        // Act
        await provider.PersistCircuitAsync(circuitId, persistedState);

        // Assert
        var result = await provider.RestoreCircuitAsync(circuitId);
        Assert.Same(persistedState, result);
    }

    [Fact]
    public async Task RestoreCircuitAsync_ReturnsNull_WhenCircuitDoesNotExist()
    {
        // Arrange
        var clock = new TestSystemClock();
        var circuitId = TestCircuitIdFactory.CreateTestFactory().CreateCircuitId();
        var provider = CreateProvider(clock);

        // Act
        var result = await provider.RestoreCircuitAsync(circuitId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RestoreCircuitAsync_RemovesCircuitFromCache()
    {
        // Arrange
        var clock = new TestSystemClock();
        var circuitId = TestCircuitIdFactory.CreateTestFactory().CreateCircuitId();
        var persistedState = new PersistedCircuitState();
        var provider = CreateProvider(clock);

        await provider.PersistCircuitAsync(circuitId, persistedState);

        // Act
        var firstResult = await provider.RestoreCircuitAsync(circuitId);
        var secondResult = await provider.RestoreCircuitAsync(circuitId);

        // Assert
        Assert.Same(persistedState, firstResult);
        Assert.Null(secondResult); // Second attempt should return null as the entry should be removed
    }

    [Fact]
    public async Task CircuitStateIsEvictedAfterConfiguredTimeout()
    {
        // Arrange
        var clock = new TestSystemClock();
        var circuitOptions = new CircuitOptions
        {
            PersistedCircuitInMemoryRetentionPeriod = TimeSpan.FromSeconds(2)
        };
        var circuitId = TestCircuitIdFactory.CreateTestFactory().CreateCircuitId();
        var persistedState = new PersistedCircuitState();
        var provider = CreateProvider(clock, circuitOptions);
        var postEvictionCallback = provider.PostEvictionCallback.EvictionCallback;
        var callbackRan = new TaskCompletionSource();
        provider.PostEvictionCallback = new PostEvictionCallbackRegistration
        {
            EvictionCallback = (key, value, reason, state) =>
            {
                callbackRan.SetResult();
                postEvictionCallback(key, value, reason, state);
            }
        };

        await provider.PersistCircuitAsync(circuitId, persistedState);

        // Act - advance the clock past the retention period
        clock.UtcNow = clock.UtcNow.AddSeconds(2);

        // Allow time for the timer to fire and the eviction to occur
        await callbackRan.Task;

        // Assert
        var result = await provider.RestoreCircuitAsync(circuitId);
        Assert.Null(result);
    }

    [Fact]
    public async Task CircuitStatesAreLimitedByConfiguredCapacity()
    {
        // Arrange
        var clock = new TestSystemClock();
        var circuitOptions = new CircuitOptions
        {
            PersistedCircuitInMemoryMaxRetained = 2 // Only allow 2 circuits to be stored
        };
        var provider = CreateProvider(clock, circuitOptions);
        var factory = TestCircuitIdFactory.CreateTestFactory();

        var evictedKeys = new List<string>();
        var evictionTcs = new TaskCompletionSource();
        var postEvictionCallback = provider.PostEvictionCallback.EvictionCallback;
        provider.PostEvictionCallback = new PostEvictionCallbackRegistration
        {
            EvictionCallback = (key, value, reason, state) =>
            {
                evictedKeys.Add((string)key);
                evictionTcs.TrySetResult();
                postEvictionCallback(key, value, reason, state);
            }
        };

        var circuitId1 = factory.CreateCircuitId();
        var circuitId2 = factory.CreateCircuitId();
        var circuitId3 = factory.CreateCircuitId();
        var circuitIds = new Dictionary<string, CircuitId>
        {
            [circuitId1.Secret] = circuitId1,
            [circuitId2.Secret] = circuitId2,
            [circuitId3.Secret] = circuitId3
        };

        var state1 = new PersistedCircuitState();
        var state2 = new PersistedCircuitState();
        var state3 = new PersistedCircuitState();

        // Act - persist 3 circuits when capacity is 2
        await provider.PersistCircuitAsync(circuitId1, state1);
        await provider.PersistCircuitAsync(circuitId2, state2);
        await provider.PersistCircuitAsync(circuitId3, state3);

        // Wait for eviction to occur
        await evictionTcs.Task;

        // Assert
        var evicted = Assert.Single(evictedKeys);
        var evictedId = circuitIds[evicted];

        circuitIds.Remove(evicted);

        var evictedResults = await provider.RestoreCircuitAsync(evictedId);
        Assert.Null(evictedResults);

        var nonEvictedResults = await Task.WhenAll(circuitIds.Select(ne => provider.RestoreCircuitAsync(ne.Value)));

        Assert.Collection(nonEvictedResults,
            Assert.NotNull,
            Assert.NotNull);
    }

    private static DefaultInMemoryCircuitPersistenceProvider CreateProvider(
        ISystemClock clock,
        CircuitOptions options = null)
    {
        return new DefaultInMemoryCircuitPersistenceProvider(
            clock,
            NullLogger<ICircuitPersistenceProvider>.Instance,
            Options.Create(options ?? new CircuitOptions()));
    }

    private class TestSystemClock : ISystemClock
    {
        public TestSystemClock()
        {
            UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }

        public DateTimeOffset UtcNow { get; set; }
    }
}
