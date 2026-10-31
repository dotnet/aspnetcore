// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http;

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
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task WriteAsync(this HttpResponse response, string text, CancellationToken cancellationToken = default(CancellationToken))
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(text);

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
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task WriteAsync(this HttpResponse response, string text, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(encoding);

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

        return response.BodyWriter.FlushAsync(cancellationToken).GetAsTask();
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
