// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace Microsoft.AspNetCore.WebUtilities;

internal sealed class MultipartReaderStream : Stream
{
    private readonly MultipartBoundary _boundary;
    private readonly BufferedReadStream _innerStream;
    private readonly ArrayPool<byte> _bytePool;

    private readonly long _innerOffset;
    private long _position;
    private long _observedLength;
    private bool _finished;

    /// <summary>
    /// Creates a stream that reads until it reaches the given boundary pattern.
    /// </summary>
    /// <param name="stream">The <see cref="BufferedReadStream"/>.</param>
    /// <param name="boundary">The boundary pattern to use.</param>
    public MultipartReaderStream(BufferedReadStream stream, MultipartBoundary boundary)
        : this(stream, boundary, ArrayPool<byte>.Shared)
    {
    }

    /// <summary>
    /// Creates a stream that reads until it reaches the given boundary pattern.
    /// </summary>
    /// <param name="stream">The <see cref="BufferedReadStream"/>.</param>
    /// <param name="boundary">The boundary pattern to use.</param>
    /// <param name="bytePool">The ArrayPool pool to use for temporary byte arrays.</param>
    public MultipartReaderStream(BufferedReadStream stream, MultipartBoundary boundary, ArrayPool<byte> bytePool)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(boundary);

        _bytePool = bytePool;
        _innerStream = stream;
        _innerOffset = _innerStream.CanSeek ? _innerStream.Position : 0;
        _boundary = boundary;
    }

    public bool FinalBoundaryFound { get; private set; }

    public long? LengthLimit { get; set; }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanSeek
    {
        get { return _innerStream.CanSeek; }
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override long Length
    {
        get { return _observedLength; }
    }

    public override long Position
    {
        get { return _position; }
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The Position must be positive.");
            }
            if (value > _observedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The Position must be less than length.");
            }
            _position = value;
            if (_position < _observedLength)
            {
                _finished = false;
            }
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin == SeekOrigin.Begin)
        {
            Position = offset;
        }
        else if (origin == SeekOrigin.Current)
        {
            Position = Position + offset;
        }
        else // if (origin == SeekOrigin.End)
        {
            Position = Length + offset;
        }
        return Position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    private void PositionInnerStream()
    {
        if (_innerStream.CanSeek && _innerStream.Position != (_innerOffset + _position))
        {
            _innerStream.Position = _innerOffset + _position;
        }
    }

    private int UpdatePosition(int read)
    {
        _position += read;
        if (_observedLength < _position)
        {
            _observedLength = _position;
            if (LengthLimit.HasValue &&
                LengthLimit.GetValueOrDefault() is var lengthLimit &&
                _observedLength > lengthLimit)
            {
                // If we hit the limit before the first boundary then we're using the header length limit, not the body length limit.
                if (_boundary.BeforeFirstBoundary())
                {
                    throw new InvalidDataException($"Multipart header length limit {lengthLimit} exceeded. Too much data before the first boundary.");
                }
                else
                {
                    throw new InvalidDataException($"Multipart body length limit {lengthLimit} exceeded.");
                }
            }
        }
        return read;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_finished)
        {
            return 0;
        }

        PositionInnerStream();
        if (!_innerStream.EnsureBuffered(_boundary.FinalBoundaryLength))
        {
            throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
        }
        var bufferedData = _innerStream.BufferedData;

        var index = bufferedData.AsSpan().IndexOf(_boundary.BoundaryBytes);
        if (index >= 0)
        {
            // There is data before the boundary, we should return it to the user
            if (index != 0)
            {
                // Sync, it's already buffered
                var slice = buffer.AsSpan(offset, Math.Min(count, index));

                var readAmount = _innerStream.Read(slice);
                return UpdatePosition(readAmount);
            }
            else
            {
                var length = _boundary.BoundaryBytes.Length;

                return ReadBoundary(this, length);
            }
        }

        // scan for a partial boundary match.
        int read;
        if (SubMatch(bufferedData, _boundary.BoundaryBytes, out var matchOffset, out var matchCount))
        {
            // We found a possible match, return any data before it.
            if (matchOffset > bufferedData.Offset)
            {
                read = _innerStream.Read(buffer, offset, Math.Min(count, matchOffset - bufferedData.Offset));
                return UpdatePosition(read);
            }

            var length = _boundary.BoundaryBytes.Length;
            Debug.Assert(matchCount == length);

            return ReadBoundary(this, length);
        }

        // No possible boundary match within the buffered data, return the data from the buffer.
        read = _innerStream.Read(buffer, offset, Math.Min(count, bufferedData.Count));
        return UpdatePosition(read);

        static int ReadBoundary(MultipartReaderStream stream, int length)
        {
            // "The boundary may be followed by zero or more characters of
            // linear whitespace. It is then terminated by either another CRLF"
            // or -- for the final boundary.
            var boundary = stream._bytePool.Rent(length);
            var read = stream._innerStream.Read(boundary, 0, length);
            stream._bytePool.Return(boundary);
            Debug.Assert(read == length); // It should have all been buffered

            var remainder = stream._innerStream.ReadLine(lengthLimit: 100).AsSpan(); // Whitespace may exceed the buffer.
            remainder = remainder.Trim();
            if (remainder.Equals("--", StringComparison.Ordinal))
            {
                stream.FinalBoundaryFound = true;
            }
            Debug.Assert(stream.FinalBoundaryFound || remainder.IsEmpty, "Un-expected data found on the boundary line: " + remainder.ToString());
            stream._finished = true;
            return 0;
        }
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (_finished)
        {
            return 0;
        }

        PositionInnerStream();
        if (!await _innerStream.EnsureBufferedAsync(_boundary.FinalBoundaryLength, cancellationToken))
        {
            throw new IOException("Unexpected end of Stream, the content may have already been read by another component. ");
        }
        var bufferedData = _innerStream.BufferedData;

        var index = bufferedData.AsSpan().IndexOf(_boundary.BoundaryBytes);

        if (index >= 0)
        {
            // There is data before the boundary, we should return it to the user
            if (index != 0)
            {
                var slice = buffer[..Math.Min(buffer.Length, index)];

                // Sync, it's already buffered
                var readAmount = _innerStream.Read(slice.Span);
                return UpdatePosition(readAmount);
            }
            else
            {
                var length = _boundary.BoundaryBytes.Length;

                return await ReadBoundaryAsync(this, length, cancellationToken);
            }
        }

        // scan for a boundary match, full or partial.
        int matchOffset;
        int matchCount;
        int read;
        if (SubMatch(bufferedData, _boundary.BoundaryBytes, out matchOffset, out matchCount))
        {
            // We found a possible match, return any data before it.
            if (matchOffset > bufferedData.Offset)
            {
                var slice = buffer[..Math.Min(buffer.Length, matchOffset - bufferedData.Offset)];

                // Sync, it's already buffered
                read = _innerStream.Read(slice.Span);
                return UpdatePosition(read);
            }

            var length = _boundary.BoundaryBytes.Length;
            Debug.Assert(matchCount == length);

            return await ReadBoundaryAsync(this, length, cancellationToken);
        }

        // No possible boundary match within the buffered data, return the data from the buffer.
        read = _innerStream.Read(buffer.Span[..Math.Min(buffer.Length, bufferedData.Count)]);
        return UpdatePosition(read);

        static async Task<int> ReadBoundaryAsync(MultipartReaderStream stream, int length, CancellationToken cancellationToken)
        {
            // "The boundary may be followed by zero or more characters of
            // linear whitespace. It is then terminated by either another CRLF"
            // or -- for the final boundary.
            var boundary = stream._bytePool.Rent(length);
            var read = stream._innerStream.Read(boundary, 0, length);
            stream._bytePool.Return(boundary);
            Debug.Assert(read == length); // It should have all been buffered

            var remainder = await stream._innerStream.ReadLineAsync(lengthLimit: 100, cancellationToken: cancellationToken); // Whitespace may exceed the buffer.
            remainder = remainder.Trim();
            if (string.Equals("--", remainder, StringComparison.Ordinal))
            {
                stream.FinalBoundaryFound = true;
            }
            Debug.Assert(stream.FinalBoundaryFound || string.Equals(string.Empty, remainder, StringComparison.Ordinal), "Un-expected data found on the boundary line: " + remainder);

            stream._finished = true;
            return 0;
        }
    }

    // Does segment1 end with the start of matchBytes?
    // 1: AAAAABBB
    // 2:      BBBBB
    private static bool SubMatch(ArraySegment<byte> segment1, ReadOnlySpan<byte> matchBytes, out int matchOffset, out int matchCount)
    {
        matchOffset = Math.Max(segment1.Offset, segment1.Offset + segment1.Count - matchBytes.Length);
        var segmentEnd = segment1.Offset + segment1.Count;

        // clear matchCount to zero
        matchCount = 0;
        for (; matchOffset < segmentEnd; matchOffset++)
        {
            var countLimit = segmentEnd - matchOffset;
            for (matchCount = 0; matchCount < matchBytes.Length && matchCount < countLimit; matchCount++)
            {
                if (matchBytes[matchCount] != segment1.Array![matchOffset + matchCount])
                {
                    matchCount = 0;
                    break;
                }
            }
            if (matchCount > 0)
            {
                break;
            }
        }

        return matchCount > 0;
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        bufferSize = Math.Max(4096, bufferSize);
        base.CopyTo(destination, bufferSize);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        // Set a minimum buffer size of 4K since the base Stream implementation has weird behavior when the stream is
        // seekable *and* the length is 0 (it passes in a buffer size of 1).
        // See https://github.com/dotnet/runtime/blob/222415c56c9ea73530444768c0e68413eb374f5d/src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs#L164-L184
        bufferSize = Math.Max(4096, bufferSize);
        return base.CopyToAsync(destination, bufferSize, cancellationToken);
    }
}
