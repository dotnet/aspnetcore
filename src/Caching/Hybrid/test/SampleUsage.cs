// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class SampleUsage
{
    [Fact]
    public async Task DistributedCacheWorks()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddTransient<SomeDCService>();
        using var provider = services.BuildServiceProvider();

        var obj = provider.GetRequiredService<SomeDCService>();
        string name = "abc";
        int id = 42;
        var x = await obj.GetSomeInformationAsync(name, id);
        var y = await obj.GetSomeInformationAsync(name, id);
        Assert.NotSame(x, y);
        Assert.Equal(id, x.Id);
        Assert.Equal(name, x.Name);
        Assert.Equal(id, y.Id);
        Assert.Equal(name, y.Name);
    }

    [Fact]
    public async Task HybridCacheWorks()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddTransient<SomeHCService>();
        using var provider = services.BuildServiceProvider();

        var obj = provider.GetRequiredService<SomeHCService>();
        string name = "abc";
        int id = 42;
        var x = await obj.GetSomeInformationAsync(name, id);
        var y = await obj.GetSomeInformationAsync(name, id);
        Assert.NotSame(x, y);
        Assert.Equal(id, x.Id);
        Assert.Equal(name, x.Name);
        Assert.Equal(id, y.Id);
        Assert.Equal(name, y.Name);
    }

    [Fact]
    public async Task HybridCacheNoCaptureWorks()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddTransient<SomeHCServiceNoCapture>();
        using var provider = services.BuildServiceProvider();

        var obj = provider.GetRequiredService<SomeHCServiceNoCapture>();
        string name = "abc";
        int id = 42;
        var x = await obj.GetSomeInformationAsync(name, id);
        var y = await obj.GetSomeInformationAsync(name, id);
        Assert.NotSame(x, y);
        Assert.Equal(id, x.Id);
        Assert.Equal(name, x.Name);
        Assert.Equal(id, y.Id);
        Assert.Equal(name, y.Name);
    }

    [Fact]
    public async Task HybridCacheNoCaptureObjReuseWorks()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddTransient<SomeHCServiceNoCaptureObjReuse>();
        using var provider = services.BuildServiceProvider();

        var obj = provider.GetRequiredService<SomeHCServiceNoCaptureObjReuse>();
        string name = "abc";
        int id = 42;
        var x = await obj.GetSomeInformationAsync(name, id);
        var y = await obj.GetSomeInformationAsync(name, id);
        Assert.Same(x, y);
        Assert.Equal(id, x.Id);
        Assert.Equal(name, x.Name);
    }

    public class SomeDCService(IDistributedCache cache)
    {
        public async Task<SomeInformation> GetSomeInformationAsync(string name, int id, CancellationToken token = default)
        {
            var key = $"someinfo:{name}:{id}"; // unique key for this combination

            var bytes = await cache.GetAsync(key, token); // try to get from cache
            SomeInformation info;
            if (bytes is null)
            {
                // cache miss; get the data from the real source
                info = await SomeExpensiveOperationAsync(name, id, token);

                // serialize and cache it
                bytes = SomeSerializer.Serialize(info);
                await cache.SetAsync(key, bytes, token);
            }
            else
            {
                // cache hit; deserialize it
                info = SomeSerializer.Deserialize<SomeInformation>(bytes);
            }
            return info;
        }
    }

    public class SomeHCService(HybridCache cache)
    {
        public async Task<SomeInformation> GetSomeInformationAsync(string name, int id, CancellationToken token = default)
        {
            return await cache.GetOrCreateAsync(
                $"someinfo:{name}:{id}", // unique key for this combination
                async ct => await SomeExpensiveOperationAsync(name, id, ct),
                token: token
                );
        }
    }

    // this is the work we're trying to cache
    private static Task<SomeInformation> SomeExpensiveOperationAsync(string name, int id,
        CancellationToken token = default)
    {
        return Task.FromResult(new SomeInformation { Id = id, Name = name });
    }
    private static Task<SomeInformationReuse> SomeExpensiveOperationReuseAsync(string name, int id,
    CancellationToken token = default)
    {
        return Task.FromResult(new SomeInformationReuse { Id = id, Name = name });
    }

    public class SomeHCServiceNoCapture(HybridCache cache)
    {
        public async Task<SomeInformation> GetSomeInformationAsync(string name, int id, CancellationToken token = default)
        {
            return await cache.GetOrCreateAsync(
                $"someinfo:{name}:{id}", // unique key for this combination
                (name, id), // all of the state we need for the final call, if needed
                static async (state, token) =>
                    await SomeExpensiveOperationAsync(state.name, state.id, token),
                token: token
            );
        }
    }

    public class SomeHCServiceNoCaptureObjReuse(HybridCache cache, CancellationToken token = default)
    {
        public async Task<SomeInformationReuse> GetSomeInformationAsync(string name, int id)
        {
            return await cache.GetOrCreateAsync(
                $"someinfo:{name}:{id}", // unique key for this combination
                (name, id), // all of the state we need for the final call, if needed
                static async (state, token) =>
                    await SomeExpensiveOperationReuseAsync(state.name, state.id, token),
                token: token
            );
        }
    }

    static class SomeSerializer
    {
        internal static T Deserialize<T>(byte[] bytes)
        {
            return JsonSerializer.Deserialize<T>(bytes)!;
        }

        internal static byte[] Serialize<T>(T info)
        {
            using var ms = new MemoryStream();
            JsonSerializer.Serialize(ms, info);
            return ms.ToArray();
        }
    }
    public class SomeInformation
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [ImmutableObject(true)]
    public sealed class SomeInformationReuse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
