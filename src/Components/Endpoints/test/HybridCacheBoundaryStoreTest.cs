// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Caching.Hybrid;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class HybridCacheBoundaryStoreTest
{
    [Fact]
    public void GetOrCreateAsync_WithExpiresSliding_ThrowsNotSupported()
    {
        var store = new HybridCacheBoundaryStore(new StubHybridCache());
        var options = new CacheStoreOptions { ExpiresSliding = TimeSpan.FromMinutes(1) };

        var ex = Assert.Throws<NotSupportedException>(() =>
        {
            store.GetOrCreateAsync("key", Factory, options, default);
        });
        Assert.Contains(nameof(CacheBoundary.ExpiresSliding), ex.Message);
    }

    private static ValueTask<byte[]> Factory(CancellationToken cancellationToken)
        => ValueTask.FromResult(Array.Empty<byte>());

    private sealed class StubHybridCache : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
