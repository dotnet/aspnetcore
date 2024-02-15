// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Distributed;
internal sealed class DefaultReadThroughCache(IOptions<ReadThroughCacheOptions> options, IServiceProvider services, IMemoryCache frontent, IDistributedCache backend,
    TimeProvider clock) : ReadThroughCache
{
    private readonly IServiceProvider _services = services;
    private readonly IMemoryCache _frontend = frontent;
    private readonly IDistributedCache _backend = backend;

    private const int BackendBuffer = 1 << 0, BackendInvalidation = 1 << 1;
    private readonly int _backendFeatures // avoid repeated type tests
        = (backend is IBufferDistributedCache ? BackendBuffer : 0)
        | (backend is IDistributedCacheInvalidation ? BackendInvalidation : 0);

    private bool HasBackendBuffer => (_backendFeatures & BackendBuffer) != 0;
    private bool HasBackendInvalidation => (_backendFeatures & BackendInvalidation) != 0;

    private readonly ReadThroughCacheEntryOptions _defaultOptions = options.Value.DefaultOptions ?? new(TimeSpan.FromMinutes(1));
    private readonly TimeProvider _clock = clock;

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

    public override ValueTask<(bool Exists, T Value)> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (ReadOnlyTypeCache<T>.IsReadOnly)
        {
            if (_frontend.TryGetValue(key, out T? existing))
            {
                return new((true, existing!));
            }
        }
        else
        {
            if (_frontend.TryGetValue(key, out byte[]? buffer))
            {
                return new((true, GetSerializer<T>().Deserialize(new(buffer!))));
            }
        }

        return HasBackendBuffer
            ? GetBufferedBackendAsync<T>(key, cancellationToken)
            : GetLegacyBackendAsync<T>(key, cancellationToken);
    }
    public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback,
        ReadThroughCacheEntryOptions? options = null, ReadOnlyMemory<string> tags = default, CancellationToken cancellationToken = default)
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

        return HasBackendBuffer
            ? GetOrCreateBufferedBackendAsync(key, state, callback, options ?? _defaultOptions, cancellationToken)
            : GetOrCreateLegacyBackendAsync(key, state, callback, options ?? _defaultOptions, cancellationToken);
    }

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

    private ValueTask<T> GetOrCreateBufferedBackendAsync<TState, T>(string key, TState state,
        Func<TState, CancellationToken, ValueTask<T>> callback,
        ReadThroughCacheEntryOptions options, CancellationToken cancellationToken)
    {
        var buffer = new RecyclableArrayBufferWriter<byte>();
        Debug.Assert(_backend is IBufferDistributedCache); // pre-validated
        var pendingGet = Unsafe.As<IBufferDistributedCache>(_backend).TryGetAsync(key, buffer, cancellationToken);

        if (!pendingGet.IsCompletedSuccessfully)
        {
            return AwaitedBackend(this, key, state, callback, options, cancellationToken, buffer, pendingGet);
        }

        var tryGetResult = pendingGet.GetAwaiter().GetResult();
        // fast path; backend available immediately
        if (tryGetResult)
        {
            var result = DeserializeAndCacheFrontend<T>(key, options, buffer);
            buffer.Dispose();
            return new(result);
        }

        // fall back to main code-path, but without the pending bytes (we've already checked those)
        return AwaitedBackend(this, key, state, callback, options, cancellationToken, buffer, default);

        static async ValueTask<T> AwaitedBackend(DefaultReadThroughCache @this, string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, ReadThroughCacheEntryOptions options,
             CancellationToken cancellationToken, RecyclableArrayBufferWriter<byte> buffer, ValueTask<bool> pendingGet)
        {
            using (buffer)
            {
                var getResult = await pendingGet;
                if (getResult)
                {
                    return @this.DeserializeAndCacheFrontend<T>(key, options, buffer);
                }

                var value = await callback(state, cancellationToken);
                if (value is null)
                {
                    await @this._backend.RemoveAsync(key, cancellationToken);
                }
                else
                {
                    @this.SerializeAndCacheFrontend<T>(key, value, options, buffer, out _);
                    await Unsafe.As<IBufferDistributedCache>(@this._backend).SetAsync(key, new(buffer.GetCommittedMemory()), options.AsDistributedCacheEntryOptions(), cancellationToken);
                }

                return value;
            }
        }
    }

    private void SerializeAndCacheFrontend<T>(string key, T value, ReadThroughCacheEntryOptions options, RecyclableArrayBufferWriter<byte> buffer,
        out byte[]? arr)
    {
        buffer.Reset();
        GetSerializer<T>().Serialize(value, buffer);

        var expiry = ComputeExpiration(options);
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

    private DateTimeOffset ComputeExpiration(ReadThroughCacheEntryOptions options)
        => _clock.GetUtcNow().Add(options.Expiry);

    private T DeserializeAndCacheFrontend<T>(string key, ReadThroughCacheEntryOptions options,
        RecyclableArrayBufferWriter<byte> buffer)
    {
        var expiry = ComputeExpiration(options);
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

    private T DeserializeAndCacheFrontend<T>(string key, ReadThroughCacheEntryOptions options, byte[] buffer)
    {
        var expiry = ComputeExpiration(options);
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

    private ValueTask<T> GetOrCreateLegacyBackendAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, ReadThroughCacheEntryOptions options, CancellationToken cancellationToken)
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

        static async ValueTask<T> AwaitedBackend(DefaultReadThroughCache @this, string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, ReadThroughCacheEntryOptions options,
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
                await @this._backend.SetAsync(key, arr ?? buffer.ToArray(), options.AsDistributedCacheEntryOptions(), cancellationToken);
            }

            return value;
        }
    }

    public override ValueTask RemoveKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        _frontend.Remove(key);
        return new (_backend.RemoveAsync(key, cancellationToken));
    }

    public override ValueTask RemoveTagsAsync(ReadOnlyMemory<string> tags, CancellationToken cancellationToken = default)
    {
        // TODO: local invalidation

        if (HasBackendInvalidation)
        {
            Debug.Assert(_backend is IDistributedCacheInvalidation); // pre-validated
            return Unsafe.As<IDistributedCacheInvalidation>(_backend).RemoveTagsAsync(tags, cancellationToken);
        }
        else
        {
            return default;
        }
    }

    private ValueTask<(bool Exists, T Value)> GetBufferedBackendAsync<T>(string key, CancellationToken cancellationToken)
    {
        var buffer = new RecyclableArrayBufferWriter<byte>();
        Debug.Assert(_backend is IBufferDistributedCache); // pre-validated
        var pendingGet = Unsafe.As<IBufferDistributedCache>(_backend).TryGetAsync(key, buffer, cancellationToken);

        if (!pendingGet.IsCompletedSuccessfully)
        {
            return AwaitedBackend(this, key, buffer, pendingGet);
        }

        var tryGetResult = pendingGet.GetAwaiter().GetResult();
        // fast path; backend available immediately
        if (tryGetResult)
        {
            var result = DeserializeAndCacheFrontend<T>(key, _defaultOptions, buffer);
            buffer.Dispose();
            return new((true, result));
        }

        return default;

        static async ValueTask<(bool Exists, T Value)> AwaitedBackend(DefaultReadThroughCache @this, string key,
             RecyclableArrayBufferWriter<byte> buffer, ValueTask<bool> pendingGet)
        {
            using (buffer)
            {
                var getResult = await pendingGet;
                if (getResult)
                {
                    return (true, @this.DeserializeAndCacheFrontend<T>(key, @this._defaultOptions, buffer));
                }
            }
            return default;
        }
    }

    private ValueTask<(bool Exists, T Value)> GetLegacyBackendAsync<T>(string key, CancellationToken cancellationToken)
    {
        var pendingBytes = _backend.GetAsync(key, cancellationToken);
        if (!pendingBytes.IsCompletedSuccessfully)
        {
            return AwaitedBackend(this, key, pendingBytes);
        }

        // fast path; backend available immediately
        var bytes = pendingBytes.Result;
        if (bytes is not null)
        {
            return new((true, DeserializeAndCacheFrontend<T>(key, _defaultOptions, bytes)));
        }

        return default;

        static async ValueTask<(bool Exists, T Value)> AwaitedBackend(DefaultReadThroughCache @this, string key, Task<byte[]?> pendingBytes)
        {
            var bytes = await pendingBytes;
            if (bytes is not null)
            {
                return (true, @this.DeserializeAndCacheFrontend<T>(key, @this._defaultOptions, bytes));
            }

            return default;
        }
    }

}
