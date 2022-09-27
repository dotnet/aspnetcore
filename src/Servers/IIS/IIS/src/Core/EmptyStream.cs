// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class EmptyStream : ReadOnlyStream
{
    private readonly IHttpBodyControlFeature _bodyControl;
    private HttpStreamState _state;
    private Exception? _error;

    public EmptyStream(IHttpBodyControlFeature bodyControl)
    {
        _bodyControl = bodyControl;
        _state = HttpStreamState.Open;
    }

    public override void Flush()
    {
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!_bodyControl.AllowSynchronousIO)
        {
            throw new InvalidOperationException(CoreStrings.SynchronousReadsDisallowed);
        }

        return 0;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateState(cancellationToken);

        return Task.FromResult(0);
    }

    public void StopAcceptingReads()
    {
        // Can't use dispose (or close) as can be disposed too early by user code
        // As exampled in EngineTests.ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes
        _state = HttpStreamState.Closed;
    }

    public void Abort(Exception? error = null)
    {
        // We don't want to throw an ODE until the app func actually completes.
        // If the request is aborted, we throw a TaskCanceledException instead,
        // unless error is not null, in which case we throw it.
        if (_state != HttpStreamState.Closed)
        {
            _state = HttpStreamState.Aborted;
            _error = error;
        }
    }

    private void ValidateState(CancellationToken cancellationToken)
    {
        switch (_state)
        {
            case HttpStreamState.Open:
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                break;
            case HttpStreamState.Closed:
                throw new ObjectDisposedException(nameof(HttpRequestStream));
            case HttpStreamState.Aborted:
                if (_error != null)
                {
                    ExceptionDispatchInfo.Capture(_error).Throw();
                }
                else
                {
                    throw new TaskCanceledException();
                }
                break;
        }
    }
}
