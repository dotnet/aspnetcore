// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// Various extension methods for dealing with the section body stream
/// </summary>
public static class MultipartSectionStreamExtensions
{
    private const int InitialBufferSize = 4096;
    private const int MaxBufferSize = 64 * 1024;

    /// <summary>
    /// Reads the body of the section as a string
    /// </summary>
    /// <param name="section">The section to read from</param>
    /// <returns>The body steam as string</returns>
    public static Task<string> ReadAsStringAsync(this MultipartSection section)
        => section.ReadAsStringAsync(CancellationToken.None).AsTask();

    /// <summary>
    /// Reads the body of the section as a string
    /// </summary>
    /// <param name="section">The section to read from</param>
    /// <param name="cancellationToken">The cancellationt token.</param>
    /// <returns>The body steam as string</returns>
    public static async ValueTask<string> ReadAsStringAsync(this MultipartSection section, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (section.Body is null)
        {
            throw new ArgumentException("Multipart section must have a body to be read.", nameof(section));
        }

        _ = MediaTypeHeaderValue.TryParse(section.ContentType, out var sectionMediaType);

        var streamEncoding = sectionMediaType?.Encoding;
        // https://learn.microsoft.com/dotnet/core/compatibility/syslib-warnings/syslib0001
        if (streamEncoding == null || streamEncoding.CodePage == 65000)
        {
            streamEncoding = Encoding.UTF8;
        }

        // The pooled buffer below is only handed to MultipartReaderStream, which copies already buffered data into
        // it and never passes it on, so it can't be observed after it is returned. Other section bodies may be
        // arbitrary streams, so they keep using StreamReader.
        if (streamEncoding.CodePage != Encoding.UTF8.CodePage || section.Body is not MultipartReaderStream)
        {
            return await ReadToEndAsync(section.Body, streamEncoding, cancellationToken);
        }

        // Fast path: most section values are small UTF-8 strings. Read them into a pooled buffer and decode
        // them directly instead of allocating a StreamReader and its buffers.
        var buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
        var length = 0;
        var firstReadLength = 0;
        var readPending = false;
        try
        {
            while (true)
            {
                if (length == buffer.Length)
                {
                    if (buffer.Length >= MaxBufferSize)
                    {
                        break;
                    }

                    var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    buffer.AsSpan(0, length).CopyTo(newBuffer);
                    ReturnBuffer(buffer, length);
                    buffer = newBuffer;
                }

                readPending = true;
                var read = await section.Body.ReadAsync(buffer.AsMemory(length), cancellationToken);
                readPending = false;
                if (read == 0)
                {
                    return length == 0 ? string.Empty : streamEncoding.GetString(buffer, 0, length);
                }

                length += read;

                if (length == read)
                {
                    firstReadLength = read;

                    // StreamReader detects byte order marks based on the first read. When the first read could
                    // start with a byte order mark, let StreamReader process the data exactly as it is read.
                    if (MayStartWithByteOrderMark(buffer[0]))
                    {
                        break;
                    }
                }
                else if (firstReadLength < 3)
                {
                    // StreamReader can't rule out a byte order mark from a first read shorter than 3 bytes and
                    // checks the next read instead. A short first read that is followed by the end of the body
                    // (for example "1" or "on") is decoded directly above; otherwise let StreamReader process
                    // the data with the same read boundaries.
                    break;
                }
            }

            // Let StreamReader handle the bytes read so far followed by the rest of the section body.
            return await ReadToEndAsync(new PrefixedReadStream(buffer, firstReadLength, length, section.Body), streamEncoding, cancellationToken);
        }
        finally
        {
            // A read that failed may have written past the bytes counted so far.
            ReturnBuffer(buffer, readPending ? buffer.Length : length);
        }
    }

    private static void ReturnBuffer(byte[] buffer, int length)
    {
        // Section values can be sensitive, so don't leave them in the shared pool.
        buffer.AsSpan(0, length).Clear();
        ArrayPool<byte>.Shared.Return(buffer);
    }

    private static async Task<string> ReadToEndAsync(Stream stream, Encoding encoding, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            encoding,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    // Lead bytes of the byte order marks StreamReader detects (UTF-8, UTF-16 LE/BE and UTF-32 LE/BE).
    private static bool MayStartWithByteOrderMark(byte firstByte) => firstByte is 0xEF or 0xFE or 0xFF or 0x00;
}
