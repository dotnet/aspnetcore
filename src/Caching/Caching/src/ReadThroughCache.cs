// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Distributed;

public abstract class ReadThroughCache
{
    protected ReadThroughCache() { }

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Does not cause ambiguity due to callback signature delta")]
    public virtual ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> callback,
        ReadThroughCacheEntryOptions? options = null, ReadOnlyMemory<string> tags = default, CancellationToken cancellationToken = default)
        => GetOrCreateAsync(key, callback, WrappedCallbackCache<T>.Instance, options, tags, cancellationToken);

    public abstract ValueTask<(bool Exists, T Value)> GetAsync<T>(string key, ReadThroughCacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    public abstract ValueTask SetAsync<T>(string key, T value, ReadThroughCacheEntryOptions? options = null, ReadOnlyMemory<string> tags = default, CancellationToken cancellationToken = default);

    // don't like this...
    //public async virtual ValueTask<ImmutableDictionary<string, T>> GetAllAsync<T>(ReadOnlyMemory<string> keys, CancellationToken cancellationToken = default)
    //{
    //    ImmutableDictionary<string, T>.Builder? builder = null;
    //    int len = keys.Length;
    //    for (int i = 0; i < len; i++)
    //    {
    //        var pair = await GetAsync<T>(keys.Span[i], cancellationToken);
    //        if (pair.Exists)
    //        {
    //            builder ??= ImmutableDictionary.CreateBuilder<string, T>(StringComparer.Ordinal);
    //            builder.Add(keys.Span[i], pair.Value);
    //        }
    //    }

    //    if (builder is null)
    //    {
    //        return ImmutableDictionary<string, T>.Empty;
    //    }
    //    return builder.ToImmutable();
    //}

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Does not cause ambiguity due to callback signature delta")]
    public abstract ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, ReadThroughCacheEntryOptions? options = null, ReadOnlyMemory<string> tags = default, CancellationToken cancellationToken = default);

    public virtual ValueTask RemoveKeyAsync(string key, CancellationToken cancellationToken = default) => default;

    public virtual ValueTask RemoveKeysAsync(ReadOnlyMemory<string> keys, CancellationToken cancellationToken = default) => keys.Length switch
    {
        0 => default,
        1 => RemoveKeyAsync(keys.Span[0], cancellationToken),
        _ => RemoveAsyncSlow(keys, cancellationToken),
    };

    private async ValueTask RemoveAsyncSlow(ReadOnlyMemory<string> keys, CancellationToken cancellationToken)
    {
        int len = keys.Length;
        for (int i = 0; i < len; i++)
        {
            await RemoveKeyAsync(keys.Span[i], cancellationToken);
        }
    }

    public virtual ValueTask RemoveTagAsync(string tag, CancellationToken cancellationToken = default)
        => RemoveTagsAsync(new [] { tag }, cancellationToken);

    public virtual ValueTask RemoveTagsAsync(ReadOnlyMemory<string> tags, CancellationToken cancellationToken = default) => default;

    private static class WrappedCallbackCache<T>
    {
        // for the simple usage scenario (no TState), pack the original callback as the "state", and use a wrapper function that just unrolls and invokes from the state
        public static readonly Func<Func<CancellationToken, ValueTask<T>>, CancellationToken, ValueTask<T>> Instance = static (callback, ct) => callback(ct);
    }
}

[Flags]
public enum ReadThroughCacheEntryFlags
{
    None = 0,
    BypassLocalCache = 1 << 0,
    BypassDistributedCache = 1 << 1,
    BypassCompression = 1 << 2,
}
public sealed class ReadThroughCacheEntryOptions(TimeSpan expiry, ReadThroughCacheEntryFlags flags = 0)
{
    public TimeSpan Expiry { get; } = expiry;

    public ReadThroughCacheEntryFlags Flags { get; } = flags;

    private DistributedCacheEntryOptions? _distributedCacheEntryOptions;
    internal DistributedCacheEntryOptions AsDistributedCacheEntryOptions()
        => _distributedCacheEntryOptions ??= new() { AbsoluteExpirationRelativeToNow = Expiry };
}
