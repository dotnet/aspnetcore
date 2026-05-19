// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class HttpResponseStream : WriteOnlyStreamInternal
{
    private readonly IHttpBodyControlFeature _bodyControl;
    private readonly IISHttpContext _context;
    private HttpStreamState _state;

    public HttpResponseStream(IHttpBodyControlFeature bodyControl, IISHttpContext context)
    {
        _bodyControl = bodyControl;
        _context = context;
        _state = HttpStreamState.Closed;
    }

    public override void Flush()
    {
        FlushAsync(default).GetAwaiter().GetResult();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ValidateState(cancellationToken);

        return _context.FlushAsync(cancellationToken);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!_bodyControl.AllowSynchronousIO)
        {
            throw new InvalidOperationException(CoreStrings.SynchronousWritesDisallowed);
        }

        WriteAsync(buffer, offset, count, default).GetAwaiter().GetResult();
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        TaskToApm.End(asyncResult);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateState(cancellationToken);

        return _context.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        ValidateState(cancellationToken);

        return new ValueTask(_context.WriteAsync(source, cancellationToken));
    }

    public void StartAcceptingWrites()
    {
        // Only start if not aborted
        if (_state == HttpStreamState.Closed)
        {
            _state = HttpStreamState.Open;
        }
    }

    public void StopAcceptingWrites()
    {
        // Can't use dispose (or close) as can be disposed too early by user code
        // As exampled in EngineTests.ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes
        _state = HttpStreamState.Closed;
    }

    public void Abort()
    {
        // We don't want to throw an ODE until the app func actually completes.
        if (_state != HttpStreamState.Closed)
        {
            _state = HttpStreamState.Aborted;
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
                throw new ObjectDisposedException(nameof(HttpResponseStream), CoreStrings.WritingToResponseBodyAfterResponseCompleted);
            case HttpStreamState.Aborted:
                if (cancellationToken.IsCancellationRequested)
                {
                    // Aborted state only throws on write if cancellationToken requests it
                    cancellationToken.ThrowIfCancellationRequested();
                }
                break;
        }
    }
}
