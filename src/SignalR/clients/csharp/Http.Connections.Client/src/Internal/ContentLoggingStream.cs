// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

// Logs chunks as HttpContent copies them into the application pipe, without buffering the response.
// The transport owns the wrapped stream and pipe. Disposing this wrapper leaves them open.
internal sealed class ContentLoggingStream(Stream inner, ILogger logger) : Stream
{
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => inner.CanWrite;
    public override long Length => inner.Length;
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush() => inner.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        TransportContentLog.Write(logger, enabled: true, received: true, buffer.AsSpan(offset, count));
        inner.Write(buffer, offset, count);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }
        TransportContentLog.Write(logger, enabled: true, received: true, buffer.AsSpan(offset, count));
        return inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        TransportContentLog.Write(logger, enabled: true, received: true, buffer);
        inner.Write(buffer);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask(Task.FromCanceled(cancellationToken));
        }
        TransportContentLog.Write(logger, enabled: true, received: true, buffer.Span);
        return inner.WriteAsync(buffer, cancellationToken);
    }
#endif
}
