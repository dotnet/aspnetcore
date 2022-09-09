// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

// Throws if the Http Status code is less than 300.
// For use with CONNECT responses that can't have a response body for 2xx.
internal sealed class StatusCheckPipeWriter : PipeWriter
{
    private readonly PipeWriter _inner;
    private HttpProtocol? _context;

    public StatusCheckPipeWriter(PipeWriter inner)
    {
        _inner = inner;
    }

    public void SetRequest(HttpProtocol context)
    {
        _context = context;
    }

    public override bool CanGetUnflushedBytes => _inner.CanGetUnflushedBytes;

    public override long UnflushedBytes => _inner.UnflushedBytes;

    private void CheckStatus()
    {
        Debug.Assert(_context != null);
        if (_context.StatusCode < 300)
        {
            throw new InvalidOperationException(CoreStrings.FormatConnectResponseCanNotHaveBody(_context.StatusCode));
        }
    }

    public override void Advance(int bytes)
    {
        CheckStatus();
        _inner.Advance(bytes);
    }

    public override void CancelPendingFlush()
    {
        CheckStatus();
        _inner.CancelPendingFlush();
    }

    public override void Complete(Exception? exception = null)
    {
        CheckStatus();
        _inner.Complete(exception);
    }

    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        CheckStatus();
        return _inner.FlushAsync(cancellationToken);
    }

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        CheckStatus();
        return _inner.GetMemory(sizeHint);
    }

    public override Span<byte> GetSpan(int sizeHint = 0)
    {
        CheckStatus();
        return _inner.GetSpan(sizeHint);
    }

    public override Stream AsStream(bool leaveOpen = false)
    {
        CheckStatus();
        return _inner.AsStream(leaveOpen);
    }

    public override ValueTask CompleteAsync(Exception? exception = null)
    {
        CheckStatus();
        return _inner.CompleteAsync(exception);
    }

    public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        CheckStatus();
        return _inner.WriteAsync(source, cancellationToken);
    }
}
