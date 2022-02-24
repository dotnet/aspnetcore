// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

public class NonSeekableReadStream : Stream
{
    private readonly Stream _inner;

    public NonSeekableReadStream(byte[] data)
        : this(new MemoryStream(data))
    {
    }

    public NonSeekableReadStream(Stream inner)
    {
        _inner = inner;
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
        throw new NotImplementedException();
    }

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
        return _inner.Read(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _inner.ReadAsync(buffer, offset, count, cancellationToken);
    }
}
