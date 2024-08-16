// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS8769 // Nullability of reference types in type of parameter doesn't match implemented member (possibly because of nullability attributes).

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class ServiceConstructionTests
{
    [Fact]
    public void CanCreateDefaultService()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        Assert.IsType<DefaultHybridCache>(provider.GetService<HybridCache>());
    }

    [Fact]
    public void CanCreateServiceWithManualOptions()
    {
        var services = new ServiceCollection();
        services.AddHybridCache(options =>
        {
            options.MaximumKeyLength = 937;
            options.DefaultEntryOptions = new() { Expiration = TimeSpan.FromSeconds(120), Flags = HybridCacheEntryFlags.DisableLocalCacheRead };
        });
        using var provider = services.BuildServiceProvider();
        var obj = Assert.IsType<DefaultHybridCache>(provider.GetService<HybridCache>());
        var options = obj.Options;
        Assert.Equal(937, options.MaximumKeyLength);
        var defaults = options.DefaultEntryOptions;
        Assert.NotNull(defaults);
        Assert.Equal(TimeSpan.FromSeconds(120), defaults.Expiration);
        Assert.Equal(HybridCacheEntryFlags.DisableLocalCacheRead, defaults.Flags);
        Assert.Null(defaults.LocalCacheExpiration); // wasn't specified
    }

    [Fact]
    public void CanParseOptions_NoEntryOptions()
    {
        var source = new JsonConfigurationSource { Path = "BasicConfig.json" };
        var configBuilder = new ConfigurationBuilder { Sources = { source } };
        var config = configBuilder.Build();
        var options = new HybridCacheOptions();
        ConfigurationBinder.Bind(config, "no_entry_options", options);

        Assert.Equal(937, options.MaximumKeyLength);
        Assert.Null(options.DefaultEntryOptions);
    }
    [Fact]
    public void CanParseOptions_WithEntryOptions() // in particular, check we can parse the timespan and [Flags] enums
    {
        var source = new JsonConfigurationSource { Path = "BasicConfig.json" };
        var configBuilder = new ConfigurationBuilder { Sources = { source } };
        var config = configBuilder.Build();
        var options = new HybridCacheOptions();
        ConfigurationBinder.Bind(config, "with_entry_options", options);

        Assert.Equal(937, options.MaximumKeyLength);
        var defaults = options.DefaultEntryOptions;
        Assert.NotNull(defaults);
        Assert.Equal(HybridCacheEntryFlags.DisableCompression | HybridCacheEntryFlags.DisableLocalCacheRead, defaults.Flags);
        Assert.Equal(TimeSpan.FromSeconds(120), defaults.LocalCacheExpiration);
        Assert.Null(defaults.Expiration); // wasn't specified
    }

    [Fact]
    public async Task BasicStatelessUsage()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        var expected = Guid.NewGuid().ToString();
        var actual = await cache.GetOrCreateAsync(Me(), async _ => expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task BasicStatefulUsage()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        var expected = Guid.NewGuid().ToString();
        var actual = await cache.GetOrCreateAsync(Me(), expected, async (state, _) => state);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DefaultSerializerConfiguration()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.IsType<InbuiltTypeSerializer>(cache.GetSerializer<string>());
        Assert.IsType<InbuiltTypeSerializer>(cache.GetSerializer<byte[]>());
        Assert.IsType<DefaultJsonSerializerFactory.DefaultJsonSerializer<Customer>>(cache.GetSerializer<Customer>());
        Assert.IsType<DefaultJsonSerializerFactory.DefaultJsonSerializer<Order>>(cache.GetSerializer<Order>());
    }

    [Fact]
    public void CustomSerializerConfiguration()
    {
        var services = new ServiceCollection();
        services.AddHybridCache().AddSerializer<Customer, CustomerSerializer>();
        using var provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.IsType<CustomerSerializer>(cache.GetSerializer<Customer>());
        Assert.IsType<DefaultJsonSerializerFactory.DefaultJsonSerializer<Order>>(cache.GetSerializer<Order>());
    }

    [Fact]
    public void CustomSerializerFactoryConfiguration()
    {
        var services = new ServiceCollection();
        services.AddHybridCache().AddSerializerFactory<CustomFactory>();
        using var provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.IsType<CustomerSerializer>(cache.GetSerializer<Customer>());
        Assert.IsType<DefaultJsonSerializerFactory.DefaultJsonSerializer<Order>>(cache.GetSerializer<Order>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DefaultMemoryDistributedCacheIsIgnored(bool manual)
    {
        var services = new ServiceCollection();
        if (manual)
        {
            services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.Null(cache.BackendCache);
    }

    [Fact]
    public void SubclassMemoryDistributedCacheIsNotIgnored()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDistributedCache, CustomMemoryDistributedCache>();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.NotNull(cache.BackendCache);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SubclassMemoryCacheIsNotIgnored(bool manual)
    {
        var services = new ServiceCollection();
        if (manual)
        {
            services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
        services.AddSingleton<IMemoryCache, CustomMemoryCache>();
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        Assert.NotNull(cache.BackendCache);
    }

    class CustomMemoryCache : MemoryCache
    {
        public CustomMemoryCache(IOptions<MemoryCacheOptions> options) : base(options) { }
        public CustomMemoryCache(IOptions<MemoryCacheOptions> options, ILoggerFactory loggerFactory) : base(options, loggerFactory) { }
    }

    class CustomMemoryDistributedCache : MemoryDistributedCache
    {
        public CustomMemoryDistributedCache(IOptions<MemoryDistributedCacheOptions> options) : base(options) { }
        public CustomMemoryDistributedCache(IOptions<MemoryDistributedCacheOptions> options, ILoggerFactory loggerFactory) : base(options, loggerFactory) { }
    }

    class Customer { }
    class Order { }

    class CustomerSerializer : IHybridCacheSerializer<Customer>
    {
        Customer IHybridCacheSerializer<Customer>.Deserialize(ReadOnlySequence<byte> source) => throw new NotImplementedException();
        void IHybridCacheSerializer<Customer>.Serialize(Customer value, IBufferWriter<byte> target) => throw new NotImplementedException();
    }

    class CustomFactory : IHybridCacheSerializerFactory
    {
        bool IHybridCacheSerializerFactory.TryCreateSerializer<T>(out IHybridCacheSerializer<T>? serializer)
        {
            if (typeof(T) == typeof(Customer))
            {
                serializer = (IHybridCacheSerializer<T>)new CustomerSerializer();
                return true;
            }
            serializer = null;
            return false;
        }
    }

    private static string Me([CallerMemberName] string caller = "") => caller;
}
