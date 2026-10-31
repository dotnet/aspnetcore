// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HybridCacheViewStore : ICacheViewStore
{
    private const string CacheViewTag = "Microsoft.AspNetCore.Components.Endpoints.CacheView";
    private static readonly string[] _tags = [CacheViewTag];

    private readonly HybridCache _hybridCache;

    public HybridCacheViewStore(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    public ValueTask<SerializedRenderFragment> GetOrCreateAsync(
        string key,
        Func<CancellationToken, ValueTask<SerializedRenderFragment>> factory,
        CacheStoreOptions options,
        CancellationToken cancellationToken)
    {
        var hybridOptions = BuildHybridOptions(options);

        // HybridCache provides single-flight stampede protection: concurrent callers for one key coalesce
        // onto a single factory invocation, and the shared factory is only cancelled when the last enlisted
        // caller cancels (ref-counted). No re-election is needed here.
        return _hybridCache.GetOrCreateAsync(key, factory, static (state, ct) => state(ct), hybridOptions, _tags, cancellationToken);
    }

    public void Clear()
    {
        // Eviction is used only for hot reload, so it is best-effort; fire-and-forget to avoid blocking
        // a thread on the async call.
        _ = ClearCoreAsync();
    }

    private async Task ClearCoreAsync()
    {
        try
        {
            await _hybridCache.RemoveByTagAsync(CacheViewTag);
        }
        catch
        {
            // Best-effort cache eviction; ignore failures.
        }
    }

    private static HybridCacheEntryOptions BuildHybridOptions(CacheStoreOptions options)
    {
        if (options.ExpiresSliding.HasValue)
        {
            throw new NotSupportedException(
                $"{nameof(CacheView)}.{nameof(CacheView.ExpiresSliding)} is not supported when the cache view store uses HybridCache. " +
                $"Use {nameof(CacheView.ExpiresAfter)} or {nameof(CacheView.ExpiresOn)} for absolute expiration.");
        }

        var absolute = options.ExpiresOn.HasValue
            ? options.ExpiresOn.Value - DateTimeOffset.UtcNow
            : options.ExpiresAfter ?? RazorComponentsServiceOptions.DefaultCacheViewExpiration;

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
