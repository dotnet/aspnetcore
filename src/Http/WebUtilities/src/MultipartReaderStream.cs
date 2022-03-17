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
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (boundary == null)
        {
            throw new ArgumentNullException(nameof(boundary));
        }

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
            if (LengthLimit.HasValue && _observedLength > LengthLimit.GetValueOrDefault())
            {
                throw new InvalidDataException($"Multipart body length limit {LengthLimit.GetValueOrDefault()} exceeded.");
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

        // scan for a boundary match, full or partial.
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

            // "The boundary may be followed by zero or more characters of
            // linear whitespace. It is then terminated by either another CRLF"
            // or -- for the final boundary.
            var boundary = _bytePool.Rent(length);
            read = _innerStream.Read(boundary, 0, length);
            _bytePool.Return(boundary);
            Debug.Assert(read == length); // It should have all been buffered

            var remainder = _innerStream.ReadLine(lengthLimit: 100); // Whitespace may exceed the buffer.
            remainder = remainder.Trim();
            if (string.Equals("--", remainder, StringComparison.Ordinal))
            {
                FinalBoundaryFound = true;
            }
            Debug.Assert(FinalBoundaryFound || string.Equals(string.Empty, remainder, StringComparison.Ordinal), "Un-expected data found on the boundary line: " + remainder);
            _finished = true;
            return 0;
        }

        // No possible boundary match within the buffered data, return the data from the buffer.
        read = _innerStream.Read(buffer, offset, Math.Min(count, bufferedData.Count));
        return UpdatePosition(read);
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

        // scan for a boundary match, full or partial.
        int matchOffset;
        int matchCount;
        int read;
        if (SubMatch(bufferedData, _boundary.BoundaryBytes, out matchOffset, out matchCount))
        {
            // We found a possible match, return any data before it.
            if (matchOffset > bufferedData.Offset)
            {
                // Sync, it's already buffered
                var slice = buffer[..Math.Min(buffer.Length, matchOffset - bufferedData.Offset)];

                read = _innerStream.Read(slice.Span);
                return UpdatePosition(read);
            }

            var length = _boundary.BoundaryBytes!.Length;
            Debug.Assert(matchCount == length);

            // "The boundary may be followed by zero or more characters of
            // linear whitespace. It is then terminated by either another CRLF"
            // or -- for the final boundary.
            var boundary = _bytePool.Rent(length);
            read = _innerStream.Read(boundary, 0, length);
            _bytePool.Return(boundary);
            Debug.Assert(read == length); // It should have all been buffered

            var remainder = await _innerStream.ReadLineAsync(lengthLimit: 100, cancellationToken: cancellationToken); // Whitespace may exceed the buffer.
            remainder = remainder.Trim();
            if (string.Equals("--", remainder, StringComparison.Ordinal))
            {
                FinalBoundaryFound = true;
            }
            Debug.Assert(FinalBoundaryFound || string.Equals(string.Empty, remainder, StringComparison.Ordinal), "Un-expected data found on the boundary line: " + remainder);

            _finished = true;
            return 0;
        }

        // No possible boundary match within the buffered data, return the data from the buffer.
        read = _innerStream.Read(buffer.Span[..Math.Min(buffer.Length, bufferedData.Count)]);
        return UpdatePosition(read);
    }

    // Does segment1 contain all of matchBytes, or does it end with the start of matchBytes?
    // 1: AAAAABBBBBCCCCC
    // 2:      BBBBB
    // Or:
    // 1: AAAAABBB
    // 2:      BBBBB
    private bool SubMatch(ArraySegment<byte> segment1, byte[] matchBytes, out int matchOffset, out int matchCount)
    {
        // case 1: does segment1 fully contain matchBytes?
        {
            var matchBytesLengthMinusOne = matchBytes.Length - 1;
            var matchBytesLastByte = matchBytes[matchBytesLengthMinusOne];
            var segmentEndMinusMatchBytesLength = segment1.Offset + segment1.Count - matchBytes.Length;

            matchOffset = segment1.Offset;
            while (matchOffset < segmentEndMinusMatchBytesLength)
            {
                var lookaheadTailChar = segment1.Array![matchOffset + matchBytesLengthMinusOne];
                if (lookaheadTailChar == matchBytesLastByte &&
                    CompareBuffers(segment1.Array, matchOffset, matchBytes, 0, matchBytesLengthMinusOne) == 0)
                {
                    matchCount = matchBytes.Length;
                    return true;
                }
                matchOffset += _boundary.GetSkipValue(lookaheadTailChar);
            }
        }

        // case 2: does segment1 end with the start of matchBytes?
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

    private static int CompareBuffers(byte[] buffer1, int offset1, byte[] buffer2, int offset2, int count)
    {
        for (; count-- > 0; offset1++, offset2++)
        {
            if (buffer1[offset1] != buffer2[offset2])
            {
                return buffer1[offset1] - buffer2[offset2];
            }
        }
        return 0;
    }
}
