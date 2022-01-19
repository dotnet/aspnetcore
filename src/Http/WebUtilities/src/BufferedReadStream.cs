// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// A Stream that wraps another stream and allows reading lines.
/// The data is buffered in memory.
/// </summary>
public class BufferedReadStream : Stream
{
    private const byte CR = (byte)'\r';
    private const byte LF = (byte)'\n';

    private readonly Stream _inner;
    private readonly byte[] _buffer;
    private readonly ArrayPool<byte> _bytePool;
    private int _bufferOffset;
    private int _bufferCount;
    private bool _disposed;

    /// <summary>
    /// Creates a new stream.
    /// </summary>
    /// <param name="inner">The stream to wrap.</param>
    /// <param name="bufferSize">Size of buffer in bytes.</param>
    public BufferedReadStream(Stream inner, int bufferSize)
        : this(inner, bufferSize, ArrayPool<byte>.Shared)
    {
    }

    /// <summary>
    /// Creates a new stream.
    /// </summary>
    /// <param name="inner">The stream to wrap.</param>
    /// <param name="bufferSize">Size of buffer in bytes.</param>
    /// <param name="bytePool">ArrayPool for the buffer.</param>
    public BufferedReadStream(Stream inner, int bufferSize, ArrayPool<byte> bytePool)
    {
        if (inner == null)
        {
            throw new ArgumentNullException(nameof(inner));
        }

        _inner = inner;
        _bytePool = bytePool;
        _buffer = bytePool.Rent(bufferSize);
    }

    /// <summary>
    /// The currently buffered data.
    /// </summary>
    public ArraySegment<byte> BufferedData
    {
        get { return new ArraySegment<byte>(_buffer, _bufferOffset, _bufferCount); }
    }

    /// <inheritdoc/>
    public override bool CanRead
    {
        get { return _inner.CanRead || _bufferCount > 0; }
    }

    /// <inheritdoc/>
    public override bool CanSeek
    {
        get { return _inner.CanSeek; }
    }

    /// <inheritdoc/>
    public override bool CanTimeout
    {
        get { return _inner.CanTimeout; }
    }

    /// <inheritdoc/>
    public override bool CanWrite
    {
        get { return _inner.CanWrite; }
    }

    /// <inheritdoc/>
    public override long Length
    {
        get { return _inner.Length; }
    }

    /// <inheritdoc/>
    public override long Position
    {
        get { return _inner.Position - _bufferCount; }
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Position must be positive.");
            }
            if (value == Position)
            {
                return;
            }

            // Backwards?
            if (value <= _inner.Position)
            {
                // Forward within the buffer?
                var innerOffset = (int)(_inner.Position - value);
                if (innerOffset <= _bufferCount)
                {
                    // Yes, just skip some of the buffered data
                    _bufferOffset += innerOffset;
                    _bufferCount -= innerOffset;
                }
                else
                {
                    // No, reset the buffer
                    _bufferOffset = 0;
                    _bufferCount = 0;
                    _inner.Position = value;
                }
            }
            else
            {
                // Forward, reset the buffer
                _bufferOffset = 0;
                _bufferCount = 0;
                _inner.Position = value;
            }
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        _inner.SetLength(value);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
            _bytePool.Return(_buffer);

