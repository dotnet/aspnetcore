// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Distributed;
internal sealed class ReadThroughCache(IOptions<ReadThroughCacheOptions> options, IServiceProvider services, IDistributedCache backend) : IReadThroughCache
{
    private readonly IServiceProvider _services = services;
    private readonly IDistributedCache _backend = backend;
    private readonly IBufferDistributedCache? _bufferBackend = backend as IBufferDistributedCache;
    private readonly DistributedCacheEntryOptions _defaultOptions = options.Value.DefaultOptions ?? new();

    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> callback, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => GetOrCreateAsync(key, callback, WrappedCallbackCache<T>.Instance, options, cancellationToken);

    public ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(callback);

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
                    break;
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
        var pendingGet = _bufferBackend!.TryGetAsync(key, buffer, cancellationToken);

        if (!pendingGet.IsCompletedSuccessfully)
        {
            return AwaitedBackend(this, key, state, callback, options, cancellationToken, buffer, pendingGet);
        }

        // fast path; backend available immediately
        if (pendingGet.GetAwaiter().GetResult())
        {
            var result = GetSerializer<T>().Deserialize(new(buffer.GetCommittedMemory()));
            buffer.Dispose();
            return new(result);
        }

        // fall back to main code-path, but without the pending bytes (we've already checked those)
        return AwaitedBackend(this, key, state, callback, options, cancellationToken, buffer, default);

        static async ValueTask<T> AwaitedBackend(ReadThroughCache @this, string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback, DistributedCacheEntryOptions options,
             CancellationToken cancellationToken, RecyclableArrayBufferWriter<byte> buffer, ValueTask<bool> pendingGet)
        {
            using (buffer)
            {
                if (await pendingGet)
                {
                    return @this.GetSerializer<T>().Deserialize(new(buffer.GetCommittedMemory()));
                }

                var value = await callback(state, cancellationToken);
                if (value is null)
                {
                    await @this._backend.RemoveAsync(key, cancellationToken);
                }
                else
                {
                    buffer.Reset();
                    @this.GetSerializer<T>().Serialize(value, buffer);
                    await @this._bufferBackend!.SetAsync(key, new(buffer.GetCommittedMemory()), options, cancellationToken);
                }

                return value;
            }
        }
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
            return new(GetSerializer<T>().Deserialize(new ReadOnlySequence<byte>(bytes))!);
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
                    return @this.GetSerializer<T>().Deserialize(new ReadOnlySequence<byte>(bytes));
                }
            }

            var value = await callback(state, cancellationToken);
            if (value is null)
            {
                await @this._backend.RemoveAsync(key, cancellationToken);
            }
            else
            {
                using var writer = new RecyclableArrayBufferWriter<byte>();
                @this.GetSerializer<T>().Serialize(value, writer);
                await @this._backend.SetAsync(key, writer.ToArray(), options, cancellationToken);
            }

            return value;
        }
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        => new(_backend.RemoveAsync(key, cancellationToken));

    private static class WrappedCallbackCache<T>
    {
        // for the simple usage scenario (no TState), pack the original callback as the "state", and use a wrapper function that just unrolls and invokes from the state
        public static readonly Func<Func<CancellationToken, ValueTask<T>>, CancellationToken, ValueTask<T>> Instance = static (callback, ct) => callback(ct);
    }
}
