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
        if (options.ExpiresSliding.HasValue)
        {
            // HybridCache has no sliding-expiration concept. Silently mapping ExpiresSliding to
            // LocalCacheExpiration would change the meaning (it's an absolute local TTL, not sliding),
            // so we fail fast instead of producing wrong behavior.
            throw new NotSupportedException(
                $"{nameof(CacheBoundary)}.{nameof(CacheBoundary.ExpiresSliding)} is not supported when the cache boundary store is backed by HybridCache. " +
                $"Use {nameof(CacheBoundary.ExpiresAfter)} or {nameof(CacheBoundary.ExpiresOn)} for absolute expiration.");
        }

        if (options.Priority.HasValue)
        {
            // HybridCache does not expose a per-entry priority knob, so silently dropping a
            // user-supplied Priority (e.g. NeverRemove) would hide the fact that the eviction
            // policy is not honored. Fail fast for the same reason as ExpiresSliding.
            throw new NotSupportedException(
                $"{nameof(CacheBoundary)}.{nameof(CacheBoundary.Priority)} is not supported when the cache boundary store is backed by HybridCache. " +
                $"Remove the {nameof(CacheBoundary.Priority)} parameter or switch to the in-memory cache boundary store.");
        }

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
        };
    }

    public void Dispose()
    {
    }
}
