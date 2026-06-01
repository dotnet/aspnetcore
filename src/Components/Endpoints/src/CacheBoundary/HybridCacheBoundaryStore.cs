// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// An <see cref="ICacheBoundaryStore"/> backed by <see cref="HybridCache"/>.
/// Delegates single-flight, stampede protection, local/distributed tiering, and
/// serialization to <c>HybridCache</c>; the factory is invoked once per key across
/// concurrent requests.
/// </summary>
internal sealed class HybridCacheBoundaryStore : ICacheBoundaryStore
{
    private const string CacheBoundaryTag = "Microsoft.AspNetCore.Components.Endpoints.CacheBoundary";
    private static readonly string[] _tags = [CacheBoundaryTag];

    private readonly HybridCache _hybridCache;

    public HybridCacheBoundaryStore(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    public ValueTask<string> GetOrCreateAsync(
        string key,
        Func<CancellationToken, ValueTask<string>> factory,
        CacheStoreOptions options,
        CancellationToken cancellationToken)
    {
        var hybridOptions = BuildHybridOptions(options);
        return _hybridCache.GetOrCreateAsync(key, factory, static (state, ct) => state(ct), hybridOptions, _tags, cancellationToken);
    }

    public void Clear()
    {
        // Evicts all entries written by this store. HybridCache only exposes async eviction,
        // so block on the task here. Clear is intended for test scenarios.
        _hybridCache.RemoveByTagAsync(CacheBoundaryTag).AsTask().GetAwaiter().GetResult();
    }

    private static HybridCacheEntryOptions BuildHybridOptions(CacheStoreOptions options)
    {
        var absolute = options.ExpiresOn.HasValue
            ? options.ExpiresOn.Value - DateTimeOffset.UtcNow
            : options.ExpiresAfter ?? RazorComponentsServiceOptions.DefaultCacheBoundaryExpiration;

        if (absolute < TimeSpan.Zero)
        {
            absolute = TimeSpan.Zero;
        }

        return new HybridCacheEntryOptions
        {
            Expiration = absolute,
            LocalCacheExpiration = options.ExpiresSliding,
        };
    }

    public void Dispose()
    {
    }
}
