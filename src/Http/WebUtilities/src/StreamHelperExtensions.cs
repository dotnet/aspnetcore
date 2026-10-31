// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// HTTP extension methods for <see cref="Stream"/>.
/// </summary>
public static class StreamHelperExtensions
{
    private const int _maxReadBufferSize = 1024 * 4;

    /// <summary>
    /// Reads the specified <paramref name="stream"/> to the end.
    /// <para>
    /// This API is effective when used in conjunction with buffering. It allows
    /// a buffered request stream to be synchronously read after it has been completely drained.
    /// </para>
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to completely read.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public static Task DrainAsync(this Stream stream, CancellationToken cancellationToken)
    {
        return stream.DrainAsync(ArrayPool<byte>.Shared, null, cancellationToken);
    }

    /// <summary>
    /// Reads the specified <paramref name="stream"/> to the end.
    /// <para>
    /// This API is effective when used in conjunction with buffering. It allows
    /// a buffered request stream to be synchronously read after it has been completely drained.
    /// </para>
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to completely read.</param>
    /// <param name="limit">The maximum number of bytes to read. Throws if the <see cref="Stream"/> is larger than this limit.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public static Task DrainAsync(this Stream stream, long? limit, CancellationToken cancellationToken)
    {
        return stream.DrainAsync(ArrayPool<byte>.Shared, limit, cancellationToken);
    }

    /// <summary>
    /// Reads the specified <paramref name="stream"/> to the end.
    /// <para>
    /// This API is effective when used in conjunction with buffering. It allows
    /// a buffered request stream to be synchronously read after it has been completely drained.
    /// </para>
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to completely read.</param>
    /// <param name="bytePool">The byte array pool to use.</param>
    /// <param name="limit">The maximum number of bytes to read. Throws if the <see cref="Stream"/> is larger than this limit.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public static async Task DrainAsync(this Stream stream, ArrayPool<byte> bytePool, long? limit, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var buffer = bytePool.Rent(_maxReadBufferSize);
        long total = 0;
        try
        {
            var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
            while (read > 0)
            {
                // Not all streams support cancellation directly.
                cancellationToken.ThrowIfCancellationRequested();
                if (limit.HasValue && limit.GetValueOrDefault() - total < read)
                {
                    throw new InvalidDataException($"The stream exceeded the data limit {limit.GetValueOrDefault()}.");
                }
                total += read;
                read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
            }
        }
        finally
        {
            bytePool.Return(buffer);
        }
    }
}
