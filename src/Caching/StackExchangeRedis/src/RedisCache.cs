// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
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
    // * Key timeouts are used in the following way:
    //
    // * If an entry has no absolute expiration time or sliding window, then
    //   the key in Redis has no timeout value.
    //
    // * If an entry has only an absolute expiration time, then the key in Redis
    //   has a corresponding timeout.
    //
    // * If an entry has a sliding window, then the key in Redis has a timeout value
    //   corresponding to the sliding window. Every fetch of that entry resets the
    //   key's timeout to (now + sliding window). This is potentially limited by an
    //   absolute expiration time, which sets an upper bound.

    private readonly Func<string, string> _expandKey;
    private readonly RedisCacheOptions _options;
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;

    // Field names
    private const string DataValueLabel = "dv";
    private const string SlidingWindowLabel = "swv";
    private const string AbsoluteExpirationValueLabel = "aev";

    // Expiration delay under which we don't try to update or refresh a key
    private static readonly TimeSpan _gracePeriod = TimeSpan.FromSeconds(1);

    private static readonly RedisValue[] _getLabels = new[]
    {
        new RedisValue(DataValueLabel),
        new RedisValue(SlidingWindowLabel),
        new RedisValue(AbsoluteExpirationValueLabel)
    };

    private static readonly RedisValue[] _refreshLabels = new[]
    {
        new RedisValue(SlidingWindowLabel),
        new RedisValue(AbsoluteExpirationValueLabel)
    };

    private volatile IConnectionMultiplexer? _connection;
    private IDatabase? _cache;
    private bool _disposed;

    private readonly SemaphoreSlim _connectionLock = new(initialCount: 1, maxCount: 1);

    /// <summary>
    /// Initializes a new instance of <see cref="RedisCache"/>.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    public RedisCache(IOptions<RedisCacheOptions> optionsAccessor)
        : this(optionsAccessor, Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<RedisCache>(), new SystemClock())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RedisCache"/>.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="clock">The system clock.</param>
    internal RedisCache(IOptions<RedisCacheOptions> optionsAccessor, ILogger logger, ISystemClock clock)
    {
        if (optionsAccessor == null)
        {
            throw new ArgumentNullException(nameof(optionsAccessor));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        _options = optionsAccessor.Value;
        _logger = logger;
        _clock = clock;

        // This allows partitioning a single backend cache for use with multiple apps/services.
        if (string.IsNullOrEmpty(_options.InstanceName))
        {
            _expandKey = key => key;
        }
        else
        {
            _expandKey = key => _options.InstanceName + key;
        }
    }

    /// <inheritdoc />
    public byte[]? Get(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var expandedKey = _expandKey(key);

        Connect();

        var value = _cache.HashGet(expandedKey, _getLabels);

        var (data, absoluteExpiration, slidingWindow) = RedisEntry.Get(value);

        if (data != null)
        {
            RefreshExpiration(expandedKey, absoluteExpiration, slidingWindow);
        }

        return data;
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default(CancellationToken))
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        token.ThrowIfCancellationRequested();

        var expandedKey = _expandKey(key);

        await ConnectAsync(token).ConfigureAwait(false);
        Debug.Assert(_cache != null);

        var value = await _cache.HashGetAsync(expandedKey, _getLabels)
#if NET6_0_OR_GREATER
        .WaitAsync(token)
#endif
        .ConfigureAwait(false);

        var (data, absoluteExpiration, slidingWindow) = RedisEntry.Get(value);

        if (data != null)
        {
            await RefreshExpirationAsync(expandedKey, absoluteExpiration, slidingWindow, token).ConfigureAwait(false);
        }

        return data;
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        // Create an
        var tx = MakeSetTransaction(key, value, options);

        _ = tx.Execute();
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        token.ThrowIfCancellationRequested();

        var transaction = MakeSetTransaction(key, value, options);

        _ = await transaction
            .ExecuteAsync()
#if NET6_0_OR_GREATER
        .WaitAsync(token)
#endif
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Refresh(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var expandedKey = _expandKey(key);

        Connect();

        var value = _cache.HashGet(expandedKey, _refreshLabels);

        var (_, absoluteExpiration, slidingWindow) = RedisEntry.Refresh(value);

        if (slidingWindow == null)
        {
            // key not found
            return;
        }

        RefreshExpiration(expandedKey, absoluteExpiration, slidingWindow);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        token.ThrowIfCancellationRequested();

        var expandedKey = _expandKey(key);

        await ConnectAsync(token).ConfigureAwait(false);
        Debug.Assert(_cache != null);

        var value = await _cache.HashGetAsync(expandedKey, _refreshLabels)
#if NET6_0_OR_GREATER
        .WaitAsync(token)
#endif
        .ConfigureAwait(false);

        var (_, absoluteExpiration, slidingWindow) = RedisEntry.Refresh(value);

        if (slidingWindow == null)
        {
            // key not found
            return;
        }

        await RefreshExpirationAsync(expandedKey, absoluteExpiration, slidingWindow, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Create a Redis transaction containing DEL, HSET and EXPIRE commands.
    /// </summary>
    private ITransaction MakeSetTransaction(string key, RedisValue value, DistributedCacheEntryOptions options)
    {
        // All the commands in a transaction are serialized and executed sequentially.
        // A request sent by another client will never be served in the middle of the execution
        // of a Redis Transaction. This guarantees that the commands are executed as a single
        // isolated operation.

        var currentTime = _clock.UtcNow;
        var (absExpiration, slidingWindow) = CalcExpirationValues(currentTime, options);
        var keyTimeout = CalcKeyTimeout(currentTime, absExpiration, slidingWindow);
        var expandedKey = _expandKey(key);

        Connect();

        // The ITransaction class exposes async methods because the result of
        // each operation will not be known until after `Execute` (or `ExecuteAsync`)
        // has completed.
        // Results for these operations are discarded because we do not care about
        // their individual outcome.

        var tx = _cache.CreateTransaction();
        _ = tx.KeyDeleteAsync(expandedKey);

        if (keyTimeout == TimeSpan.Zero || keyTimeout >= _gracePeriod)
        {
            if (slidingWindow != null)
            {
                if (absExpiration != null)
                {
                    var hashes = new[]
                    {
                        new HashEntry(DataValueLabel, value),
                        new HashEntry(SlidingWindowLabel, slidingWindow.Value.Ticks),
                        new HashEntry(AbsoluteExpirationValueLabel,  absExpiration.Value.Ticks)
                    };
                    _ = tx.HashSetAsync(expandedKey, hashes);
                }
                else
                {
                    var hashes = new[]
                    {
                        new HashEntry(DataValueLabel, value),
                        new HashEntry(SlidingWindowLabel, slidingWindow.Value.Ticks),
                    };
                    _ = tx.HashSetAsync(expandedKey, hashes);
                }
            }
            else
            {
                _ = tx.HashSetAsync(expandedKey, DataValueLabel, value);
            }

            if (keyTimeout != TimeSpan.Zero)
            {
                _ = tx.KeyExpireAsync(expandedKey, keyTimeout);
            }
        }

        return tx;
    }

    // Generate the two optional expiration values to inject into Redis
    private static (DateTimeOffset? absExpiration, TimeSpan? slidingWindow) CalcExpirationValues(DateTimeOffset currentTime, DistributedCacheEntryOptions options)
    {
        var absExpiration = options.AbsoluteExpiration;
        if (options.AbsoluteExpirationRelativeToNow != null)
        {
            absExpiration = currentTime + options.AbsoluteExpirationRelativeToNow.Value;
        }

        return (absExpiration, options.SlidingExpiration);
    }

    // Given the calculated expiration values, figure out the timeout value that should be applied to a Redis key
    private static TimeSpan CalcKeyTimeout(DateTimeOffset currentTime, DateTimeOffset? absExpiration, TimeSpan? slidingWindow)
    {
        if (slidingWindow != null)
        {
            if (absExpiration != null)
            {
                var absWindow = absExpiration.Value - currentTime;
                if (absWindow < slidingWindow.Value)
                {
                    return absWindow;
                }
            }

            return slidingWindow.Value;
        }

        if (absExpiration != null)
        {
            return absExpiration.Value - currentTime;
        }

        return TimeSpan.Zero;
    }

    [MemberNotNull(nameof(_cache), nameof(_connection))]
    private void Connect()
    {
        CheckDisposed();
        if (_cache != null)
        {
            Debug.Assert(_connection != null);
            return;
        }

        _connectionLock.Wait();
        try
        {
            if (_cache == null)
            {
                if (_options.ConnectionMultiplexerFactory == null)
                {
                    if (_options.ConfigurationOptions is not null)
                    {
                        _connection = ConnectionMultiplexer.Connect(_options.ConfigurationOptions);
                    }
                    else
                    {
                        _connection = ConnectionMultiplexer.Connect(_options.Configuration);
                    }
                }
                else
                {
                    _connection = _options.ConnectionMultiplexerFactory().GetAwaiter().GetResult();
                }

                PrepareConnection();
                _cache = _connection.GetDatabase();
            }
        }
        finally
        {
            _connectionLock.Release();
        }

        Debug.Assert(_connection != null);
    }

    private async Task ConnectAsync(CancellationToken token = default(CancellationToken))
    {
        CheckDisposed();
        token.ThrowIfCancellationRequested();

        if (_cache != null)
        {
            Debug.Assert(_connection != null);
            return;
        }

        try
        {
            await _connectionLock.WaitAsync(token).ConfigureAwait(false);

            if (_cache == null)
            {
                if (_options.ConnectionMultiplexerFactory is null)
                {
                    if (_options.ConfigurationOptions is not null)
                    {
                        _connection = await ConnectionMultiplexer.ConnectAsync(_options.ConfigurationOptions).ConfigureAwait(false);
                    }
                    else
                    {
                        _connection = await ConnectionMultiplexer.ConnectAsync(_options.Configuration).ConfigureAwait(false);
                    }
                }
                else
                {
                    _connection = await _options.ConnectionMultiplexerFactory().ConfigureAwait(false);
                }

                PrepareConnection();
                _cache = _connection.GetDatabase();
            }
        }
        finally
        {
            _connectionLock.Release();
        }

        Debug.Assert(_connection != null);
    }

    private void PrepareConnection()
    {
        TryRegisterProfiler();
    }

    private void TryRegisterProfiler()
    {
        _ = _connection ?? throw new InvalidOperationException($"{nameof(_connection)} cannot be null.");

        if (_options.ProfilingSession != null)
        {
            _connection.RegisterProfiler(_options.ProfilingSession);
        }
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        Connect();

        _ = _cache.KeyDelete(_expandKey(key));
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        await ConnectAsync(token).ConfigureAwait(false);
        Debug.Assert(_cache is not null);

        await _cache.KeyDeleteAsync(_expandKey(key))
#if NET6_0_OR_GREATER
        .WaitAsync(token)
#endif
        .ConfigureAwait(false);
    }

    private void RefreshExpiration(string expandedKey, DateTimeOffset? absExpiration, TimeSpan? slidingWindow)
    {
        if (slidingWindow != null)
        {
            var timeout = CalcKeyTimeout(_clock.UtcNow, absExpiration, slidingWindow);

            if (timeout >= _gracePeriod)
            {
                Connect();

                _ = _cache.KeyExpire(expandedKey, timeout, CommandFlags.FireAndForget);
            }
        }
    }

    private Task RefreshExpirationAsync(string expandedKey, DateTimeOffset? absExpiration, TimeSpan? slidingWindow, CancellationToken token)
    {
        Debug.Assert(_cache != null);

        if (slidingWindow != null)
        {
            var timeout = CalcKeyTimeout(_clock.UtcNow, absExpiration, slidingWindow);

            if (timeout >= _gracePeriod)
            {
                return _cache.KeyExpireAsync(expandedKey, timeout, CommandFlags.FireAndForget)
#if NET6_0_OR_GREATER
                    .WaitAsync(token)
#endif
                    ;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _connection?.Close();
        GC.SuppressFinalize(this);
    }

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }
    }
}
