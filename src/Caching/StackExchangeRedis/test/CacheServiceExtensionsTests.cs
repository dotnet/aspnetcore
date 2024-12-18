// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

public class CacheServiceExtensionsTests
{
    [Fact]
    public void AddStackExchangeRedisCache_RegistersDistributedCacheAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStackExchangeRedisCache(options => { });

        // Assert
        var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

        Assert.NotNull(distributedCache);
        Assert.Equal(ServiceLifetime.Singleton, distributedCache.Lifetime);
    }

    [Fact]
    public void AddStackExchangeRedisCache_ReplacesPreviouslyUserRegisteredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped(typeof(IDistributedCache), sp => Mock.Of<IDistributedCache>());

        // Act
        services.AddStackExchangeRedisCache(options => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

        Assert.NotNull(distributedCache);
        Assert.Equal(ServiceLifetime.Scoped, distributedCache.Lifetime);
        Assert.IsAssignableFrom<RedisCache>(serviceProvider.GetRequiredService<IDistributedCache>());
    }

    [Fact]
    public void AddStackExchangeRedisCache_allows_chaining()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddStackExchangeRedisCache(_ => { }));
    }

    [Fact]
    public void AddStackExchangeRedisCache_IDistributedCacheWithoutLoggingCanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStackExchangeRedisCache(options => { });

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();

        Assert.NotNull(distributedCache);
    }

    [Fact]
    public void AddStackExchangeRedisCache_IDistributedCacheWithLoggingCanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStackExchangeRedisCache(options => { });
        services.AddLogging();

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();

        Assert.NotNull(distributedCache);
    }

    [Fact]
    public void AddStackExchangeRedisCache_UsesLoggerFactoryAlreadyRegisteredWithServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped(typeof(IDistributedCache), sp => Mock.Of<IDistributedCache>());

        var loggerFactory = new Mock<ILoggerFactory>();

        loggerFactory
            .Setup(lf => lf.CreateLogger(It.IsAny<string>()))
            .Returns((string name) => NullLoggerFactory.Instance.CreateLogger(name))
            .Verifiable();

        services.AddScoped(typeof(ILoggerFactory), _ => loggerFactory.Object);

        // Act
        services.AddLogging();
        services.AddStackExchangeRedisCache(options => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

        Assert.NotNull(distributedCache);
        Assert.Equal(ServiceLifetime.Scoped, distributedCache.Lifetime);
        Assert.IsAssignableFrom<RedisCache>(serviceProvider.GetRequiredService<IDistributedCache>());

        loggerFactory.Verify();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddStackExchangeRedisCache_HybridCacheDetected(bool hybridCacheActive)
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddLogging();

        // Act
        services.AddStackExchangeRedisCache(options => { });
        if (hybridCacheActive)
        {
            services.TryAddSingleton<HybridCache>(new DummyHybridCache());
        }

        using var provider = services.BuildServiceProvider();
        var cache = Assert.IsAssignableFrom<RedisCache>(provider.GetRequiredService<IDistributedCache>());
        Assert.Equal(hybridCacheActive, cache.HybridCacheActive);
    }

    sealed class DummyHybridCache : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions options = null, IEnumerable<string> tags = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions options = null, IEnumerable<string> tags = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
