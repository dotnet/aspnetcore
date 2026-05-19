// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

#nullable enable

namespace Microsoft.AspNetCore.Connections;

internal sealed class CompletionPipeReader : PipeReader
{
    private readonly PipeReader _inner;

    public bool IsCompleted { get; private set; }
    public Exception? CompleteException { get; private set; }
    public bool IsCompletedSuccessfully => IsCompleted && CompleteException == null;

    public CompletionPipeReader(PipeReader inner)
    {
        _inner = inner;
    }

    public override void AdvanceTo(SequencePosition consumed)
    {
        _inner.AdvanceTo(consumed);
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        _inner.AdvanceTo(consumed, examined);
    }

    public override ValueTask CompleteAsync(Exception? exception = null)
    {
        IsCompleted = true;
        CompleteException = exception;
        return _inner.CompleteAsync(exception);
    }

    public override void Complete(Exception? exception = null)
    {
        IsCompleted = true;
        CompleteException = exception;
        _inner.Complete(exception);
    }

    public override void CancelPendingRead()
    {
        _inner.CancelPendingRead();
    }

    public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        return _inner.ReadAsync(cancellationToken);
    }

    public override bool TryRead(out ReadResult result)
    {
        return _inner.TryRead(out result);
    }

    public void Reset()
    {
        IsCompleted = false;
        CompleteException = null;
    }
}
