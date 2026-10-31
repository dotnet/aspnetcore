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

        // Sections produced by MultipartReader are typically small form values. Read them into a pooled buffer and
        // decode directly rather than allocating a StreamReader and its buffers. The pooled buffer is only handed
        // to our own MultipartReaderStream, which copies into it and does not retain it.
        if (section.Body is MultipartReaderStream)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(StreamReaderBufferSize);
            try
            {
                var length = 0;
                while (length < buffer.Length)
                {
                    var read = await section.Body.ReadAsync(buffer.AsMemory(length), cancellationToken);
                    if (read == 0)
                    {
                        return Decode(buffer, length, streamEncoding);
                    }

                    length += read;
                }

                // The value doesn't fit in the buffer, so read the rest with a StreamReader as before,
                // starting with the bytes that were already consumed.
                using var prefixedReader = CreateStreamReader(new PrefixedReadStream(buffer, length, section.Body), streamEncoding);
                return await prefixedReader.ReadToEndAsync(cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        using var reader = CreateStreamReader(section.Body, streamEncoding);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private const int StreamReaderBufferSize = 1024;

    private static StreamReader CreateStreamReader(Stream stream, Encoding encoding)
        => new StreamReader(
            stream,
            encoding,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: StreamReaderBufferSize,
            leaveOpen: true);

    private static string Decode(byte[] buffer, int length, Encoding encoding)
    {
        // Without a possible byte order mark, StreamReader decodes UTF-8 exactly like Encoding.UTF8.GetString,
        // including replacing invalid or truncated sequences.
        if (ReferenceEquals(encoding, Encoding.UTF8) && (length == 0 || !IsPossibleByteOrderMarkStart(buffer[0])))
        {
            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        using var reader = CreateStreamReader(new MemoryStream(buffer, 0, length, writable: false), encoding);
        return reader.ReadToEnd();
    }

    // First bytes of the UTF-8, UTF-16 and UTF-32 byte order marks that StreamReader detects.
    private static bool IsPossibleByteOrderMarkStart(byte b) => b is 0xEF or 0xFE or 0xFF or 0x00;

    // Replays already-consumed bytes before continuing with the remaining data of the inner stream.
    private sealed class PrefixedReadStream : Stream
    {
        private readonly byte[] _prefix;
        private readonly int _prefixLength;
        private readonly Stream _inner;
        private int _prefixOffset;

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
            var remaining = _prefixLength - _prefixOffset;
            if (remaining == 0 || buffer.IsEmpty)
            {
                read = 0;
                return remaining != 0;
            }

            read = Math.Min(remaining, buffer.Length);
            _prefix.AsSpan(_prefixOffset, read).CopyTo(buffer);
            _prefixOffset += read;
            return true;
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
