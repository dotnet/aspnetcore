// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Distributed;

public interface IReadThroughCache
{
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Does not cause ambiguity due to callback signature delta")]
    ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> callback,
        ReadThroughCacheEntryOptions? options = null, ReadOnlyMemory<string> tags = default, CancellationToken cancellationToken = default);

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Does not cause ambiguity due to callback signature delta")]
    ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, ReadThroughCacheEntryOptions? options = null, ReadOnlyMemory<string> tags = default, CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    ValueTask RemoveTagsAsync(ReadOnlyMemory<string> tags, CancellationToken cancellationToken = default);
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
