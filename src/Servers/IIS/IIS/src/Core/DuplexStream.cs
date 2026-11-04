// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS.Core;

// See https://github.com/aspnet/IISIntegration/issues/426
internal sealed class DuplexStream : Stream
{
    private readonly Stream _requestBody;
    private readonly Stream _responseBody;

    public DuplexStream(Stream requestBody, Stream responseBody)
    {
        _requestBody = requestBody;
        _responseBody = responseBody;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush()
    {
        _responseBody.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _requestBody.Read(buffer, offset, count);
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
        _responseBody.Write(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _requestBody.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _requestBody.ReadAsync(buffer, cancellationToken);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _responseBody.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _responseBody.WriteAsync(buffer, cancellationToken);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return _requestBody.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _responseBody.FlushAsync(cancellationToken);
    }
}
