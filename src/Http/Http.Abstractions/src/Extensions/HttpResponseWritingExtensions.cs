// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Convenience methods for writing to the response.
    /// </summary>
    public static class HttpResponseWritingExtensions
    {
        private const int UTF8MaxByteLength = 6;

        /// <summary>
        /// Writes the given text to the response body. UTF-8 encoding will be used.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponse"/>.</param>
        /// <param name="text">The text to write to the response.</param>
        /// <param name="cancellationToken">Notifies when request operations should be cancelled.</param>
        /// <returns>A task that represents the completion of the write operation.</returns>
        public static Task WriteAsync(this HttpResponse response, string text, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return response.WriteAsync(text, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Writes the given text to the response body using the given encoding.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponse"/>.</param>
        /// <param name="text">The text to write to the response.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="cancellationToken">Notifies when request operations should be cancelled.</param>
        /// <returns>A task that represents the completion of the write operation.</returns>
        public static Task WriteAsync(this HttpResponse response, string text, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            // Need to call StartAsync before GetMemory/GetSpan
            if (!response.HasStarted)
            {
                var startAsyncTask = response.StartAsync(cancellationToken);
                if (!startAsyncTask.IsCompletedSuccessfully)
                {
                    return StartAndWriteAsyncAwaited(response, text, encoding, cancellationToken, startAsyncTask);
                }
            }

            Write(response, text, encoding);

            var flushAsyncTask = response.BodyWriter.FlushAsync(cancellationToken);
            if (flushAsyncTask.IsCompletedSuccessfully)
            {
                // Most implementations of ValueTask reset state in GetResult, so call it before returning a completed task.
                flushAsyncTask.GetAwaiter().GetResult();
                return Task.CompletedTask;
            }

            return flushAsyncTask.AsTask();
        }

        private static async Task StartAndWriteAsyncAwaited(this HttpResponse response, string text, Encoding encoding, CancellationToken cancellationToken, Task startAsyncTask)
        {
            await startAsyncTask;
            Write(response, text, encoding);
            await response.BodyWriter.FlushAsync(cancellationToken);
        }

        private static void Write(this HttpResponse response, string text, Encoding encoding)
        {
            var minimumByteSize = GetEncodingMaxByteSize(encoding);
            var pipeWriter = response.BodyWriter;
            var encodedLength = encoding.GetByteCount(text);
            var destination = pipeWriter.GetSpan(minimumByteSize);

            if (encodedLength <= destination.Length)
            {
                // Just call Encoding.GetBytes if everything will fit into a single segment.
                var bytesWritten = encoding.GetBytes(text, destination);
                pipeWriter.Advance(bytesWritten);
            }
            else
            {
                WriteMultiSegmentEncoded(pipeWriter, text, encoding, destination, encodedLength, minimumByteSize);
            }
        }

        private static int GetEncodingMaxByteSize(Encoding encoding)
        {
            if (encoding == Encoding.UTF8)
            {
                return UTF8MaxByteLength;
            }

            return encoding.GetMaxByteCount(1);
        }

        private static void WriteMultiSegmentEncoded(PipeWriter writer, string text, Encoding encoding, Span<byte> destination, int encodedLength, int minimumByteSize)
        {
            var encoder = encoding.GetEncoder();
            var source = text.AsSpan();
            var completed = false;
            var totalBytesUsed = 0;

            // This may be a bug, but encoder.Convert returns completed = true for UTF7 too early.
            // Therefore, we check encodedLength - totalBytesUsed too.
            while (!completed || encodedLength - totalBytesUsed != 0)
            {
                // 'text' is a complete string, the converter should always flush its buffer.
                encoder.Convert(source, destination, flush: true, out var charsUsed, out var bytesUsed, out completed);
                totalBytesUsed += bytesUsed;

                writer.Advance(bytesUsed);
                source = source.Slice(charsUsed);

                destination = writer.GetSpan(minimumByteSize);
            }
        }
    }
}
