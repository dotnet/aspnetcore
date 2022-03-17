// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS.Core;

/// <summary>
/// A <see cref="Stream"/> which only allows for writes.
/// </summary>
internal abstract class WriteOnlyStreamInternal : Stream
{
    ///<inheritdoc/>
    public override bool CanRead => false;

    ///<inheritdoc/>
    public override bool CanWrite => true;

    ///<inheritdoc/>
    public override int ReadTimeout
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    ///<inheritdoc/>
    public override bool CanSeek => false;

    ///<inheritdoc/>
    public override long Length => throw new NotSupportedException();

    ///<inheritdoc/>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    ///<inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    ///<inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    ///<inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    ///<inheritdoc/>
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }
}
