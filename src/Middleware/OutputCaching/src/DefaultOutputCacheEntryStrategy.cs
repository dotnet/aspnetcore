// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Stores the response under <see cref="OutputCacheContext.CacheKey"/>.
/// </summary>
internal sealed class DefaultOutputCacheEntryStrategy : IOutputCacheEntryStrategy
{
    private readonly IOutputCacheStore _store;
    private readonly ILogger _logger;
    private readonly WorkDispatcher<string, OutputCacheEntry?> _outputCacheEntryDispatcher = new();

    public DefaultOutputCacheEntryStrategy(IOutputCacheStore store, ILogger logger)
    {
        _store = store;
        _logger = logger;
    }

    public async ValueTask<OutputCacheEntry?> GetAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        // Locking cache lookups by default
        // TODO: should it be part of the cache implementations or can we assume all caches would benefit from it?
        // It makes sense for caches that use IO (disk, network) or need to deserialize the state but could also be a global option
        return await _outputCacheEntryDispatcher.ScheduleAsync(context.CacheKey, (Store: _store, CacheContext: context), static async (key, state) => await OutputCacheEntryFormatter.GetAsync(key, state.Store, state.CacheContext.HttpContext.RequestAborted), cancellationToken);
    }

    public ValueTask StoreAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return OutputCacheEntryFormatter.StoreAsync(context.CacheKey, context.CachedResponse!, context.Tags, context.CachedResponseValidFor, _store, _logger, cancellationToken);
    }
}
