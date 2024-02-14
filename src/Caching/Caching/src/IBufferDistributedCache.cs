// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Caching.Distributed;

public interface IBufferDistributedCache : IDistributedCache
{
    ValueTask<CacheGetResult> GetAsync(string key, IBufferWriter<byte> destination, CancellationToken cancellationToken);
    ValueTask SetAsync(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken cancellationToken);
}

public readonly struct CacheGetResult
{
    private const byte DoesNotExist = 0, ExistsNoExpiry = 1,
        ExistsAbsoluteExpiry = 2, ExistsRelativeExpiry = 3;

    private readonly byte _discriminator;
    private readonly long _value;
    public CacheGetResult(bool exists)
    {
        _discriminator = exists ? ExistsNoExpiry : DoesNotExist;
        _value = 0;
    }
    public CacheGetResult(DateTime expiry)
    {
        _discriminator = ExistsAbsoluteExpiry;
        _value = Unsafe.As<DateTime, long>(ref expiry);
    }

    public CacheGetResult(TimeSpan expiry)
    {
        _discriminator = ExistsRelativeExpiry;
        _value = Unsafe.As<TimeSpan, long>(ref expiry);
    }

    public bool Exists => _discriminator != DoesNotExist;
    public TimeSpan? ExpiryRelative => _discriminator switch
    {
        ExistsRelativeExpiry => Unsafe.As<long, TimeSpan>(ref Unsafe.AsRef(in _value)),
        _ => null,
    };

    public DateTime? ExpiryAbsolute => _discriminator switch
    {
        ExistsAbsoluteExpiry => Unsafe.As<long, DateTime>(ref Unsafe.AsRef(in _value)),
        _ => null,
    };
}
