// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
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
        try
        {
            var length = 0;
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
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = newBuffer;
                }

                var read = await section.Body.ReadAsync(buffer.AsMemory(length), cancellationToken);
                if (read == 0)
                {
                    return length == 0 ? string.Empty : streamEncoding.GetString(buffer, 0, length);
                }

                length += read;

                // StreamReader detects byte order marks based on the first read. When the first read could start
                // with a byte order mark, or is too short for StreamReader to rule one out, let StreamReader
                // process the data exactly as it is read. Otherwise no detection happens and decoding the bytes
                // directly is equivalent.
                if (length == read && (read < 3 || MayStartWithByteOrderMark(buffer[0])))
                {
                    break;
                }
            }

            // Let StreamReader handle the bytes read so far followed by the rest of the section body.
            return await ReadToEndAsync(new PrefixedReadStream(buffer, length, section.Body), streamEncoding, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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

    private sealed class PrefixedReadStream : Stream
    {
        private readonly byte[] _prefix;
        private readonly int _prefixLength;
        private readonly Stream _inner;
        private int _prefixPosition;

        public PrefixedReadStream(byte[] prefix, int prefixLength, Stream inner)
        {
            _prefix = prefix;
            _prefixLength = prefixLength;
            _inner = inner;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (TryReadPrefix(buffer, out var read))
            {
                return read;
            }

            return _inner.Read(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (TryReadPrefix(buffer.Span, out var read))
            {
                return new ValueTask<int>(read);
            }

            return _inner.ReadAsync(buffer, cancellationToken);
        }

        private bool TryReadPrefix(Span<byte> buffer, out int read)
        {
            var remaining = _prefixLength - _prefixPosition;
            if (remaining == 0 || buffer.IsEmpty)
            {
                read = 0;
                return false;
            }

            read = Math.Min(remaining, buffer.Length);
            _prefix.AsSpan(_prefixPosition, read).CopyTo(buffer);
            _prefixPosition += read;
            return true;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
