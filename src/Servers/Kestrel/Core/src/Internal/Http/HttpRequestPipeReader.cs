// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

/// <summary>
/// Default HttpRequest PipeReader implementation to be used by Kestrel.
/// </summary>
internal sealed class HttpRequestPipeReader : PipeReader
{
    private MessageBody? _body;
    private HttpStreamState _state;
    private ExceptionDispatchInfo? _error;

    public HttpRequestPipeReader()
    {
        _state = HttpStreamState.Closed;
    }

    public override void AdvanceTo(SequencePosition consumed)
    {
        ValidateState();

        _body!.AdvanceTo(consumed);
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        ValidateState();

        _body!.AdvanceTo(consumed, examined);
    }

    public override void CancelPendingRead()
    {
        ValidateState();

        _body!.CancelPendingRead();
    }

    public override void Complete(Exception? exception = null)
    {
        ValidateState();

        _body!.Complete(exception);
    }

    public override ValueTask CompleteAsync(Exception? exception = null)
    {
        ValidateState();

        return _body!.CompleteAsync(exception);
    }

    public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        ValidateState(cancellationToken);

        return _body!.ReadAsync(cancellationToken);
    }

    public override bool TryRead(out ReadResult result)
    {
        ValidateState();

        return _body!.TryRead(out result);
    }

    public void StartAcceptingReads(MessageBody body)
    {
        // Only start if not aborted
        if (_state == HttpStreamState.Closed)
        {
            _state = HttpStreamState.Open;
            _body = body;
        }
    }

    public void StopAcceptingReads()
    {
        // Can't use dispose (or close) as can be disposed too early by user code
        // As exampled in EngineTests.ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes
        _state = HttpStreamState.Closed;
        _body = null;
    }

    public void Abort(Exception? error = null)
    {
        // If the request is aborted, we throw a TaskCanceledException instead,
        // unless error is not null, in which case we throw it.

        if (error is not null)
        {
            _error ??= ExceptionDispatchInfo.Capture(error);
        }
        else
        {
            // Do not change state if there is an error because we don't want to throw a TaskCanceledException
            // and we do not want to introduce any memory barriers at this layer. This is just for reporting errors
            // early when we know the transport will fail.
            _state = HttpStreamState.Aborted;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateState(CancellationToken cancellationToken = default)
    {
        switch (_state)
        {
            case HttpStreamState.Open:
                cancellationToken.ThrowIfCancellationRequested();
                break;
            case HttpStreamState.Closed:
                ThrowObjectDisposedException();
                break;
            case HttpStreamState.Aborted:
                ThrowTaskCanceledException();
                break;
        }

        // Abort errors are always terminal. We don't use _state to see if there is an error
        // because we don't want to be forced to synchronize. This is best effort. The transport should
        // report the same error we aborted it with if the read gets that far.
        _error?.Throw();

        static void ThrowObjectDisposedException() => throw new ObjectDisposedException(nameof(HttpRequestStream));
        static void ThrowTaskCanceledException() => throw new TaskCanceledException();
    }
}
