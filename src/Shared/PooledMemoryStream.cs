// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.WebUtilities;

internal sealed class PooledMemoryStream : Stream
{
    private readonly ArrayPool<byte> _bytePool;
    private readonly int _maxCapacity;
    private byte[]? _buffer;
    private int _length;
    private int _position;
    private bool _disposed;

    public PooledMemoryStream(ArrayPool<byte> bytePool, int maxCapacity)
    {
        ArgumentNullException.ThrowIfNull(bytePool);
        ArgumentOutOfRangeException.ThrowIfNegative(maxCapacity);

        _bytePool = bytePool;
        _maxCapacity = maxCapacity;
    }

    public override bool CanRead => !_disposed;

    public override bool CanSeek => !_disposed;

    public override bool CanWrite => !_disposed;

    public override long Length
    {
        get
        {
            ThrowIfDisposed();

            return _length;
        }
    }

    public override long Position
    {
        get
        {
            ThrowIfDisposed();

            return _position;
        }
        set
        {
            ThrowIfDisposed();
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, int.MaxValue);

            _position = (int)value;
        }
    }

    public ReadOnlyMemory<byte> WrittenMemory
    {
        get
        {
            ThrowIfDisposed();

            return _buffer.AsMemory(0, _length);
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);

        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();

        var bytesToRead = Math.Min(_length - Math.Min(_position, _length), buffer.Length);
        if (bytesToRead == 0)
        {
            return 0;
        }

        _buffer.AsSpan(_position, bytesToRead).CopyTo(buffer);
        _position += bytesToRead;

        return bytesToRead;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return new ValueTask<int>(Read(buffer.Span));
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();

        var endPosition = checked(_position + buffer.Length);
        EnsureCapacity(endPosition);

        if (_position > _length)
        {
            _buffer.AsSpan(_length, _position - _length).Clear();
        }

        buffer.CopyTo(_buffer.AsSpan(_position));
        _position = endPosition;
        _length = Math.Max(_length, endPosition);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Write(buffer.Span);

        return ValueTask.CompletedTask;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();

        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        if (newPosition < 0)
        {
            throw new IOException("An attempt was made to move the position before the beginning of the stream.");
        }

        Position = newPosition;

        return _position;
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _maxCapacity);

        var length = (int)value;
        EnsureCapacity(length);
        if (length > _length)
        {
            _buffer.AsSpan(_length, length - _length).Clear();
        }

        _length = length;
        _position = Math.Min(_position, _length);
    }

    public override void Flush()
    {
        ThrowIfDisposed();
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;

            if (_buffer is not null)
            {
                _bytePool.Return(_buffer);
                _buffer = null;
            }
        }

        base.Dispose(disposing);
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity > _maxCapacity)
        {
            throw new IOException("Memory threshold exceeded.");
        }

        if (_buffer is not null && requiredCapacity <= _buffer.Length)
        {
            return;
        }

        if (requiredCapacity == 0)
        {
            return;
        }

        var newCapacity = _buffer is null
            ? requiredCapacity
            : Math.Max(requiredCapacity, Math.Min(_buffer.Length * 2, _maxCapacity));
        var newBuffer = _bytePool.Rent(newCapacity);

        if (_buffer is not null)
        {
            _buffer.AsSpan(0, _length).CopyTo(newBuffer);
            _bytePool.Return(_buffer);
        }

        _buffer = newBuffer;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
