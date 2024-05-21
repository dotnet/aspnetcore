// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Microsoft.Extensions.Caching.Benchmarks;

[MemoryDiagnoser]
public class HybridCacheBenchmarks : IDisposable
{
    private const string RedisConfigurationString = "127.0.0.1,AllowAdmin=true";
    private readonly ConnectionMultiplexer _multiplexer;
    private readonly IDistributedCache _distributed;
    private readonly HybridCache _hybrid;
    public HybridCacheBenchmarks()
    {
        _multiplexer = ConnectionMultiplexer.Connect(RedisConfigurationString);
        var services = new ServiceCollection();
        services.AddStackExchangeRedisCache(options =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(_multiplexer);
        });
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();

        _distributed = provider.GetRequiredService<IDistributedCache>();

        _distributed.Remove(KeyDirect);
        _distributed.Remove(KeyHybrid);
        _distributed.Remove(KeyHybridImmutable);

        _hybrid = provider.GetRequiredService<HybridCache>();
    }

    private const string KeyDirect = "direct";
    private const string KeyHybrid = "hybrid";
    private const string KeyHybridImmutable = "I_brid"; // want 6 chars

    public void Dispose() => _multiplexer.Dispose();

    private const int CustomerId = 42;

    private static readonly DistributedCacheEntryOptions OneHour = new DistributedCacheEntryOptions()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    // scenario: 100% (or as-near-as) cache hit rate
    [Benchmark(Baseline = true)]
    public async ValueTask<Customer> HitDistributedCache() 
    {
        var bytes = await _distributed.GetAsync(KeyDirect);
        if (bytes is null)
        {
            var cust = await Customer.GetAsync(CustomerId);
            await _distributed.SetAsync(KeyDirect, Serialize(cust), OneHour);
            return cust;
        }
        else
        {
            return Deserialize<Customer>(bytes)!;
        }
    }

    // scenario: 100% (or as-near-as) cache hit rate
    [Benchmark]
    public ValueTask<Customer> HitCaptureHybridCache()
        => _hybrid.GetOrCreateAsync(KeyHybrid,
                ct => Customer.GetAsync(CustomerId, ct));

    // scenario: 100% (or as-near-as) cache hit rate
    [Benchmark]
    public ValueTask<Customer> HitHybridCache()
        => _hybrid.GetOrCreateAsync(KeyHybrid, CustomerId,
            static (id, ct) => Customer.GetAsync(id, ct));

    [Benchmark]
    public ValueTask<ImmutableCustomer> HitHybridCacheImmutable() // scenario: 100% (or as-near-as) cache hit rate
    => _hybrid.GetOrCreateAsync(KeyHybridImmutable, CustomerId, static (id, ct) => ImmutableCustomer.GetAsync(id, ct));

    private static byte[] Serialize<T>(T obj)
    {
        using var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, obj);
        return ms.ToArray();
    }

    private static T? Deserialize<T>(byte[] bytes)
    {
        using var ms = new MemoryStream();
        return JsonSerializer.Deserialize<T>(bytes);
    }

    public class Customer
    {
        public static ValueTask<Customer> GetAsync(int id, CancellationToken token = default)
            => new(new Customer
            {
                Id = id,
                Name = "Random customer",
                Region = 2,
                Description = "Good for testing",
                CreationDate = new DateTime(2024, 04, 17),
                OrderValue = 123_456.789M
            });

        public int Id { get; set; }
        public string? Name {get; set; }
        public int Region { get; set; }
        public string? Description { get; set; }
        public DateTime CreationDate { get; set; }
        public decimal OrderValue { get; set; }
    }

    [ImmutableObject(true)]
    public sealed class ImmutableCustomer
    {
        public static ValueTask<ImmutableCustomer> GetAsync(int id, CancellationToken token = default)
            => new(new ImmutableCustomer
            {
                Id = id,
                Name = "Random customer",
                Region = 2,
                Description = "Good for testing",
                CreationDate = new DateTime(2024, 04, 17),
                OrderValue = 123_456.789M
            });

        public int Id { get; init; }
        public string? Name { get; init; }
        public int Region { get; init; }
        public string? Description { get; init; }
        public DateTime CreationDate { get; init; }
        public decimal OrderValue { get; init; }
    }
}
