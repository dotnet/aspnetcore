// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class WrappingPipeWriter : PipeWriter
{
    private PipeWriter _inner;

    public WrappingPipeWriter(PipeWriter inner)
    {
        _inner = inner;
    }

    public void SetInnerPipe(PipeWriter inner)
    {
        _inner = inner;
    }

    public override bool CanGetUnflushedBytes => _inner.CanGetUnflushedBytes;

    public override long UnflushedBytes => _inner.UnflushedBytes;

    public override void Advance(int bytes)
    {
        _inner.Advance(bytes);
    }

    public override void CancelPendingFlush()
    {
        _inner.CancelPendingFlush();
    }

    public override void Complete(Exception? exception = null)
    {
        _inner.Complete(exception);
    }

    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        return _inner.FlushAsync(cancellationToken);
    }

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        return _inner.GetMemory(sizeHint);
    }

    public override Span<byte> GetSpan(int sizeHint = 0)
    {
        return _inner.GetSpan(sizeHint);
    }

    public override Stream AsStream(bool leaveOpen = false)
    {
        return _inner.AsStream(leaveOpen);
    }

    public override ValueTask CompleteAsync(Exception? exception = null)
    {
        return _inner.CompleteAsync(exception);
    }

    public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        return _inner.WriteAsync(source, cancellationToken);
    }
}
