// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

/// <summary>
/// Distributed cache implementation using Redis.
/// <para>Uses <c>StackExchange.Redis</c> as the Redis client.</para>
/// </summary>
public partial class RedisCache : IDistributedCache, IDisposable
{
    // Note that the "force reconnect" pattern as described https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/cache-best-practices-connection#using-forcereconnect-with-stackexchangeredis
    // can be enabled via the "Microsoft.AspNetCore.Caching.StackExchangeRedis.UseForceReconnect" app-context switch
    //
    // -- Explanation of why two kinds of SetScript are used --
    // * Redis 2.0 had HSET key field value for setting individual hash fields,
    // and HMSET key field value [field value ...] for setting multiple hash fields (against the same key).
    // * Redis 4.0 added HSET key field value [field value ...] and deprecated HMSET.
    //
    // On Redis versions that don't have the newer HSET variant, we use SetScriptPreExtendedSetCommand
    // which uses the (now deprecated) HMSET.

    // KEYS[1] = = key
    // ARGV[1] = absolute-expiration - ticks as long (-1 for none)
    // ARGV[2] = sliding-expiration - ticks as long (-1 for none)
    // ARGV[3] = relative-expiration (long, in seconds, -1 for none) - Min(absolute-expiration - Now, sliding-expiration)
    // ARGV[4] = data - byte[]
    // this order should not change LUA script depends on it
    private const string SetScript = (@"
                redis.call('HSET', KEYS[1], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', ARGV[4])
                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', KEYS[1], ARGV[3])
                end
                return 1");
    private const string SetScriptPreExtendedSetCommand = (@"
                redis.call('HMSET', KEYS[1], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', ARGV[4])
                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', KEYS[1], ARGV[3])
                end
                return 1");

    private const string AbsoluteExpirationKey = "absexp";
    private const string SlidingExpirationKey = "sldexp";
    private const string DataKey = "data";

    // combined keys - same hash keys fetched constantly; avoid allocating an array each time
    private static readonly RedisValue[] _hashMembersAbsoluteExpirationSlidingExpirationData = new RedisValue[] { AbsoluteExpirationKey, SlidingExpirationKey, DataKey };
    private static readonly RedisValue[] _hashMembersAbsoluteExpirationSlidingExpiration = new RedisValue[] { AbsoluteExpirationKey, SlidingExpirationKey };

    private static RedisValue[] GetHashFields(bool getData) => getData
        ? _hashMembersAbsoluteExpirationSlidingExpirationData
        : _hashMembersAbsoluteExpirationSlidingExpiration;

    private const long NotPresent = -1;
    private static readonly Version ServerVersionWithExtendedSetCommand = new Version(4, 0, 0);

    private volatile IDatabase? _cache;
    private bool _disposed;
    private string _setScript = SetScript;

    private readonly RedisCacheOptions _options;
    private readonly RedisKey _instancePrefix;
    private readonly ILogger _logger;

    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

    private long _lastConnectTicks = DateTimeOffset.UtcNow.Ticks;
    private long _firstErrorTimeTicks;
    private long _previousErrorTimeTicks;

    // StackExchange.Redis will also be trying to reconnect internally,
    // so limit how often we recreate the ConnectionMultiplexer instance
    // in an attempt to reconnect

    // Never reconnect within 60 seconds of the last attempt to connect or reconnect.
    private readonly TimeSpan ReconnectMinInterval = TimeSpan.FromSeconds(60);
    // Only reconnect if errors have occurred for at least the last 30 seconds.
    // This count resets if there are no errors for 30 seconds
    private readonly TimeSpan ReconnectErrorThreshold = TimeSpan.FromSeconds(30);

    private static DateTimeOffset ReadTimeTicks(ref long field)
    {
        var ticks = Volatile.Read(ref field); // avoid torn values
        return ticks == 0 ? DateTimeOffset.MinValue : new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    private static void WriteTimeTicks(ref long field, DateTimeOffset value)
    {
        var ticks = value == DateTimeOffset.MinValue ? 0L : value.UtcTicks;
        Volatile.Write(ref field, ticks); // avoid torn values
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RedisCache"/>.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    public RedisCache(IOptions<RedisCacheOptions> optionsAccessor)
        : this(optionsAccessor, Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<RedisCache>())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RedisCache"/>.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    /// <param name="logger">The logger.</param>
    internal RedisCache(IOptions<RedisCacheOptions> optionsAccessor, ILogger logger)
    {
        ArgumentNullThrowHelper.ThrowIfNull(optionsAccessor);
        ArgumentNullThrowHelper.ThrowIfNull(logger);

        _options = optionsAccessor.Value;
        _logger = logger;

        // This allows partitioning a single backend cache for use with multiple apps/services.
        var instanceName = _options.InstanceName;
        if (!string.IsNullOrEmpty(instanceName))
        {
            // SE.Redis allows efficient append of key-prefix scenarios, but we can help it
            // avoid some work/allocations by forcing the key-prefix to be a byte[]; SE.Redis
            // would do this itself anyway, using UTF8
            _instancePrefix = (RedisKey)Encoding.UTF8.GetBytes(instanceName);
        }
    }

    /// <inheritdoc />
    public byte[]? Get(string key)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        return GetAndRefresh(key, getData: true);
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        token.ThrowIfCancellationRequested();

        return await GetAndRefreshAsync(key, getData: true, token: token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);
        ArgumentNullThrowHelper.ThrowIfNull(value);
        ArgumentNullThrowHelper.ThrowIfNull(options);

        var cache = Connect();

        var creationTime = DateTimeOffset.UtcNow;

        var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

        try
        {
            cache.ScriptEvaluate(_setScript, new RedisKey[] { _instancePrefix.Append(key) },
                new RedisValue[]
                {
                        absoluteExpiration?.Ticks ?? NotPresent,
                        options.SlidingExpiration?.Ticks ?? NotPresent,
                        GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
                        value
                });
        }
        catch (Exception ex)
        {
            OnRedisError(ex, cache);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);
        ArgumentNullThrowHelper.ThrowIfNull(value);
        ArgumentNullThrowHelper.ThrowIfNull(options);

        token.ThrowIfCancellationRequested();

        var cache = await ConnectAsync(token).ConfigureAwait(false);
        Debug.Assert(cache is not null);

        var creationTime = DateTimeOffset.UtcNow;

        var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

        try
        {
            await cache.ScriptEvaluateAsync(_setScript, new RedisKey[] { _instancePrefix.Append(key) },
                new RedisValue[]
                {
                absoluteExpiration?.Ticks ?? NotPresent,
                options.SlidingExpiration?.Ticks ?? NotPresent,
                GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
                value
                }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnRedisError(ex, cache);
            throw;
        }
    }

    /// <inheritdoc />
    public void Refresh(string key)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        GetAndRefresh(key, getData: false);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        token.ThrowIfCancellationRequested();

        await GetAndRefreshAsync(key, getData: false, token: token).ConfigureAwait(false);
    }

    [MemberNotNull(nameof(_cache))]
    private IDatabase Connect()
    {
        CheckDisposed();
        var cache = _cache;
        if (cache is not null)
        {
            Debug.Assert(_cache is not null);
            return cache;
        }

        _connectionLock.Wait();
        try
        {
            cache = _cache;
            if (cache is null)
            {
                IConnectionMultiplexer connection;
                if (_options.ConnectionMultiplexerFactory is null)
                {
                    if (_options.ConfigurationOptions is not null)
                    {
                        connection = ConnectionMultiplexer.Connect(_options.ConfigurationOptions);
                    }
                    else
                    {
                        connection = ConnectionMultiplexer.Connect(_options.Configuration!);
                    }
                }
                else
                {
                    connection = _options.ConnectionMultiplexerFactory().GetAwaiter().GetResult();
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

    private void PrepareConnection(IConnectionMultiplexer connection)
    {
        WriteTimeTicks(ref _lastConnectTicks, DateTimeOffset.UtcNow);
        ValidateServerFeatures(connection);
        TryRegisterProfiler(connection);
    }

    private void ValidateServerFeatures(IConnectionMultiplexer connection)
    {
        _ = connection ?? throw new InvalidOperationException($"{nameof(connection)} cannot be null.");

        try
        {
            foreach (var endPoint in connection.GetEndPoints())
            {
                if (connection.GetServer(endPoint).Version < ServerVersionWithExtendedSetCommand)
                {
                    _setScript = SetScriptPreExtendedSetCommand;
                    return;
                }
            }
        }
        catch (NotSupportedException ex)
        {
            Log.CouldNotDetermineServerVersion(_logger, ex);

            // The GetServer call may not be supported with some configurations, in which
            // case let's also fall back to using the older command.
            _setScript = SetScriptPreExtendedSetCommand;
        }
    }

    private void TryRegisterProfiler(IConnectionMultiplexer connection)
    {
        _ = connection ?? throw new InvalidOperationException($"{nameof(connection)} cannot be null.");

        if (_options.ProfilingSession is not null)
        {
            connection.RegisterProfiler(_options.ProfilingSession);
        }
    }

    private byte[]? GetAndRefresh(string key, bool getData)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        var cache = Connect();

        // This also resets the LRU status as desired.
        // TODO: Can this be done in one operation on the server side? Probably, the trick would just be the DateTimeOffset math.
        RedisValue[] results;
        try
        {
            results = cache.HashGet(_instancePrefix.Append(key), GetHashFields(getData));
        }
        catch (Exception ex)
        {
            OnRedisError(ex, cache);
            throw;
        }

        if (results.Length >= 2)
        {
            MapMetadata(results, out DateTimeOffset? absExpr, out TimeSpan? sldExpr);
            Refresh(cache, key, absExpr, sldExpr);
        }

        if (results.Length >= 3 && results[2].HasValue)
        {
            return results[2];
        }

        return null;
    }

    private async Task<byte[]?> GetAndRefreshAsync(string key, bool getData, CancellationToken token = default)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        token.ThrowIfCancellationRequested();

        var cache = await ConnectAsync(token).ConfigureAwait(false);
        Debug.Assert(cache is not null);

        // This also resets the LRU status as desired.
        // TODO: Can this be done in one operation on the server side? Probably, the trick would just be the DateTimeOffset math.
        RedisValue[] results;
        try
        {
            results = await cache.HashGetAsync(_instancePrefix.Append(key), GetHashFields(getData)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnRedisError(ex, cache);
            throw;
        }

        if (results.Length >= 2)
        {
            MapMetadata(results, out DateTimeOffset? absExpr, out TimeSpan? sldExpr);
            await RefreshAsync(cache, key, absExpr, sldExpr, token).ConfigureAwait(false);
        }

        if (results.Length >= 3 && results[2].HasValue)
        {
            return results[2];
        }

        return null;
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        var cache = Connect();
        try
        {
            cache.KeyDelete(_instancePrefix.Append(key));
        }
        catch (Exception ex)
        {
            OnRedisError(ex, cache);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        var cache = await ConnectAsync(token).ConfigureAwait(false);
        Debug.Assert(cache is not null);

        try
        {
            await cache.KeyDeleteAsync(_instancePrefix.Append(key)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnRedisError(ex, cache);
            throw;
        }
    }

    private static void MapMetadata(RedisValue[] results, out DateTimeOffset? absoluteExpiration, out TimeSpan? slidingExpiration)
    {
        absoluteExpiration = null;
        slidingExpiration = null;
        var absoluteExpirationTicks = (long?)results[0];
        if (absoluteExpirationTicks.HasValue && absoluteExpirationTicks.Value != NotPresent)
        {
            absoluteExpiration = new DateTimeOffset(absoluteExpirationTicks.Value, TimeSpan.Zero);
        }
        var slidingExpirationTicks = (long?)results[1];
        if (slidingExpirationTicks.HasValue && slidingExpirationTicks.Value != NotPresent)
        {
            slidingExpiration = new TimeSpan(slidingExpirationTicks.Value);
        }
    }

    private void Refresh(IDatabase cache, string key, DateTimeOffset? absExpr, TimeSpan? sldExpr)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        // Note Refresh has no effect if there is just an absolute expiration (or neither).
        if (sldExpr.HasValue)
        {
            TimeSpan? expr;
            if (absExpr.HasValue)
            {
                var relExpr = absExpr.Value - DateTimeOffset.Now;
                expr = relExpr <= sldExpr.Value ? relExpr : sldExpr;
            }
            else
            {
                expr = sldExpr;
            }
            try
            {
                cache.KeyExpire(_instancePrefix.Append(key), expr);
            }
            catch (Exception ex)
            {
                OnRedisError(ex, cache);
                throw;
            }
        }
    }

    private async Task RefreshAsync(IDatabase cache, string key, DateTimeOffset? absExpr, TimeSpan? sldExpr, CancellationToken token = default)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        token.ThrowIfCancellationRequested();

        // Note Refresh has no effect if there is just an absolute expiration (or neither).
        if (sldExpr.HasValue)
        {
            TimeSpan? expr;
            if (absExpr.HasValue)
            {
                var relExpr = absExpr.Value - DateTimeOffset.Now;
                expr = relExpr <= sldExpr.Value ? relExpr : sldExpr;
            }
            else
            {
                expr = sldExpr;
            }
            try
            {
                await cache.KeyExpireAsync(_instancePrefix.Append(key), expr).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnRedisError(ex, cache);
                throw;
            }
        }
    }

    private static long? GetExpirationInSeconds(DateTimeOffset creationTime, DateTimeOffset? absoluteExpiration, DistributedCacheEntryOptions options)
    {
        if (absoluteExpiration.HasValue && options.SlidingExpiration.HasValue)
        {
            return (long)Math.Min(
                (absoluteExpiration.Value - creationTime).TotalSeconds,
                options.SlidingExpiration.Value.TotalSeconds);
        }
        else if (absoluteExpiration.HasValue)
        {
            return (long)(absoluteExpiration.Value - creationTime).TotalSeconds;
        }
        else if (options.SlidingExpiration.HasValue)
        {
            return (long)options.SlidingExpiration.Value.TotalSeconds;
        }
        return null;
    }

    private static DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset creationTime, DistributedCacheEntryOptions options)
    {
        if (options.AbsoluteExpiration.HasValue && options.AbsoluteExpiration <= creationTime)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
                options.AbsoluteExpiration.Value,
                "The absolute expiration value must be in the future.");
        }

        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            return creationTime + options.AbsoluteExpirationRelativeToNow;
        }

        return options.AbsoluteExpiration;
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

    private void CheckDisposed()
    {
        ObjectDisposedThrowHelper.ThrowIf(_disposed, this);
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
