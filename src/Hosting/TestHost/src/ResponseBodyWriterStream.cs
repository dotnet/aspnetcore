// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.TestHost;

internal class ResponseBodyWriterStream : Stream
{
    private readonly ResponseBodyPipeWriter _responseWriter;
    private readonly Func<bool> _allowSynchronousIO;

    public ResponseBodyWriterStream(ResponseBodyPipeWriter responseWriter, Func<bool> allowSynchronousIO)
    {
        _responseWriter = responseWriter;
        _allowSynchronousIO = allowSynchronousIO;
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        if (!_allowSynchronousIO())
        {
            throw new InvalidOperationException("Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true.");
        }

        FlushAsync().GetAwaiter().GetResult();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _responseWriter.FlushAsync(cancellationToken);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!_allowSynchronousIO())
        {
            throw new InvalidOperationException("Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true.");
        }

        // The Pipe Write method requires calling FlushAsync to notify the reader. Call WriteAsync instead.
        WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _responseWriter.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        await _responseWriter.WriteAsync(buffer, cancellationToken);
    }
}
