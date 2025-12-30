// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Caching.SqlServer;

internal sealed class DatabaseOperations : IDatabaseOperations
{
    /// <summary>
    /// Since there is no specific exception type representing a 'duplicate key' error, we are relying on
    /// the following message number which represents the following text in Microsoft SQL Server database.
    ///     "Violation of %ls constraint '%.*ls'. Cannot insert duplicate key in object '%.*ls'.
    ///     The duplicate key value is %ls."
    /// You can find the list of system messages by executing the following query:
    /// "SELECT * FROM sys.messages WHERE [text] LIKE '%duplicate%'"
    /// </summary>
    private const int DuplicateKeyErrorId = 2627;

    private const string UtcNowParameterName = "UtcNow";

    public DatabaseOperations(
        string connectionString, string schemaName, string tableName, ISystemClock systemClock)
    {
        ConnectionString = connectionString;
        SchemaName = schemaName;
        TableName = tableName;
        SystemClock = systemClock;
        SqlQueries = new SqlQueries(schemaName, tableName);
    }

    internal SqlQueries SqlQueries { get; }

    internal string ConnectionString { get; }

    internal string SchemaName { get; }

    internal string TableName { get; }

    private ISystemClock SystemClock { get; }

    public void DeleteCacheItem(string key)
    {
        using (var connection = new SqlConnection(ConnectionString))
        using (var command = new SqlCommand(SqlQueries.DeleteCacheItem, connection))
        {
            command.Parameters.AddCacheItemId(key);

            connection.Open();

            command.ExecuteNonQuery();
        }
    }

    public async Task DeleteCacheItemAsync(string key, CancellationToken token = default(CancellationToken))
    {
        token.ThrowIfCancellationRequested();

        using (var connection = new SqlConnection(ConnectionString))
        using (var command = new SqlCommand(SqlQueries.DeleteCacheItem, connection))
        {
            command.Parameters.AddCacheItemId(key);

            await connection.OpenAsync(token).ConfigureAwait(false);

            await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }
    }

    public byte[]? GetCacheItem(string key)
    {
        return GetCacheItem(key, includeValue: true);
    }

    public bool TryGetCacheItem(string key, IBufferWriter<byte> destination)
    {
        return GetCacheItem(key, includeValue: true, destination: destination) is not null;
    }

    public Task<byte[]?> GetCacheItemAsync(string key, CancellationToken token = default(CancellationToken))
    {
        token.ThrowIfCancellationRequested();

        return GetCacheItemAsync(key, includeValue: true, token: token);
    }

    public async Task<bool> TryGetCacheItemAsync(string key, IBufferWriter<byte> destination, CancellationToken token = default(CancellationToken))
    {
        token.ThrowIfCancellationRequested();

        var arr = await GetCacheItemAsync(key, includeValue: true, destination: destination, token: token).ConfigureAwait(false);
        return arr is not null;
    }

    public void RefreshCacheItem(string key)
    {
        GetCacheItem(key, includeValue: false);
    }

    public Task RefreshCacheItemAsync(string key, CancellationToken token = default(CancellationToken))
    {
        token.ThrowIfCancellationRequested();

        return GetCacheItemAsync(key, includeValue: false, token: token);
    }

    public void DeleteExpiredCacheItems()
    {
        var utcNow = SystemClock.UtcNow;

        using (var connection = new SqlConnection(ConnectionString))
        using (var command = new SqlCommand(SqlQueries.DeleteExpiredCacheItems, connection))
        {
            command.Parameters.AddWithValue(UtcNowParameterName, SqlDbType.DateTimeOffset, utcNow);

            connection.Open();

            var effectedRowCount = command.ExecuteNonQuery();
        }
    }

    public void SetCacheItem(string key, ArraySegment<byte> value, DistributedCacheEntryOptions options)
    {
        var utcNow = SystemClock.UtcNow;

        var absoluteExpiration = DatabaseOperations.GetAbsoluteExpiration(utcNow, options);
        DatabaseOperations.ValidateOptions(options.SlidingExpiration, absoluteExpiration);

        using (var connection = new SqlConnection(ConnectionString))
        using (var upsertCommand = new SqlCommand(SqlQueries.SetCacheItem, connection))
        {
            upsertCommand.Parameters
                .AddCacheItemId(key)
                .AddCacheItemValue(value)
                .AddSlidingExpirationInSeconds(options.SlidingExpiration)
                .AddAbsoluteExpiration(absoluteExpiration)
                .AddWithValue(UtcNowParameterName, SqlDbType.DateTimeOffset, utcNow);

            connection.Open();

            try
            {
                upsertCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (DatabaseOperations.IsDuplicateKeyException(ex))
                {
                    // There is a possibility that multiple requests can try to add the same item to the cache, in
                    // which case we receive a 'duplicate key' exception on the primary key column.
                }
                else
                {
                    throw;
                }
            }
        }
    }

