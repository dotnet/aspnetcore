// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

public class NonSeekableReadStream : Stream
{
    private readonly Stream _inner;
    private readonly bool _allowSyncReads;

    public NonSeekableReadStream(byte[] data, bool allowSyncReads = true)
        : this(new MemoryStream(data), allowSyncReads)
    {
    }

    public NonSeekableReadStream(Stream inner, bool allowSyncReads)
    {
        _inner = inner;
        _allowSyncReads = allowSyncReads;
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length
    {
        get { throw new NotSupportedException(); }
    }

    public override long Position
    {
        get { throw new NotSupportedException(); }
        set { throw new NotSupportedException(); }
    }

    public override void Flush()
    {
        // No-op
    }

    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!_allowSyncReads)
        {
            throw new InvalidOperationException("Cannot perform synchronous reads");
        }

        count = Math.Max(count, 1);
        return _inner.Read(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        count = Math.Max(count, 1);
        return _inner.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        return _inner.ReadAsync(buffer, cancellationToken);
    }
}

