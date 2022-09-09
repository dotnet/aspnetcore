// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

// Used for CONNECT responses that can't have a body for 2XX responses.
internal sealed class StatusCheckWriteStream : WriteOnlyStream
{
    private readonly Stream _inner;
    private HttpProtocol? _context;

    public StatusCheckWriteStream(Stream inner)
    {
        _inner = inner;
    }

    public void SetRequest(HttpProtocol context)
    {
        _context = context;
    }

    private void CheckStatus()
    {
        Debug.Assert(_context != null);
        if (_context.StatusCode < 300)
        {
            throw new InvalidOperationException(CoreStrings.FormatConnectResponseCanNotHaveBody(_context.StatusCode));
        }
    }

    public override bool CanSeek => _inner.CanSeek;

    public override bool CanWrite => _inner.CanWrite;

    public override bool CanTimeout => _inner.CanTimeout;

    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override int WriteTimeout
    {
        get => _inner.WriteTimeout;
        set => _inner.WriteTimeout = value;
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
        CheckStatus();
        _inner.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        CheckStatus();
        return _inner.FlushAsync(cancellationToken);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        CheckStatus();
        _inner.Write(buffer, offset, count);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        CheckStatus();
        return _inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        CheckStatus();
        return _inner.WriteAsync(source, cancellationToken);
    }

    public override void WriteByte(byte value)
    {
        CheckStatus();
        _inner.WriteByte(value);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        CheckStatus();
        return _inner.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
        => _inner.EndWrite(asyncResult);

    public override void Close()
    {
        CheckStatus();
        _inner.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }
    }
}
