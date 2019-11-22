// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    // FYI: In most cases the source will be a FileStream and the destination will be to the network.
    internal static class StreamCopyOperationInternal
    {
        private const int DefaultBufferSize = 4096;

        /// <summary>Asynchronously reads the given number of bytes from the source stream and writes them to another stream.</summary>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <param name="source">The stream from which the contents will be copied.</param>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="count">The count of bytes to be copied.</param>
        /// <param name="cancel">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
        public static Task CopyToAsync(Stream source, Stream destination, long? count, CancellationToken cancel)
        {
            return CopyToAsync(source, destination, count, DefaultBufferSize, cancel);
        }

        /// <summary>Asynchronously reads the given number of bytes from the source stream and writes them to another stream, using a specified buffer size.</summary>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <param name="source">The stream from which the contents will be copied.</param>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="count">The count of bytes to be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 4096.</param>
        /// <param name="cancel">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
        public static async Task CopyToAsync(Stream source, Stream destination, long? count, int bufferSize, CancellationToken cancel)
        {
            long? bytesRemaining = count;

            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                Debug.Assert(source != null);
                Debug.Assert(destination != null);
                Debug.Assert(!bytesRemaining.HasValue || bytesRemaining.GetValueOrDefault() >= 0);
                Debug.Assert(buffer != null);

                while (true)
                {
                    // The natural end of the range.
                    if (bytesRemaining.HasValue && bytesRemaining.GetValueOrDefault() <= 0)
                    {
                        return;
                    }

                    cancel.ThrowIfCancellationRequested();

                    int readLength = buffer.Length;
                    if (bytesRemaining.HasValue)
                    {
                        readLength = (int)Math.Min(bytesRemaining.GetValueOrDefault(), (long)readLength);
                    }
                    int read = await source.ReadAsync(buffer, 0, readLength, cancel);

                    if (bytesRemaining.HasValue)
                    {
                        bytesRemaining -= read;
                    }

                    // End of the source stream.
                    if (read == 0)
                    {
                        return;
                    }

                    cancel.ThrowIfCancellationRequested();

                    await destination.WriteAsync(buffer, 0, read, cancel);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
