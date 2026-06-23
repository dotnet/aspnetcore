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
    private readonly ConcurrentDictionary<string, Task<string>> _pending = new(StringComparer.Ordinal);

    public MemoryCacheBoundaryStore(IOptions<RazorComponentsServiceOptions> options, ILogger<MemoryCacheBoundaryStore> logger)
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.Value.CacheBoundarySizeLimit,
        });
        _logger = logger;
    }

    public async ValueTask<string> GetOrCreateAsync(
        string key,
        Func<CancellationToken, ValueTask<string>> factory,
        CacheStoreOptions options,
        CancellationToken cancellationToken)
    {
        // Loops only to re-elect a creator when an in-flight creator is cancelled (e.g. its request
        // is aborted) while this caller is still alive. Each iteration either returns a cached value,
        // observes another caller's result, or becomes the creator itself.
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_cache.TryGetValue<string>(key, out var existing) && existing is not null)
            {
                return existing;
            }

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = _pending.GetOrAdd(key, tcs.Task);
            if (!ReferenceEquals(pending, tcs.Task))
            {
                // Another caller is already creating this entry; observe their result.
                try
                {
                    return await pending.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // The creator was cancelled but this request is still alive. The creator removes
                    // its pending entry before faulting, so loop back to re-check the cache and, if
                    // necessary, re-elect a new creator (possibly this caller). A genuine factory
                    // exception is not an OperationCanceledException, so it still propagates here.
                    continue;
                }
            }

            try
            {
                var json = await factory(cancellationToken);
                StoreEntry(key, json, options);
                tcs.SetResult(json);
                return json;
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
            finally
            {
                _pending.TryRemove(new KeyValuePair<string, Task<string>>(key, tcs.Task));
            }
        }
    }

    private void StoreEntry(string key, string json, CacheStoreOptions options)
    {
        try
        {
            var entryOptions = new MemoryCacheEntryOptions
            {
                Size = json.Length * sizeof(char),
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

            if (options.Priority.HasValue)
            {
                entryOptions.Priority = options.Priority.Value;
            }

            _cache.Set(key, json, entryOptions);
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
