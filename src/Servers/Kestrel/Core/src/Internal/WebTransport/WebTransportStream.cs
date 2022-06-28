// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.IO.Pipelines;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

/// <summary>
/// Represents a base WebTransport stream. Do not use directly as it does not
/// contain logic for handling data.
/// </summary>
public class WebTransportStream : Stream
{
    private bool _canRead;
    private bool _isClosed;

    private PipeReader Input => _context.Transport.Input;
    private PipeWriter Output => _context.Transport.Output;

    internal readonly Http3StreamContext _context;
    internal readonly IStreamIdFeature _streamIdFeature = default!;
    internal readonly IStreamAbortFeature _streamAbortFeature = default!;
    internal readonly IProtocolErrorCodeFeature _errorCodeFeature = default!;
    internal readonly WebTransportStreamType _type;

    internal KestrelTrace Log => _context.ServiceContext.Log;

    /// <summary>
    /// True if data can be read from this stream. False otherwise.
    /// </summary>
    public override bool CanRead => _canRead && !_isClosed;

    /// <summary>
    /// Seeking is not supported by WebTransport.
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    /// True if data can be written from this stream. False otherwise.
    /// </summary>
    public override bool CanWrite => _type != WebTransportStreamType.Input && !_isClosed;

    /// <summary>
    /// The unique identifier of the stream.
    /// </summary>
    public long StreamId => _streamIdFeature.StreamId;

    internal WebTransportStream(Http3StreamContext context, WebTransportStreamType type)
    {
        _canRead = type != WebTransportStreamType.Output;
        _type = type;
        _context = context;
        _streamIdFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamIdFeature>();
        _streamAbortFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamAbortFeature>();
        _errorCodeFeature = context.ConnectionFeatures.GetRequiredFeature<IProtocolErrorCodeFeature>();

        // will not trigger if closed only of of the directions of a stream. Stream must be fully
        // ended before this will be called. Then it will be considered an abort
        context.StreamContext.ConnectionClosed.Register(state =>
        {
            var stream = (WebTransportStream)state!;
            stream._context.WebTransportSession?.TryRemoveStream(stream._streamIdFeature.StreamId);
        }, this);
    }

    private async Task<int> ReadAsyncInternal(Memory<byte> destination, int offset, int count, CancellationToken cancellationToken)
    {
        try
        {
            var result = await Input.ReadAsync(cancellationToken);

            if (result.IsCanceled)
            {
                throw new OperationCanceledException("The read was canceled");
            }

            var buffer = result.Buffer;
            var length = buffer.Length;

            var consumed = buffer.End;

            if (length != 0)
            {
                var actual = (int)Math.Min(length, Math.Min(destination.Length, count));

                var slice = actual == length ? buffer : buffer.Slice(0, actual);
                consumed = slice.End;
                slice.CopyTo(destination[offset..].Span);

                Input.AdvanceTo(consumed);

                return actual;
            }
            else
            {
                Input.AdvanceTo(consumed);
                _canRead = false;
                return 0;
            }
        }
        catch (Exception)
        {
            _canRead = false;
            return 0;
        }
    }

    internal virtual void AbortCore(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;

        Log.Http3StreamAbort(_context.ConnectionId, errorCode, abortReason);

        _errorCodeFeature.Error = (long)errorCode;

        if (CanRead)
        {
            _streamAbortFeature.AbortRead((long)errorCode, abortReason);
            Input.Complete(abortReason);
        }

        if (CanWrite)
        {
            _streamAbortFeature.AbortWrite((long)errorCode, abortReason);
            Output.Complete(abortReason);
        }
    }

    /// <summary>
    /// Hard abort the stream and cancel data transmission.
    /// </summary>
    /// <param name="abortReason"></param>
    public void Abort(ConnectionAbortedException abortReason)
    {
        AbortCore(abortReason, Http3ErrorCode.InternalError);
    }

    /// <summary>
    /// Soft close the stream and end data transmission.
    /// </summary>
    public override void Close()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;

        if (CanRead)
        {
            Input.Complete();
        }

        if (CanWrite)
        {
            Output.Complete();
        }
    }

    /// <summary>
    /// Flushes the Output pipe.
    /// </summary>
    /// <exception cref="NotSupportedException">If this stream has no output pipe</exception>
    public override void Flush()
    {
        if (!CanWrite)
        {
            throw new NotSupportedException();
        }

        if (!_context.ServiceContext.ServerOptions.AllowSynchronousIO)
        {
            throw new InvalidOperationException(CoreStrings.SynchronousWritesDisallowed);
        }

        FlushAsync(default).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Flushes the Output pipe.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <exception cref="NotSupportedException">If this stream has no output pipe</exception>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        if (!CanWrite)
        {
            throw new NotSupportedException();
        }

        return Output.FlushAsync(cancellationToken).GetAsTask();
    }

    /// <summary>
    /// Read data from the input pipe.
    /// </summary>
    /// <param name="buffer">The buffer to read the data into.</param>
    /// <param name="offset">The starting index of the buffer to store data into.</param>
    /// <param name="count">The ending index of the buffer to store data into.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!CanRead)
        {
            throw new NotSupportedException();
        }

        if (!_context.ServiceContext.ServerOptions.AllowSynchronousIO)
        {
            throw new InvalidOperationException(CoreStrings.SynchronousReadsDisallowed);
        }

        return ReadAsyncInternal(new(buffer), offset, count, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Writes data from the buffer to the stream.
    /// </summary>
    /// <param name="buffer">Data to write.</param>
    /// <param name="offset">offset from which to start reading in data ion the buffer.</param>
    /// <param name="count">number of bytes to write from the buffer.</param>
    /// <exception cref="NotSupportedException">Not supported on readonly streams.</exception>
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!CanWrite)
        {
            throw new NotSupportedException();
        }

        if (!_context.ServiceContext.ServerOptions.AllowSynchronousIO)
        {
            throw new InvalidOperationException(CoreStrings.SynchronousWritesDisallowed);
        }

        WriteAsync(buffer, offset, count, default).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Writes data from the buffer to the stream.
    /// </summary>
    /// <param name="buffer">Data to write.</param>
    /// <param name="offset">offset from which to start reading in data ion the buffer.</param>
    /// <param name="count">number of bytes to write from the buffer.</param>
    /// <param name="cancellationToken">THe cancellation token for this operation.</param>
    /// <returns>An awaitable task that completes when the write is done.</returns>
    /// <exception cref="NotSupportedException">Not supported on readonly streams.</exception>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (!CanWrite)
        {
            throw new NotSupportedException();
        }

        return Output.WriteAsync(new Memory<byte>(buffer, offset, count), cancellationToken).GetAsTask();
    }

    /// <summary>
    /// Writes data from the buffer to the stream.
    /// </summary>
    /// <param name="buffer">Data to write.</param>
    /// <param name="cancellationToken">THe cancellation token for this operation.</param>
    /// <returns>An awaitable task that completes when the write is done.</returns>
    /// <exception cref="NotSupportedException">Not supported on readonly streams.</exception>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        if (!CanWrite)
        {
            throw new NotSupportedException();
        }

        return Output.WriteAsync(buffer, cancellationToken).GetAsValueTask();
    }

    #region Unsupported stream functionality
    /// <summary>
    /// WebTransport streams don't have a fixed length.
    /// So this field should not be used.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override long Length => throw new NotSupportedException();

    /// <summary>
    /// WebTransport streams can't seek. So this field should not be used
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Don't use. Seeking is not supported
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Don't use. Length is not defined
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }
    #endregion
}
