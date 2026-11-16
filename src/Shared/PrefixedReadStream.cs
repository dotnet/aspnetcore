// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Internal;

/// <summary>
/// A read-only stream that first returns bytes that were already read from <c>inner</c>, and then continues
/// reading from <c>inner</c>. The buffered bytes are returned with the same boundary as the original first read:
/// no read returns bytes from both before and after <c>firstReadLength</c>, or from both the buffer and <c>inner</c>.
/// This lets consumers whose behavior depends on read boundaries, such as <see cref="StreamReader"/> byte order
/// mark detection, see the same reads as if they had read from <c>inner</c> directly.
/// </summary>
/// <remarks>
/// The stream does not copy or own <c>buffer</c>. The caller must keep it unchanged while this stream is in use and
/// must not expose this stream to code that can keep it beyond that.
/// </remarks>
internal sealed class PrefixedReadStream : Stream
{
    private readonly byte[] _buffer;
    private readonly int _firstReadLength;
    private readonly int _length;
    private readonly Stream _inner;
    private int _position;

    public PrefixedReadStream(byte[] buffer, int firstReadLength, int length, Stream inner)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegative(firstReadLength);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, firstReadLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, buffer.Length);

        _buffer = buffer;
        _firstReadLength = firstReadLength;
        _length = length;
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
        if (TryReadBuffered(buffer, out var read))
        {
            return read;
        }

        return _inner.Read(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (TryReadBuffered(buffer.Span, out var read))
        {
            return new ValueTask<int>(read);
        }

        return _inner.ReadAsync(buffer, cancellationToken);
    }

    private bool TryReadBuffered(Span<byte> destination, out int read)
    {
        var end = _position < _firstReadLength ? _firstReadLength : _length;
        var remaining = end - _position;
        if (remaining == 0)
        {
            read = 0;
            return false;
        }

        read = Math.Min(remaining, destination.Length);
        _buffer.AsSpan(_position, read).CopyTo(destination);
        _position += read;
        return true;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
