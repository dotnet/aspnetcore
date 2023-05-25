// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET7_0_OR_GREATER // IOutputCacheStore only exists from net7

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

internal class RedisOutputCacheStore : IOutputCacheStore, IDisposable
{
    private readonly RedisCacheOptions _options;
    private readonly ILogger _logger;
    private readonly RedisKey _valueKeyPrefix, _tagKeyPrefix, _tagMasterKey;
    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

    private bool _disposed;
    private volatile IDatabase? _cache;
    private long _lastConnectTicks = DateTimeOffset.UtcNow.Ticks;
    private long _firstErrorTimeTicks;
    private long _previousErrorTimeTicks;
    private bool _useMultiExec, _use62Features;

    // Never reconnect within 60 seconds of the last attempt to connect or reconnect.
    private readonly TimeSpan ReconnectMinInterval = TimeSpan.FromSeconds(60);
    // Only reconnect if errors have occurred for at least the last 30 seconds.
    // This count resets if there are no errors for 30 seconds
    private readonly TimeSpan ReconnectErrorThreshold = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Initializes a new instance of <see cref="RedisCache"/>.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    public RedisOutputCacheStore(IOptions<RedisCacheOptions> optionsAccessor) // TODO: OC-specific options?
        : this(optionsAccessor, Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<RedisCache>())
    {
    }

