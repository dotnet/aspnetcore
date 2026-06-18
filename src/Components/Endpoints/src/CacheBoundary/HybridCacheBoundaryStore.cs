// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HybridCacheBoundaryStore : ICacheBoundaryStore
{
    private const string CacheBoundaryTag = "Microsoft.AspNetCore.Components.Endpoints.CacheBoundary";
    private static readonly string[] _tags = [CacheBoundaryTag];

    private readonly HybridCache _hybridCache;

    public HybridCacheBoundaryStore(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    public async ValueTask<string> GetOrCreateAsync(
        string key,
        Func<CancellationToken, ValueTask<string>> factory,
        CacheStoreOptions options,
        CancellationToken cancellationToken)
    {
        var hybridOptions = BuildHybridOptions(options);

        // Loops only to re-elect a creator when the in-flight creator's render is cancelled (its
        // request is aborted, cancelling the boundary's captureCompletion) while this caller is still
        // alive. HybridCache removes the faulted stampede entry, so a retry re-checks the cache and,
        // if necessary, re-elects a new creator (possibly this caller). A genuine factory exception is
        // not an OperationCanceledException, so it still propagates.
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _hybridCache.GetOrCreateAsync(key, factory, static (state, ct) => state(ct), hybridOptions, _tags, cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                continue;
            }
        }
    }

    public void Clear()
    {
        _hybridCache.RemoveByTagAsync(CacheBoundaryTag).AsTask().GetAwaiter().GetResult();
    }

    private static HybridCacheEntryOptions BuildHybridOptions(CacheStoreOptions options)
    {
        if (options.ExpiresSliding.HasValue)
        {
            throw new NotSupportedException(
                $"{nameof(CacheBoundary)}.{nameof(CacheBoundary.ExpiresSliding)} is not supported when the cache boundary store uses HybridCache. " +
                $"Use {nameof(CacheBoundary.ExpiresAfter)} or {nameof(CacheBoundary.ExpiresOn)} for absolute expiration.");
        }

        if (options.Priority.HasValue)
        {
            throw new NotSupportedException(
                $"{nameof(CacheBoundary)}.{nameof(CacheBoundary.Priority)} is not supported when the cache boundary store uses HybridCache. " +
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
