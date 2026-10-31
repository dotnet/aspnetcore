// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.SqlServer;

/// <summary>
/// Distributed cache implementation using Microsoft SQL Server database.
/// </summary>
public class SqlServerCache : IDistributedCache, IBufferDistributedCache
{
    private static readonly TimeSpan MinimumExpiredItemsDeletionInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);

    private readonly IDatabaseOperations _dbOperations;
    private readonly ISystemClock _systemClock;
    private readonly TimeSpan _expiredItemsDeletionInterval;
    private DateTimeOffset _lastExpirationScan;
    private readonly Action _deleteExpiredCachedItemsDelegate;
    private readonly TimeSpan _defaultSlidingExpiration;
    private readonly Object _mutex = new Object();

    /// <summary>
    /// Initializes a new instance of <see cref="SqlServerCache"/>.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public SqlServerCache(IOptions<SqlServerCacheOptions> options)
    {
        var cacheOptions = options.Value;

        ArgumentThrowHelper.ThrowIfNullOrEmpty(cacheOptions.ConnectionString);
        ArgumentThrowHelper.ThrowIfNullOrEmpty(cacheOptions.SchemaName);
        ArgumentThrowHelper.ThrowIfNullOrEmpty(cacheOptions.TableName);

        if (cacheOptions.ExpiredItemsDeletionInterval.HasValue &&
            cacheOptions.ExpiredItemsDeletionInterval.Value < MinimumExpiredItemsDeletionInterval)
        {
            throw new ArgumentException(
                $"{nameof(SqlServerCacheOptions.ExpiredItemsDeletionInterval)} cannot be less than the minimum " +
                $"value of {MinimumExpiredItemsDeletionInterval.TotalMinutes} minutes.");
        }
        if (cacheOptions.DefaultSlidingExpiration <= TimeSpan.Zero)
        {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            throw new ArgumentOutOfRangeException(
                nameof(cacheOptions.DefaultSlidingExpiration),
                cacheOptions.DefaultSlidingExpiration,
                "The sliding expiration value must be positive.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }

        _systemClock = cacheOptions.SystemClock ?? new SystemClock();
        _expiredItemsDeletionInterval =
            cacheOptions.ExpiredItemsDeletionInterval ?? DefaultExpiredItemsDeletionInterval;
        _deleteExpiredCachedItemsDelegate = DeleteExpiredCacheItems;
        _defaultSlidingExpiration = cacheOptions.DefaultSlidingExpiration;

        _dbOperations = new DatabaseOperations(
            cacheOptions.ConnectionString,
            cacheOptions.SchemaName,
            cacheOptions.TableName,
            _systemClock);
    }

    /// <inheritdoc />
    public byte[]? Get(string key)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        var value = _dbOperations.GetCacheItem(key);

        ScanForExpiredItemsIfRequired();

        return value;
    }

    bool IBufferDistributedCache.TryGet(string key, IBufferWriter<byte> destination)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);
        ArgumentNullThrowHelper.ThrowIfNull(destination);

        var value = _dbOperations.TryGetCacheItem(key, destination);

        ScanForExpiredItemsIfRequired();

        return value;
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default(CancellationToken))
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        token.ThrowIfCancellationRequested();

        var value = await _dbOperations.GetCacheItemAsync(key, token).ConfigureAwait(false);

        ScanForExpiredItemsIfRequired();

        return value;
    }

    async ValueTask<bool> IBufferDistributedCache.TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);
        ArgumentNullThrowHelper.ThrowIfNull(destination);

        var value = await _dbOperations.TryGetCacheItemAsync(key, destination, token).ConfigureAwait(false);

        ScanForExpiredItemsIfRequired();

        return value;
    }

    /// <inheritdoc />
    public void Refresh(string key)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        _dbOperations.RefreshCacheItem(key);

        ScanForExpiredItemsIfRequired();
    }

    /// <inheritdoc />
    public async Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        token.ThrowIfCancellationRequested();

        await _dbOperations.RefreshCacheItemAsync(key, token).ConfigureAwait(false);

        ScanForExpiredItemsIfRequired();
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        _dbOperations.DeleteCacheItem(key);

        ScanForExpiredItemsIfRequired();
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);

        token.ThrowIfCancellationRequested();

        await _dbOperations.DeleteCacheItemAsync(key, token).ConfigureAwait(false);

        ScanForExpiredItemsIfRequired();
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);
        ArgumentNullThrowHelper.ThrowIfNull(value);
        ArgumentNullThrowHelper.ThrowIfNull(options);

        GetOptions(ref options);

        _dbOperations.SetCacheItem(key, new(value), options);

        ScanForExpiredItemsIfRequired();
    }

    void IBufferDistributedCache.Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);
        ArgumentNullThrowHelper.ThrowIfNull(options);

        GetOptions(ref options);

        _dbOperations.SetCacheItem(key, Linearize(value, out var lease), options);
        Recycle(lease); // we're fine to only recycle on success

        ScanForExpiredItemsIfRequired();
    }

    /// <inheritdoc />
    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default(CancellationToken))
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);
        ArgumentNullThrowHelper.ThrowIfNull(value);
        ArgumentNullThrowHelper.ThrowIfNull(options);

        token.ThrowIfCancellationRequested();

        GetOptions(ref options);

        await _dbOperations.SetCacheItemAsync(key, new(value), options, token).ConfigureAwait(false);

        ScanForExpiredItemsIfRequired();
    }

    async ValueTask IBufferDistributedCache.SetAsync(
        string key,
        ReadOnlySequence<byte> value,
        DistributedCacheEntryOptions options,
        CancellationToken token)
    {
        ArgumentNullThrowHelper.ThrowIfNull(key);
        ArgumentNullThrowHelper.ThrowIfNull(options);

        token.ThrowIfCancellationRequested();

        GetOptions(ref options);

        await _dbOperations.SetCacheItemAsync(key, Linearize(value, out var lease), options, token).ConfigureAwait(false);
        Recycle(lease); // we're fine to only recycle on success

        ScanForExpiredItemsIfRequired();
    }

    private static ArraySegment<byte> Linearize(in ReadOnlySequence<byte> value, out byte[]? lease)
    {
        if (value.IsEmpty)
        {
            lease = null;
            return new([], 0, 0);
        }

        // SqlClient only supports single-segment chunks via byte[] with offset/count; this will
        // almost never be an issue, but on those rare occasions: use a leased array to harmonize things
        if (value.IsSingleSegment && MemoryMarshal.TryGetArray(value.First, out var segment))
        {
            lease = null;
            return segment;
        }
        var length = checked((int)value.Length);
        lease = ArrayPool<byte>.Shared.Rent(length);
        value.CopyTo(lease);
        return new(lease, 0, length);
    }

    private static void Recycle(byte[]? lease)
    {
        if (lease is not null)
        {
            ArrayPool<byte>.Shared.Return(lease);
        }
    }

    // Called by multiple actions to see how long it's been since we last checked for expired items.
    // If sufficient time has elapsed then a scan is initiated on a background task.
    private void ScanForExpiredItemsIfRequired()
    {
        lock (_mutex)
        {
            var utcNow = _systemClock.UtcNow;
            if ((utcNow - _lastExpirationScan) > _expiredItemsDeletionInterval)
            {
                _lastExpirationScan = utcNow;
                Task.Run(_deleteExpiredCachedItemsDelegate);
            }
        }
    }

    private void DeleteExpiredCacheItems()
    {
        _dbOperations.DeleteExpiredCacheItems();
    }

    private void GetOptions(ref DistributedCacheEntryOptions options)
    {
        if (!options.AbsoluteExpiration.HasValue
            && !options.AbsoluteExpirationRelativeToNow.HasValue
            && !options.SlidingExpiration.HasValue)
        {
            options = new DistributedCacheEntryOptions()
            {
                SlidingExpiration = _defaultSlidingExpiration
            };
        }
    }
}
