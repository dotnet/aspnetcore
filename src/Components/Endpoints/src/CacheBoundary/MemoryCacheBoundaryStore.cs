// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class MemoryCacheBoundaryStore : ICacheBoundaryStore
{
    private readonly MemoryCache _cache;
    private readonly ILogger<MemoryCacheBoundaryStore> _logger;
    private readonly ConcurrentDictionary<string, Task<SerializedRenderFragment>> _pending = new(StringComparer.Ordinal);

    public MemoryCacheBoundaryStore(IOptions<RazorComponentsServiceOptions> options, ILogger<MemoryCacheBoundaryStore> logger)
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.Value.CacheBoundarySizeLimit,
        });
        _logger = logger;
    }

    public async ValueTask<SerializedRenderFragment> GetOrCreateAsync(
        string key,
        Func<CancellationToken, ValueTask<SerializedRenderFragment>> factory,
        CacheStoreOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_cache.TryGetValue<SerializedRenderFragment>(key, out var existing) && existing is not null)
        {
            return existing;
        }

        var tcs = new TaskCompletionSource<SerializedRenderFragment>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = _pending.GetOrAdd(key, tcs.Task);
        if (!ReferenceEquals(pending, tcs.Task))
        {
            return await pending.WaitAsync(cancellationToken);
        }

        try
        {
            var payload = await factory(cancellationToken);
            StoreEntry(key, payload, options);
            tcs.SetResult(payload);
            return payload;
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
            throw;
        }
        finally
        {
            _pending.TryRemove(new KeyValuePair<string, Task<SerializedRenderFragment>>(key, tcs.Task));
        }
    }

    private void StoreEntry(string key, SerializedRenderFragment payload, CacheStoreOptions options)
    {
        try
        {
            var entryOptions = new MemoryCacheEntryOptions
            {
                Size = EstimateSize(payload),
            };

            if (options.ExpiresSliding.HasValue)
            {
                entryOptions.SlidingExpiration = options.ExpiresSliding.Value;
            }

            if (options.ExpiresOn.HasValue)
            {
                entryOptions.AbsoluteExpiration = options.ExpiresOn.Value;
            }
            else
            {
                entryOptions.AbsoluteExpirationRelativeToNow = options.ExpiresAfter ?? RazorComponentsServiceOptions.DefaultCacheBoundaryExpiration;
            }

            _cache.Set(key, payload, entryOptions);
        }
        catch (Exception ex)
        {
            // Failing to cache the entry should not fail the request; the value was still
            // produced successfully, so log and continue without caching.
            // Identical behaviour to HybridCache
            Log.StoreEntryFailed(_logger, key, ex);
        }
    }

    public void Clear()
    {
        _cache.Clear();
    }

    // The store keeps the fragment as an object instead of a serialized byte[]; the captured markup
    // length was recorded when the fragment was built, so reuse it directly for the cache limit.
    private static long EstimateSize(SerializedRenderFragment payload)
        => Math.Max(payload.ContentSize, 1);

    public void Dispose()
    {
        _cache.Dispose();
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "Failed to store CacheBoundary entry for key '{Key}'.", EventName = "StoreEntryFailed")]
        public static partial void StoreEntryFailed(ILogger logger, string key, Exception exception);
    }
}
