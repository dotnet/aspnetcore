// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Distributed;
internal sealed class ReadThroughCache(IOptions<ReadThroughCacheOptions> options, IServiceProvider services, IMemoryCache frontent, IDistributedCache backend) : IReadThroughCache
{
    private readonly IServiceProvider _services = services;
    private readonly IMemoryCache _frontend = frontent;
    private readonly IDistributedCache _backend = backend;
    private readonly IBufferDistributedCache? _bufferBackend = backend as IBufferDistributedCache;
    private readonly DistributedCacheEntryOptions _defaultOptions = options.Value.DefaultOptions ?? new();

    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> callback, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => GetOrCreateAsync(key, callback, WrappedCallbackCache<T>.Instance, options, cancellationToken);

    private static class ReadOnlyTypeCache<T>
    {
        public static readonly bool IsReadOnly = IsReadOnly(typeof(T));
    }

    private static bool IsReadOnly(Type type)
    {
        if (type == typeof(string) || type == typeof(byte[]))
        {
            return true;
        }
        var attribs = type.GetCustomAttributes(false);
        foreach (var attrib in attribs)
        {
            if (attrib.GetType().Name == "System.Runtime.CompilerServices.IsReadOnlyAttribute")
            {
                return true;
            }
            if (attrib is ImmutableObjectAttribute { Immutable: true })
            {
                return true;
            }
        }
        return false;
    }

    public ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(callback);

        if (ReadOnlyTypeCache<T>.IsReadOnly)
        {
            if (_frontend.TryGetValue(key, out T? existing))
            {
                return new(existing!);
            }
        }
        else
        {
            if (_frontend.TryGetValue(key, out byte[]? buffer))
            {
                return new(GetSerializer<T>().Deserialize(new(buffer!)));
            }
        }

