// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Tests.Circuits;

public class HybridCacheCircuitPersistenceProviderTest
{
    [Fact]
    public async Task CanPersistAndRestoreState()
    {
        // Arrange
        var hybridCache = CreateHybridCache();
        var circuitId = TestCircuitIdFactory.CreateTestFactory().CreateCircuitId();
        var persistedState = new PersistedCircuitState()
        {
            RootComponents = [1, 2, 3],
            ApplicationState = new Dictionary<string, byte[]> {
                { "key1", new byte[] { 4, 5, 6 } },
                { "key2", new byte[] { 7, 8, 9 } }
            }
        };
        var provider = CreateProvider(hybridCache);

        // Act
        await provider.PersistCircuitAsync(circuitId, persistedState);

        // Assert
        var result = await provider.RestoreCircuitAsync(circuitId);
        Assert.NotNull(result);
        Assert.Equal(persistedState.RootComponents, result.RootComponents);
        Assert.Equal(persistedState.ApplicationState, result.ApplicationState);
    }

    [Fact]
    public async Task RestoreCircuitAsync_ReturnsNull_WhenCircuitDoesNotExist()
    {
        // Arrange
        var hybridCache = CreateHybridCache();
        var circuitId = TestCircuitIdFactory.CreateTestFactory().CreateCircuitId();
        var provider = CreateProvider(hybridCache);
        var cacheKey = circuitId.Secret;

        // Act
        var result = await provider.RestoreCircuitAsync(circuitId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RestoreCircuitAsync_RemovesCircuitFromCache()
    {
        // Arrange
        var hybridCache = CreateHybridCache();
        var circuitId = TestCircuitIdFactory.CreateTestFactory().CreateCircuitId();
        var persistedState = new PersistedCircuitState()
        {
            RootComponents = [1, 2, 3],
            ApplicationState = new Dictionary<string, byte[]> {
                { "key1", new byte[] { 4, 5, 6 } },
                { "key2", new byte[] { 7, 8, 9 } }
            }
        };

        var provider = CreateProvider(hybridCache);
        var cacheKey = circuitId.Secret;

        await provider.PersistCircuitAsync(circuitId, persistedState);

        // Act
        var result1 = await provider.RestoreCircuitAsync(circuitId);
        var result2 = await provider.RestoreCircuitAsync(circuitId);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(persistedState.RootComponents, result1.RootComponents);
        Assert.Equal(persistedState.ApplicationState, result1.ApplicationState);

        Assert.Null(result2); // Circuit should be removed after first restore
    }

    private HybridCache CreateHybridCache()
    {
        return new ServiceCollection()
            .AddHybridCache().Services
            .BuildServiceProvider()
            .GetRequiredService<HybridCache>();
    }

    private static HybridCacheCircuitPersistenceProvider CreateProvider(
        HybridCache hybridCache,
        CircuitOptions options = null)
    {
        return new HybridCacheCircuitPersistenceProvider(
            hybridCache,
            NullLogger<ICircuitPersistenceProvider>.Instance,
            Options.Create(options ?? new CircuitOptions()));
    }
}
