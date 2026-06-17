// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

#nullable enable

namespace Microsoft.AspNetCore.Connections;

internal sealed class CompletionPipeWriter : PipeWriter
{
    private readonly PipeWriter _inner;

    public bool IsCompleted { get; private set; }
    public Exception? CompleteException { get; private set; }
    public bool IsCompletedSuccessfully => IsCompleted && CompleteException == null;

    public CompletionPipeWriter(PipeWriter inner)
    {
        _inner = inner;
    }

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
        IsCompleted = true;
        CompleteException = exception;
        _inner.Complete(exception);
    }

    public override ValueTask CompleteAsync(Exception? exception = null)
    {
        IsCompleted = true;
        CompleteException = exception;
        return _inner.CompleteAsync(exception);
    }

    public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        return _inner.WriteAsync(source, cancellationToken);
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

    public override bool CanGetUnflushedBytes => _inner.CanGetUnflushedBytes;

    public override long UnflushedBytes => _inner.UnflushedBytes;

    public void Reset()
    {
        IsCompleted = false;
        CompleteException = null;
    }
}