    internal async ValueTask<string> GetConfigurationInfo(CancellationToken cancellationToken = default)
    {
        await ConnectAsync(cancellationToken).ConfigureAwait(false);
        return $"redis output-cache; MULTI/EXEC: {_useMultiExec}, v6.2+: {_use62Features}";
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RedisCache"/>.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    /// <param name="logger">The logger.</param>
    internal RedisOutputCacheStore(IOptions<RedisCacheOptions> optionsAccessor, ILogger logger)
    {
        ArgumentNullThrowHelper.ThrowIfNull(optionsAccessor);
        ArgumentNullThrowHelper.ThrowIfNull(logger);

        _options = optionsAccessor.Value;
        _logger = logger;

        // This allows partitioning a single backend cache for use with multiple apps/services.

        // SE.Redis allows efficient append of key-prefix scenarios, but we can help it
        // avoid some work/allocations by forcing the key-prefix to be a byte[]; SE.Redis
        // would do this itself anyway, using UTF8
        _valueKeyPrefix = (RedisKey)Encoding.UTF8.GetBytes(_options.InstanceName + "__MSOCV_");
        _tagKeyPrefix = (RedisKey)Encoding.UTF8.GetBytes(_options.InstanceName + "__MSOCT_");
        _tagMasterKey = (RedisKey)Encoding.UTF8.GetBytes(_options.InstanceName + "__MSOCT");
    }

    ValueTask IOutputCacheStore.EvictByTagAsync(string tag, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    private RedisKey GetValueKey(string key)
        => _valueKeyPrefix.Append(key);

    async ValueTask<byte[]?> IOutputCacheStore.GetAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        var cache = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        Debug.Assert(cache is not null);

        try
        {
            return (byte[]?)(await cache.StringGetAsync(GetValueKey(key)).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            OnRedisError(ex, cache);
            throw;
        }
    }

    async ValueTask IOutputCacheStore.SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
    {
        if (tags is not null && tags.Length > 0)
        {
            throw new NotImplementedException("tags");
        }

        var cache = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        Debug.Assert(cache is not null);

        await cache.StringSetAsync(GetValueKey(key), value, validFor).ConfigureAwait(false);
    }

    async ValueTask IOutputCacheStore.SetAsync(string key, ReadOnlySequence<byte> value, IReadOnlySet<string>? tags, TimeSpan validFor, CancellationToken cancellationToken)
    {
        if (tags is not null && tags.Count > 0)
        {
            throw new NotImplementedException("tags");
        }

        var cache = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        Debug.Assert(cache is not null);

        byte[]? leased = null;
        ReadOnlyMemory<byte> singleChunk;
        if (value.IsSingleSegment)
        {
            singleChunk = value.First;
        }
        else
        {
            int len = checked((int)value.Length);
            leased = ArrayPool<byte>.Shared.Rent(len);
            value.CopyTo(leased);
            singleChunk = new(leased, 0, len);
        }

        await cache.StringSetAsync(GetValueKey(key), singleChunk, validFor).ConfigureAwait(false);

        // only return lease on success
        if (leased is not null)
        {
            ArrayPool<byte>.Shared.Return(leased);
        }
    }

    private ValueTask<IDatabase> ConnectAsync(CancellationToken token = default)
    {
        CheckDisposed();
        token.ThrowIfCancellationRequested();

        var cache = _cache;
        if (cache is not null)
        {
            Debug.Assert(_cache is not null);
            return new(cache);
        }
        return ConnectSlowAsync(token);
    }
    private async ValueTask<IDatabase> ConnectSlowAsync(CancellationToken token)
    {
        await _connectionLock.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var cache = _cache;
            if (cache is null)
            {
                IConnectionMultiplexer connection;
                if (_options.ConnectionMultiplexerFactory is null)
                {
                    if (_options.ConfigurationOptions is not null)
                    {
                        connection = await ConnectionMultiplexer.ConnectAsync(_options.ConfigurationOptions).ConfigureAwait(false);
                    }
                    else
                    {
                        connection = await ConnectionMultiplexer.ConnectAsync(_options.Configuration!).ConfigureAwait(false);
                    }
                }
                else
                {
                    connection = await _options.ConnectionMultiplexerFactory().ConfigureAwait(false);
                }

                PrepareConnection(connection);
                cache = _cache = connection.GetDatabase();
            }
            Debug.Assert(_cache is not null);
            return cache;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ReleaseConnection(Interlocked.Exchange(ref _cache, null));
    }

    private void OnRedisError(Exception exception, IDatabase cache)
    {
        if (_options.UseForceReconnect && (exception is RedisConnectionException or SocketException))
        {
            var utcNow = DateTimeOffset.UtcNow;
            var previousConnectTime = ReadTimeTicks(ref _lastConnectTicks);
            TimeSpan elapsedSinceLastReconnect = utcNow - previousConnectTime;

            // We want to limit how often we perform this top-level reconnect, so we check how long it's been since our last attempt.
            if (elapsedSinceLastReconnect < ReconnectMinInterval)
            {
                return;
            }

            var firstErrorTime = ReadTimeTicks(ref _firstErrorTimeTicks);
            if (firstErrorTime == DateTimeOffset.MinValue)
            {
                // note: order/timing here (between the two fields) is not critical
                WriteTimeTicks(ref _firstErrorTimeTicks, utcNow);
                WriteTimeTicks(ref _previousErrorTimeTicks, utcNow);
                return;
            }

            TimeSpan elapsedSinceFirstError = utcNow - firstErrorTime;
            TimeSpan elapsedSinceMostRecentError = utcNow - ReadTimeTicks(ref _previousErrorTimeTicks);

            bool shouldReconnect =
                    elapsedSinceFirstError >= ReconnectErrorThreshold // Make sure we gave the multiplexer enough time to reconnect on its own if it could.
                    && elapsedSinceMostRecentError <= ReconnectErrorThreshold; // Make sure we aren't working on stale data (e.g. if there was a gap in errors, don't reconnect yet).

            // Update the previousErrorTime timestamp to be now (e.g. this reconnect request).
            WriteTimeTicks(ref _previousErrorTimeTicks, utcNow);

            if (!shouldReconnect)
            {
                return;
            }

            WriteTimeTicks(ref _firstErrorTimeTicks, DateTimeOffset.MinValue);
            WriteTimeTicks(ref _previousErrorTimeTicks, DateTimeOffset.MinValue);

            // wipe the shared field, but *only* if it is still the cache we were
            // thinking about (once it is null, the next caller will reconnect)
            ReleaseConnection(Interlocked.CompareExchange(ref _cache, null, cache));
        }
    }

    private void PrepareConnection(IConnectionMultiplexer connection)
    {
        WriteTimeTicks(ref _lastConnectTicks, DateTimeOffset.UtcNow);
        ValidateServerFeatures(connection);
        TryRegisterProfiler(connection);
    }

    private void ValidateServerFeatures(IConnectionMultiplexer connection)
    {
        int serverCount = 0, standaloneCount = 0, v62_Count = 0;
        foreach (var ep in connection.GetEndPoints())
        {
            var server = connection.GetServer(ep);
            if (server is null)
            {
                continue; // wat?
            }
            serverCount++;
            if (server.ServerType == ServerType.Standalone)
            {
                standaloneCount++;
            }
            if (server.Features.SortedSetRangeStore) // just a random v6.2 feature
            {
                v62_Count++;
            }
        }
        _useMultiExec = serverCount == standaloneCount;
        _use62Features = serverCount == v62_Count;
    }

    private void TryRegisterProfiler(IConnectionMultiplexer connection)
    {
        _ = connection ?? throw new InvalidOperationException($"{nameof(connection)} cannot be null.");

        if (_options.ProfilingSession is not null)
        {
            connection.RegisterProfiler(_options.ProfilingSession);
        }
    }

    private static void WriteTimeTicks(ref long field, DateTimeOffset value)
    {
        var ticks = value == DateTimeOffset.MinValue ? 0L : value.UtcTicks;
        Volatile.Write(ref field, ticks); // avoid torn values
    }

    private void CheckDisposed()
    {
        ObjectDisposedThrowHelper.ThrowIf(_disposed, this);
    }

    private static DateTimeOffset ReadTimeTicks(ref long field)
    {
        var ticks = Volatile.Read(ref field); // avoid torn values
        return ticks == 0 ? DateTimeOffset.MinValue : new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    static void ReleaseConnection(IDatabase? cache)
    {
        var connection = cache?.Multiplexer;
        if (connection is not null)
        {
            try
            {
                connection.Close();
                connection.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
#endif
