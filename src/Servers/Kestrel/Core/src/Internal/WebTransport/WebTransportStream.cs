// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.IO.Pipelines;
using System.Net.Http;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

/// <summary>
/// Represents a base WebTransport stream. Do not use directly as it does not
/// contain logic for handling data.
/// </summary>
[RequiresPreviewFeatures("WebTransport is a preview feature")]
internal class WebTransportStream : Stream, IHttp3Stream
{
    internal readonly Http3StreamContext _context;
    internal readonly IStreamIdFeature _streamIdFeature = default!;
    internal readonly IStreamAbortFeature _streamAbortFeature = default!;
    internal readonly IProtocolErrorCodeFeature _errorCodeFeature;
    internal readonly WebTransportStreamType _type;
    internal KestrelTrace Log => _context.ServiceContext.Log;

    private readonly Http3FrameWriter _frameWriter;
    private readonly Http3RawFrame _incomingFrame = new(); // todo I probably did something wrong as this is never used
    private volatile bool _isClosed;
    private Pipe RequestBodyPipe { get; set; } = default!;
    private PipeReader Input => _context.Transport.Input;

    internal WebTransportStream(Http3StreamContext context, WebTransportStreamType type)
    {
        _type = type;
        _isClosed = false;
        _context = context;
        _streamIdFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamIdFeature>();
        _streamAbortFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamAbortFeature>();
        _errorCodeFeature = context.ConnectionFeatures.GetRequiredFeature<IProtocolErrorCodeFeature>();

        context.StreamContext.ConnectionClosed.Register(state =>
        {
            var stream = (WebTransportStream)state!;
            stream._context.WebTransportSession.TryRemoveStream(stream._streamIdFeature.StreamId);
        }, this);

        var httpLimits = context.ServiceContext.ServerOptions.Limits;

        _frameWriter = new Http3FrameWriter(
            context.StreamContext,
            context.TimeoutControl,
            httpLimits.MinResponseDataRate,
            context.MemoryPool,
            context.ServiceContext.Log,
            _streamIdFeature,
            context.ClientPeerSettings,
            this);

        RequestBodyPipe = CreateRequestBodyPipe(64 * 1024);

        _frameWriter.Reset(context.Transport.Output, context.ConnectionId);
    }

    long IHttp3Stream.StreamId => _streamIdFeature.StreamId;

    /// <summary>
    /// Hard abort the stream and cancel data transmission.
    /// </summary>
    /// <param name="abortReason"></param>
    public void Abort(ConnectionAbortedException abortReason)
    {
        AbortCore(abortReason, Http3ErrorCode.InternalError);
    }

    void IHttp3Stream.Abort(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        AbortCore(abortReason, errorCode);
    }

    internal virtual void AbortCore(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        _isClosed = true;

        Log.Http3StreamAbort(_context.ConnectionId, errorCode, abortReason);
        _errorCodeFeature.Error = (long)errorCode;

        _streamAbortFeature.AbortRead((long)errorCode, abortReason);

        RequestBodyPipe.Writer.Complete(new OperationCanceledException());

        _frameWriter.Abort(abortReason);

        Input.Complete(abortReason);
    }

    internal Pipe CreateRequestBodyPipe(uint windowSize) => new(new PipeOptions
    (
        pool: _context.MemoryPool,
        readerScheduler: _context.ServiceContext.Scheduler,
        writerScheduler: PipeScheduler.Inline,
        // Never pause within the window range. Flow control will prevent more data from being added.
        // See the assert in OnDataAsync.
        pauseWriterThreshold: windowSize + 1,
        resumeWriterThreshold: windowSize + 1,
        useSynchronizationContext: false,
        minimumSegmentSize: _context.MemoryPool.GetMinimumSegmentSize()
    ));

    //internal void Execute()
    //{
    //    // do the message reading and writting loop but place data in a buffer
    //    // then the user can read from it by calling the functions below from the
    //    // stream class

    //}

    /// <summary>
    /// Can data be read from this stream
    /// </summary>
    public override bool CanRead => _type != WebTransportStreamType.Output && !_isClosed;

    /// <summary>
    /// Seeking is not supported by WebTransport
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    /// Can data be written to this stream
    /// </summary>
    public override bool CanWrite => _type != WebTransportStreamType.Input && !_isClosed;

    public override void Flush()
    {
        FlushAsync(default).GetAwaiter().GetResult();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _frameWriter.FlushAsync(null, cancellationToken).GetAsTask();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!CanRead)
        {
            throw new NotSupportedException();
        }

        // since there is no seekig we ignore the offset parameter
        return ReadAsyncInternal(new(buffer), default).GetAwaiter().GetResult();
    }

    private async ValueTask<int> ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await Input.ReadAsync(cancellationToken);

            if (result.IsCanceled)
            {
                throw new OperationCanceledException("The read was canceled");
            }

            var buffer = result.Buffer;
            var length = buffer.Length;

            var consumed = buffer.End;
            try
            {
                if (length != 0)
                {
                    var actual = (int)Math.Min(length, destination.Length);

                    var slice = actual == length ? buffer : buffer.Slice(0, actual);
                    consumed = slice.End;
                    slice.CopyTo(destination.Span);

                    return actual;
                }

                if (result.IsCompleted)
                {
                    return 0;
                }
            }
            finally
            {
                Input.AdvanceTo(consumed);
            }
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!CanWrite)
        {
            throw new NotSupportedException();
        }

        WriteAsync(buffer, offset, count, default).GetAwaiter().GetResult();
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _frameWriter.WriteDataAsync(new ReadOnlySequence<byte>(new Memory<byte>(buffer, offset, count))).GetAsTask();
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

    #region Unsupported IHttp3Stream functionality
    bool IHttp3Stream.IsReceivingHeader => throw new NotSupportedException(); // not-applicable

    bool IHttp3Stream.IsDraining => throw new NotSupportedException(); // not-applicable

    bool IHttp3Stream.IsRequestStream => throw new NotSupportedException(); // not-applicable

    string IHttp3Stream.TraceIdentifier => throw new NotSupportedException(); // not-applicable

    long IHttp3Stream.StreamTimeoutTicks { // not-applicable
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    #endregion
}