            if (disposing)
            {
                _inner.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        _inner.Flush();
    }

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _inner.FlushAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        _inner.Write(buffer, offset, count);
    }

    /// <inheritdoc/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        return _inner.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBuffer(buffer, offset, count);

        // Drain buffer
        if (_bufferCount > 0)
        {
            int toCopy = Math.Min(_bufferCount, count);
            Buffer.BlockCopy(_buffer, _bufferOffset, buffer, offset, toCopy);
            _bufferOffset += toCopy;
            _bufferCount -= toCopy;
            return toCopy;
        }

        return _inner.Read(buffer, offset, count);
    }

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBuffer(buffer, offset, count);
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        // Drain buffer
        if (_bufferCount > 0)
        {
            int toCopy = Math.Min(_bufferCount, buffer.Length);
            _buffer.AsMemory(_bufferOffset, toCopy).CopyTo(buffer);
            _bufferOffset += toCopy;
            _bufferCount -= toCopy;
            return toCopy;
        }

        return await _inner.ReadAsync(buffer, cancellationToken);
    }

    /// <summary>
    /// Ensures that the buffer is not empty.
    /// </summary>
    /// <returns>Returns <c>true</c> if the buffer is not empty; <c>false</c> otherwise.</returns>
    public bool EnsureBuffered()
    {
        if (_bufferCount > 0)
        {
            return true;
        }
        // Downshift to make room
        _bufferOffset = 0;
        _bufferCount = _inner.Read(_buffer, 0, _buffer.Length);
        return _bufferCount > 0;
    }

    /// <summary>
    /// Ensures that the buffer is not empty.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Returns <c>true</c> if the buffer is not empty; <c>false</c> otherwise.</returns>
    public async Task<bool> EnsureBufferedAsync(CancellationToken cancellationToken)
    {
        if (_bufferCount > 0)
        {
            return true;
        }
        // Downshift to make room
        _bufferOffset = 0;
        _bufferCount = await _inner.ReadAsync(_buffer.AsMemory(), cancellationToken);
        return _bufferCount > 0;
    }

    /// <summary>
    /// Ensures that a minimum amount of buffered data is available.
    /// </summary>
    /// <param name="minCount">Minimum amount of buffered data.</param>
    /// <returns>Returns <c>true</c> if the minimum amount of buffered data is available; <c>false</c> otherwise.</returns>
    public bool EnsureBuffered(int minCount)
    {
        if (minCount > _buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(minCount), minCount, "The value must be smaller than the buffer size: " + _buffer.Length);
        }
        while (_bufferCount < minCount)
        {
            // Downshift to make room
            if (_bufferOffset > 0)
            {
                if (_bufferCount > 0)
                {
                    Buffer.BlockCopy(_buffer, _bufferOffset, _buffer, 0, _bufferCount);
                }
                _bufferOffset = 0;
            }
            int read = _inner.Read(_buffer, _bufferOffset + _bufferCount, _buffer.Length - _bufferCount - _bufferOffset);
            _bufferCount += read;
            if (read == 0)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Ensures that a minimum amount of buffered data is available.
    /// </summary>
    /// <param name="minCount">Minimum amount of buffered data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Returns <c>true</c> if the minimum amount of buffered data is available; <c>false</c> otherwise.</returns>
    public async Task<bool> EnsureBufferedAsync(int minCount, CancellationToken cancellationToken)
    {
        if (minCount > _buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(minCount), minCount, "The value must be smaller than the buffer size: " + _buffer.Length);
        }
        while (_bufferCount < minCount)
        {
            // Downshift to make room
            if (_bufferOffset > 0)
            {
                if (_bufferCount > 0)
                {
                    Buffer.BlockCopy(_buffer, _bufferOffset, _buffer, 0, _bufferCount);
                }
                _bufferOffset = 0;
            }
            int read = await _inner.ReadAsync(_buffer.AsMemory(_bufferOffset + _bufferCount, _buffer.Length - _bufferCount - _bufferOffset), cancellationToken);
            _bufferCount += read;
            if (read == 0)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Reads a line. A line is defined as a sequence of characters followed by
    /// a carriage return immediately followed by a line feed. The resulting string does not
    /// contain the terminating carriage return and line feed.
    /// </summary>
    /// <param name="lengthLimit">Maximum allowed line length.</param>
    /// <returns>A line.</returns>
    public string ReadLine(int lengthLimit)
    {
        CheckDisposed();
        using (var builder = new MemoryStream(200))
        {
            bool foundCR = false, foundCRLF = false;

            while (!foundCRLF && EnsureBuffered())
            {
                if (builder.Length > lengthLimit)
                {
                    throw new InvalidDataException($"Line length limit {lengthLimit} exceeded.");
                }
                ProcessLineChar(builder, ref foundCR, ref foundCRLF);
            }

            return DecodeLine(builder, foundCRLF);
        }
    }

    /// <summary>
    /// Reads a line. A line is defined as a sequence of characters followed by
    /// a carriage return immediately followed by a line feed. The resulting string does not
    /// contain the terminating carriage return and line feed.
    /// </summary>
    /// <param name="lengthLimit">Maximum allowed line length.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A line.</returns>
    public async Task<string> ReadLineAsync(int lengthLimit, CancellationToken cancellationToken)
    {
        CheckDisposed();
        using (var builder = new MemoryStream(200))
        {
            bool foundCR = false, foundCRLF = false;

            while (!foundCRLF && await EnsureBufferedAsync(cancellationToken))
            {
                if (builder.Length > lengthLimit)
                {
                    throw new InvalidDataException($"Line length limit {lengthLimit} exceeded.");
                }

                ProcessLineChar(builder, ref foundCR, ref foundCRLF);
            }

            return DecodeLine(builder, foundCRLF);
        }
    }

    private void ProcessLineChar(MemoryStream builder, ref bool foundCR, ref bool foundCRLF)
    {
        var b = _buffer[_bufferOffset];
        builder.WriteByte(b);
        _bufferOffset++;
        _bufferCount--;
        if (b == LF && foundCR)
        {
            foundCRLF = true;
            return;
        }
        foundCR = b == CR;
    }

    private static string DecodeLine(MemoryStream builder, bool foundCRLF)
    {
        // Drop the final CRLF, if any
        var length = foundCRLF ? builder.Length - 2 : builder.Length;
        return Encoding.UTF8.GetString(builder.ToArray(), 0, (int)length);
    }

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BufferedReadStream));
        }
    }

    private static void ValidateBuffer(byte[] buffer, int offset, int count)
    {
        // Delegate most of our validation.
        _ = new ArraySegment<byte>(buffer, offset, count);
        if (count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "The value must be greater than zero.");
        }
    }
}
