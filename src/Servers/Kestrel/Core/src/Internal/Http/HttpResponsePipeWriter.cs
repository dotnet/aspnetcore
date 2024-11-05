// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal sealed class HttpResponsePipeWriter : PipeWriter
{
    private readonly IHttpResponseControl _pipeControl;

    private HttpStreamState _state;
    private Task _completeTask = Task.CompletedTask;

    public HttpResponsePipeWriter(IHttpResponseControl pipeControl)
    {
        _pipeControl = pipeControl;
        _state = HttpStreamState.Closed;
    }

    public override void Advance(int bytes)
    {
        ValidateState();
        _pipeControl.Advance(bytes);
    }

    public override void CancelPendingFlush()
    {
        ValidateState();
        _pipeControl.CancelPendingFlush();
    }

    public override void Complete(Exception? exception = null)
    {
        ValidateState();
        _completeTask = _pipeControl.CompleteAsync(exception);
    }

    public override ValueTask CompleteAsync(Exception? exception = null)
    {
        Complete();
        return new ValueTask(_completeTask);
    }

    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        ValidateState(cancellationToken);
        return _pipeControl.FlushPipeAsync(cancellationToken);
    }

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        ValidateState();
        return _pipeControl.GetMemory(sizeHint);
    }

    public override Span<byte> GetSpan(int sizeHint = 0)
    {
        ValidateState();
        return _pipeControl.GetSpan(sizeHint);
    }

    public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        ValidateState(cancellationToken);
        return _pipeControl.WritePipeAsync(source, cancellationToken);
    }

    public void StartAcceptingWrites()
    {
        // Only start if not aborted
        if (_state == HttpStreamState.Closed)
        {
            _state = HttpStreamState.Open;
        }
    }

    public Task StopAcceptingWritesAsync()
    {
        // Can't use dispose (or close) as can be disposed too early by user code
        // As exampled in EngineTests.ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes
        _state = HttpStreamState.Closed;
        return _completeTask;
    }

    public void Abort()
    {
        // We don't want to throw an ODE until the app func actually completes.
        if (_state != HttpStreamState.Closed)
        {
            _state = HttpStreamState.Aborted;
        }
    }

    public override bool CanGetUnflushedBytes => true;
    public override long UnflushedBytes => _pipeControl.UnflushedBytes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateState(CancellationToken cancellationToken = default)
    {
        var state = _state;
        if (state == HttpStreamState.Open || state == HttpStreamState.Aborted)
        {
            // Aborted state only throws on write if cancellationToken requests it
            cancellationToken.ThrowIfCancellationRequested();
        }
        else
        {
            ThrowObjectDisposedException();
        }

        static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(nameof(HttpResponseStream), CoreStrings.WritingToResponseBodyAfterResponseCompleted);
        }
    }
}
