// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Distributed;
internal sealed class DistributedCache<T> : IDistributedCache<T>
{
    private readonly ICacheSerializer<T> _serializer;
    private readonly IDistributedCache _backend;
    private readonly IBufferDistributedCache? _bufferBackend;

    public DistributedCache(IOptions<TypedDistributedCacheOptions> options, ICacheSerializer<T> serializer, IDistributedCache backend)
    {
        _serializer = serializer;
        _backend = backend;
        _bufferBackend = backend as IBufferDistributedCache; // do the type test once only
        _ = options;
    }

    public ValueTask<T> GetAsync(string key, Func<ValueTask<T>> callback, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(callback);

        return _bufferBackend is not null
            ? GetBufferedBackendAsync(key, callback, options, cancellationToken)
            : GetLegacyBackendAsync(key, callback, options, cancellationToken);

    }

    private ValueTask<T> GetBufferedBackendAsync(string key, Func<ValueTask<T>> callback, DistributedCacheEntryOptions? options, CancellationToken cancellationToken)
    {
        var buffer = new RecyclableArrayBufferWriter<byte>();
        var pendingGet = _bufferBackend!.TryGetAsync(key, buffer, cancellationToken);

        if (!pendingGet.IsCompletedSuccessfully)
        {
            return AwaitedBackend(this, key, callback, options, cancellationToken, buffer, pendingGet);
        }

        // fast path; backend available immediately
        if (pendingGet.GetAwaiter().GetResult())
        {
            var result = _serializer.Deserialize(new(buffer.GetCommittedMemory()));
            buffer.Dispose();
            return new(result);
        }
    
        // fall back to main code-path, but without the pending bytes (we've already checked those)
        return AwaitedBackend(this, key, callback, options, cancellationToken, buffer, default);

        static async ValueTask<T> AwaitedBackend(DistributedCache<T> @this, string key, Func<ValueTask<T>> callback, DistributedCacheEntryOptions? options,
             CancellationToken cancellationToken, RecyclableArrayBufferWriter<byte> buffer, ValueTask<bool> pendingGet)
        {
            using (buffer)
            {
                if (await pendingGet)
                {
                    return @this._serializer.Deserialize(new(buffer.GetCommittedMemory()));
                }

                var value = await callback();
                if (value is null)
                {
                    await @this._backend.RemoveAsync(key, cancellationToken);
                }
                else
                {
                    buffer.Reset();
                    @this._serializer.Serialize(value, buffer);
                    await @this._bufferBackend!.SetAsync(key, new(buffer.GetCommittedMemory()), options ?? _defaultOptions, cancellationToken);
                }

                return value;
            }
        }
    }

    private ValueTask<T> GetLegacyBackendAsync(string key, Func<ValueTask<T>> callback, DistributedCacheEntryOptions? options, CancellationToken cancellationToken)
    {
        var pendingBytes = _backend.GetAsync(key, cancellationToken);
        if (!pendingBytes.IsCompletedSuccessfully)
        {
            return AwaitedBackend(this, key, callback, options, cancellationToken, pendingBytes);
        }

        // fast path; backend available immediately
        var bytes = pendingBytes.Result;
        if (bytes is not null)
        {
            return new(_serializer.Deserialize(new ReadOnlySequence<byte>(bytes))!);
        }

        // fall back to main code-path, but without the pending bytes (we've already checked those)
        return AwaitedBackend(this, key, callback, options, cancellationToken, null);

        static async ValueTask<T> AwaitedBackend(DistributedCache<T> @this, string key, Func<ValueTask<T>> callback, DistributedCacheEntryOptions? options,
             CancellationToken cancellationToken, Task<byte[]?>? pendingBytes)
        {
            if (pendingBytes is not null)
            {
                var bytes = await pendingBytes;
                if (bytes is not null)
                {
                    return @this._serializer.Deserialize(new ReadOnlySequence<byte>(bytes));
                }
            }

            var value = await callback();
            if (value is null)
            {
                await @this._backend.RemoveAsync(key, cancellationToken);
            }
            else
            {
                using var writer = new RecyclableArrayBufferWriter<byte>();
                @this._serializer.Serialize(value, writer);
                await @this._backend.SetAsync(key, writer.ToArray(), options ?? _defaultOptions, cancellationToken);
            }

            return value;
        }
    }

    static readonly DistributedCacheEntryOptions _defaultOptions = new();

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        => new(_backend.RemoveAsync(key, cancellationToken));
}
