// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// A Stream that wraps another stream starting at a certain offset and reading for the given length.
/// </summary>
internal sealed class ReferenceReadStream : Stream
{
    private readonly Stream _inner;
    private readonly long _innerOffset;
    private readonly long _length;
    private long _position;

    private bool _disposed;

    public ReferenceReadStream(Stream inner, long offset, long length)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
        _innerOffset = offset;
        _length = length;
        _inner.Position = offset;
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanSeek
    {
        get { return _inner.CanSeek; }
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override long Length
    {
        get { return _length; }
    }

    public override long Position
    {
        get { return _position; }
        set
        {
            ThrowIfDisposed();
            if (value < 0 || value > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"The Position must be within the length of the Stream: {Length}");
            }
            VerifyPosition();
            _position = value;
            _inner.Position = _innerOffset + _position;
        }
    }

    // Throws if the position in the underlying stream has changed without our knowledge, indicating someone else is trying
    // to use the stream at the same time which could lead to data corruption.
    private void VerifyPosition()
    {
        if (_inner.Position != _innerOffset + _position)
        {
            throw new InvalidOperationException("The inner stream position has changed unexpectedly.");
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin == SeekOrigin.Begin)
        {
            Position = offset;
        }
        else if (origin == SeekOrigin.End)
        {
            Position = Length + offset;
        }
        else // if (origin == SeekOrigin.Current)
        {
            Position = Position + offset;
        }
        return Position;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        VerifyPosition();
        var toRead = Math.Min(count, _length - _position);
        var read = _inner.Read(buffer, offset, (int)toRead);
        _position += read;
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        VerifyPosition();
        var toRead = (int)Math.Min(buffer.Length, _length - _position);
        var read = await _inner.ReadAsync(buffer.Slice(0, toRead), cancellationToken);
        _position += read;
        return read;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
