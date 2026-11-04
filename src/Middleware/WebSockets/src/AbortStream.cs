// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.WebSockets;

/// <summary>
/// Used in <see cref="WebSocketMiddleware"/> to wrap the <see cref="HttpContext"/>.Request.Body stream
/// so that we can call <see cref="HttpContext.Abort"/> when the stream is disposed and the WebSocket is in the <see cref="WebSocketState.Aborted"/> state.
/// The Stream provided by Kestrel (and maybe other servers) noops in Dispose as it doesn't know whether it's a graceful close or not
/// and can result in truncated responses if in the graceful case.
///
/// This handles explicit <see cref="WebSocket.Abort"/> calls as well as the Keep-Alive timeout setting <see cref="WebSocketState.Aborted"/> and disposing the stream.
/// </summary>
/// <remarks>
/// Workaround for https://github.com/dotnet/runtime/issues/44272
/// </remarks>
internal sealed class AbortStream : Stream
{
    private readonly Stream _innerStream;
    private readonly HttpContext _httpContext;

    public WebSocket? WebSocket { get; set; }

    public AbortStream(HttpContext httpContext, Stream innerStream)
    {
        _innerStream = innerStream;
        _httpContext = httpContext;
    }

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => _innerStream.CanWrite;

    public override bool CanTimeout => _innerStream.CanTimeout;

    public override long Length => _innerStream.Length;

    public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _innerStream.ReadAsync(buffer, cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _innerStream.Read(buffer, offset, count);
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _innerStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return _innerStream.EndRead(asyncResult);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _innerStream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        _innerStream.EndWrite(asyncResult);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _innerStream.WriteAsync(buffer, cancellationToken);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _innerStream.FlushAsync(cancellationToken);
    }

    public override ValueTask DisposeAsync()
    {
        return _innerStream.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        // Currently, if ManagedWebSocket sets the Aborted state it calls Stream.Dispose after
        if (WebSocket?.State == WebSocketState.Aborted)
        {
            _httpContext.Abort();
        }
        _innerStream.Dispose();
    }
}