    public async Task SetCacheItemAsync(string key, ArraySegment<byte> value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
    {
        token.ThrowIfCancellationRequested();

        var utcNow = SystemClock.UtcNow;

        var absoluteExpiration = DatabaseOperations.GetAbsoluteExpiration(utcNow, options);
        DatabaseOperations.ValidateOptions(options.SlidingExpiration, absoluteExpiration);

        using (var connection = new SqlConnection(ConnectionString))
        using (var upsertCommand = new SqlCommand(SqlQueries.SetCacheItem, connection))
        {
            upsertCommand.Parameters
                .AddCacheItemId(key)
                .AddCacheItemValue(value)
                .AddSlidingExpirationInSeconds(options.SlidingExpiration)
                .AddAbsoluteExpiration(absoluteExpiration)
                .AddWithValue(UtcNowParameterName, SqlDbType.DateTimeOffset, utcNow);

            await connection.OpenAsync(token).ConfigureAwait(false);

            try
            {
                await upsertCommand.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                if (DatabaseOperations.IsDuplicateKeyException(ex))
                {
                    // There is a possibility that multiple requests can try to add the same item to the cache, in
                    // which case we receive a 'duplicate key' exception on the primary key column.
                }
                else
                {
                    throw;
                }
            }
        }
    }

    private byte[]? GetCacheItem(string key, bool includeValue, IBufferWriter<byte>? destination = null)
    {
        var utcNow = SystemClock.UtcNow;

        string query;
        if (includeValue)
        {
            query = SqlQueries.GetCacheItem;
        }
        else
        {
            query = SqlQueries.GetCacheItemWithoutValue;
        }

        byte[]? value = null;
        using (var connection = new SqlConnection(ConnectionString))
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters
                .AddCacheItemId(key)
                .AddWithValue(UtcNowParameterName, SqlDbType.DateTimeOffset, utcNow);

            connection.Open();

            if (includeValue)
            {
                using var reader = command.ExecuteReader(
                    CommandBehavior.SequentialAccess | CommandBehavior.SingleRow | CommandBehavior.SingleResult);

                if (reader.Read())
                {
                    if (destination is null)
                    {
                        value = reader.GetFieldValue<byte[]>(Columns.Indexes.CacheItemValueIndex);
                    }
                    else
                    {
                        StreamOut(reader, Columns.Indexes.CacheItemValueIndex, destination);
                        value = []; // use non-null here as a sentinel to say "we got one"
                    }
                }
            }
            else
            {
                command.ExecuteNonQuery();
            }
        }

        return value;
    }

    private async Task<byte[]?> GetCacheItemAsync(string key, bool includeValue, IBufferWriter<byte>? destination = null, CancellationToken token = default(CancellationToken))
    {
        token.ThrowIfCancellationRequested();

        var utcNow = SystemClock.UtcNow;

        string query;
        if (includeValue)
        {
            query = SqlQueries.GetCacheItem;
        }
        else
        {
            query = SqlQueries.GetCacheItemWithoutValue;
        }

        byte[]? value = null;
        using (var connection = new SqlConnection(ConnectionString))
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters
                .AddCacheItemId(key)
                .AddWithValue(UtcNowParameterName, SqlDbType.DateTimeOffset, utcNow);

            await connection.OpenAsync(token).ConfigureAwait(false);

            if (includeValue)
            {
                using var reader = await command.ExecuteReaderAsync(
                    CommandBehavior.SequentialAccess | CommandBehavior.SingleRow | CommandBehavior.SingleResult, token).ConfigureAwait(false);

                if (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    if (destination is null)
                    {
                        value = await reader.GetFieldValueAsync<byte[]>(Columns.Indexes.CacheItemValueIndex, token).ConfigureAwait(false);
                    }
                    else
                    {
                        StreamOut(reader, Columns.Indexes.CacheItemValueIndex, destination);
                        value = []; // use non-null here as a sentinel to say "we got one"
                    }
                }
            }
            else
            {
                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        return value;
    }

    private static long StreamOut(SqlDataReader source, int ordinal, IBufferWriter<byte> destination)
    {
        long dataIndex = 0;
        int read = 0;
        byte[]? lease = null;
        do
        {
            dataIndex += read; // increment offset

            const int DefaultPageSize = 8192;

            var memory = destination.GetMemory(DefaultPageSize); // start from the page size
            if (MemoryMarshal.TryGetArray<byte>(memory, out var segment))
            {
                // avoid an extra copy by writing directly to the target array when possible
                read = (int)source.GetBytes(ordinal, dataIndex, segment.Array, segment.Offset, segment.Count);
                if (read > 0)
                {
                    destination.Advance(read);
                }
            }
            else
            {
                lease ??= ArrayPool<byte>.Shared.Rent(DefaultPageSize);
                read = (int)source.GetBytes(ordinal, dataIndex, lease, 0, lease.Length);

                if (read > 0)
                {
                    if (new ReadOnlySpan<byte>(lease, 0, read).TryCopyTo(memory.Span))
                    {
                        destination.Advance(read);
                    }
                    else
                    {
                        // multi-chunk write (utility method)
                        destination.Write(new(lease, 0, read));
                    }
                }
            }
        }
        while (read > 0);

        if (lease is not null)
        {
            ArrayPool<byte>.Shared.Return(lease);
        }
        return dataIndex;
    }

    private static bool IsDuplicateKeyException(SqlException ex)
    {
        if (ex.Errors != null)
        {
            return ex.Errors.Cast<SqlError>().Any(error => error.Number == DuplicateKeyErrorId);
        }
        return false;
    }

    private static DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset utcNow, DistributedCacheEntryOptions options)
    {
        // calculate absolute expiration
        DateTimeOffset? absoluteExpiration = null;
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            absoluteExpiration = utcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }
        else if (options.AbsoluteExpiration.HasValue)
        {
            if (options.AbsoluteExpiration.Value <= utcNow)
            {
                throw new InvalidOperationException("The absolute expiration value must be in the future.");
            }

            absoluteExpiration = options.AbsoluteExpiration.Value;
        }
        return absoluteExpiration;
    }

    private static void ValidateOptions(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
    {
        if (!slidingExpiration.HasValue && !absoluteExpiration.HasValue)
        {
            throw new InvalidOperationException("Either absolute or sliding expiration needs " +
                "to be provided.");
        }
    }
}