        return _bufferBackend is not null
            ? GetBufferedBackendAsync(key, state, callback, options ?? _defaultOptions, cancellationToken)
            : GetLegacyBackendAsync(key, state, callback, options ?? _defaultOptions, cancellationToken);
    }

    ValueTask IReadThroughCache.RefreshAsync(string key, CancellationToken cancellationToken) => new(_backend.RefreshAsync(key, cancellationToken));

    private readonly ConcurrentDictionary<Type, object> _serializerCache = new();

    internal IReadThroughCacheSerializer<T> GetSerializer<T>()
    {
        return _serializerCache.TryGetValue(typeof(T), out var serializer)
            ? (IReadThroughCacheSerializer<T>)serializer
            : GetSerializerSlow<T>();
    }
    private IReadThroughCacheSerializer<T> GetSerializerSlow<T>()
    {
        var serializer = _services.GetService<IReadThroughCacheSerializer<T>>();
        if (serializer is null)
        {
            foreach (var svc in _services.GetServices<IReadThroughCacheSerializerFactory>())
            {
                // *last* wins, "Add" meaning "make more specific"
                if (svc.TryCreateSerializer<T>(out var tmp))
                {
                    serializer = tmp;
                }
            }
        }
        if (serializer is null)
        {
            throw new InvalidOperationException("No serializer registered for " + typeof(T).FullName);
        }
        _serializerCache[typeof(T)] = serializer;
        return serializer;
    }

    private ValueTask<T> GetBufferedBackendAsync<TState, T>(string key, TState state,
        Func<TState, CancellationToken, ValueTask<T>> callback,
        DistributedCacheEntryOptions options, CancellationToken cancellationToken)
    {
        var buffer = new RecyclableArrayBufferWriter<byte>();
        var pendingGet = _bufferBackend!.GetAsync(key, buffer, cancellationToken);

        if (!pendingGet.IsCompletedSuccessfully)
        {
            return AwaitedBackend(this, key, state, callback, options, cancellationToken, buffer, pendingGet);
        }

        var getResult = pendingGet.GetAwaiter().GetResult();
        // fast path; backend available immediately
        if (getResult.Exists)
        {
            var result = DeserializeAndCacheFrontend<T>(key, options, getResult, buffer);
            buffer.Dispose();
            return new(result);
        }

        // fall back to main code-path, but without the pending bytes (we've already checked those)
        return AwaitedBackend(this, key, state, callback, options, cancellationToken, buffer, default);

        static async ValueTask<T> AwaitedBackend(ReadThroughCache @this, string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, DistributedCacheEntryOptions options,
             CancellationToken cancellationToken, RecyclableArrayBufferWriter<byte> buffer, ValueTask<CacheGetResult> pendingGet)
        {
            using (buffer)
            {
                var getResult = await pendingGet;
                if (getResult.Exists)
                {
                    return @this.DeserializeAndCacheFrontend<T>(key, options, getResult, buffer);
                }

                var value = await callback(state, cancellationToken);
                if (value is null)
                {
                    await @this._backend.RemoveAsync(key, cancellationToken);
                }
                else
                {
                    @this.SerializeAndCacheFrontend<T>(key, value, options, buffer, out _);
                    await @this._bufferBackend!.SetAsync(key, new(buffer.GetCommittedMemory()), options, cancellationToken);
                }

                return value;
            }
        }
    }

    private void SerializeAndCacheFrontend<T>(string key, T value, DistributedCacheEntryOptions options, RecyclableArrayBufferWriter<byte> buffer,
        out byte[]? arr)
    {
        buffer.Reset();
        GetSerializer<T>().Serialize(value, buffer);

        var expiry = ComputeExpiration(options, default);
        if (ReadOnlyTypeCache<T>.IsReadOnly)
        {
            arr = null;
            _frontend.Set(key, value, expiry);
        }
        else
        {
            _frontend.Set(key, arr = buffer.ToArray(), expiry);
        }
    }

    private static DateTime ComputeExpiration(DistributedCacheEntryOptions options, CacheGetResult result)
    {
        if (result.ExpiryAbsolute.HasValue)
        {
            return result.ExpiryAbsolute.GetValueOrDefault();
        }
        if (result.ExpiryRelative.HasValue)
        {
            return DateTime.UtcNow.Add(result.ExpiryRelative.GetValueOrDefault());
        }
        if (options.AbsoluteExpiration.HasValue)
        {
            // DateTimeOffset to DateTime
            return new DateTime(options.AbsoluteExpiration.GetValueOrDefault().UtcTicks, DateTimeKind.Utc);
        }
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            return DateTime.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.GetValueOrDefault());
        }
        if (options.SlidingExpiration.HasValue)
        {
            return DateTime.UtcNow.Add(options.SlidingExpiration.GetValueOrDefault());
        }
        const int HardDefaultExpiryMinutes = 5;
        return DateTime.UtcNow.AddMinutes(HardDefaultExpiryMinutes);
    }

    private T DeserializeAndCacheFrontend<T>(string key, DistributedCacheEntryOptions options, CacheGetResult getResult, RecyclableArrayBufferWriter<byte> buffer)
    {
        var expiry = ComputeExpiration(options, getResult);
        if (!ReadOnlyTypeCache<T>.IsReadOnly)
        {
            _frontend.Set(key, buffer.ToArray(), expiry);
        }
        var result = GetSerializer<T>().Deserialize(new(buffer.GetCommittedMemory()));

        if (ReadOnlyTypeCache<T>.IsReadOnly)
        {
            _frontend.Set(key, result, expiry);
        }
        return result;
    }

    private T DeserializeAndCacheFrontend<T>(string key, DistributedCacheEntryOptions options, byte[] buffer)
    {
        var expiry = ComputeExpiration(options, default);
        if (!ReadOnlyTypeCache<T>.IsReadOnly)
        {
            _frontend.Set(key, buffer, expiry);
        }
        var result = GetSerializer<T>().Deserialize(new(buffer));

        if (ReadOnlyTypeCache<T>.IsReadOnly)
        {
            _frontend.Set(key, result, expiry);
        }
        return result;
    }

    private ValueTask<T> GetLegacyBackendAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
    {
        var pendingBytes = _backend.GetAsync(key, cancellationToken);
        if (!pendingBytes.IsCompletedSuccessfully)
        {
            return AwaitedBackend(this, key, state, callback, options, cancellationToken, pendingBytes);
        }

        // fast path; backend available immediately
        var bytes = pendingBytes.Result;
        if (bytes is not null)
        {
            return new(DeserializeAndCacheFrontend<T>(key, options, bytes));
        }

        // fall back to main code-path, but without the pending bytes (we've already checked those)
        return AwaitedBackend(this, key, state, callback, options, cancellationToken, null);

        static async ValueTask<T> AwaitedBackend(ReadThroughCache @this, string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, DistributedCacheEntryOptions options,
             CancellationToken cancellationToken, Task<byte[]?>? pendingBytes)
        {
            if (pendingBytes is not null)
            {
                var bytes = await pendingBytes;
                if (bytes is not null)
                {
                    return @this.DeserializeAndCacheFrontend<T>(key, options, bytes);
                }
            }

            var value = await callback(state, cancellationToken);
            if (value is null)
            {
                await @this._backend.RemoveAsync(key, cancellationToken);
            }
            else
            {
                using var buffer = new RecyclableArrayBufferWriter<byte>();
                @this.SerializeAndCacheFrontend<T>(key, value, options, buffer, out var arr);
                await @this._backend.SetAsync(key, arr ?? buffer.ToArray(), options, cancellationToken);
            }

            return value;
        }
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _frontend.Remove(key);
        return new (_backend.RemoveAsync(key, cancellationToken));
    }

    private static class WrappedCallbackCache<T>
    {
        // for the simple usage scenario (no TState), pack the original callback as the "state", and use a wrapper function that just unrolls and invokes from the state
        public static readonly Func<Func<CancellationToken, ValueTask<T>>, CancellationToken, ValueTask<T>> Instance = static (callback, ct) => callback(ct);
    }
}
