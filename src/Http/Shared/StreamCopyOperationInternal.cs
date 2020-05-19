// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    // FYI: In most cases the source will be a FileStream and the destination will be to the network.
    internal static class StreamCopyOperationInternal
    {
        private const int DefaultBufferSize = 4 * 1024;

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
                Debug.Assert(!bytesRemaining.HasValue || bytesRemaining.Value >= 0);
                Debug.Assert(buffer != null);

                while (true)
                {
                    // The natural end of the range.
                    if (bytesRemaining.HasValue && bytesRemaining.Value <= 0)
                    {
                        return;
                    }

                    cancel.ThrowIfCancellationRequested();

                    int readLength = buffer.Length;
                    if (bytesRemaining.HasValue)
                    {
                        readLength = (int)Math.Min(bytesRemaining.Value, (long)readLength);
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

        /// <summary>Asynchronously reads the given number of bytes from the source stream and writes them using pipe writer.</summary>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <param name="source">The stream from which the contents will be copied.</param>
        /// <param name="writer">The PipeWriter to which the contents of the current stream will be copied.</param>
        /// <param name="count">The count of bytes to be copied.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        public static Task CopyToAsync(Stream source, PipeWriter writer, long? count, CancellationToken cancel)
        {
            if (count == null)
            {
                // No length, do a copy with the default buffer size (based on whatever the pipe settings are, default is 4K)
                return source.CopyToAsync(writer, cancel);
            }

            static async Task CopyToAsync(Stream source, PipeWriter writer, long bytesRemaining, CancellationToken cancel)
            {
                // The array pool likes powers of 2
                const int maxBufferSize = 64 * 1024;
                const int minBufferSize = 1024;

                // We know exactly how much we're going to copy
                while (bytesRemaining > 0)
                {
                    var bufferSize = (int)Math.Clamp(bytesRemaining, minBufferSize, maxBufferSize);

                    // The natural end of the range.
                    var memory = writer.GetMemory(bufferSize);

                    if (memory.Length > bytesRemaining)
                    {
                        memory = memory.Slice(0, (int)bytesRemaining);
                    }

                    var read = await source.ReadAsync(memory, cancel);

                    bytesRemaining -= read;

                    // End of the source stream.
                    if (read == 0)
                    {
                        break;
                    }

                    writer.Advance(read);

                    var result = await writer.FlushAsync(cancel);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }

            Debug.Assert(source != null);
            Debug.Assert(writer != null);
            Debug.Assert(count >= 0);

            return CopyToAsync(source, writer, count.Value, cancel);
        }
    }
}
