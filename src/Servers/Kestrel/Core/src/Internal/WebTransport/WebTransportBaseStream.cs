// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net.Http;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;

/// <summary>
/// Represents a base WebTransport stream. Do not use directly as it does not
/// contain logic for handling data.
/// </summary>
[RequiresPreviewFeatures("WebTransport is a preview feature")]
public abstract class WebTransportBaseStream : IHttp3Stream, IThreadPoolWorkItem
{
    internal readonly Http3StreamContext _context;
    internal readonly IStreamIdFeature _streamIdFeature = default!;
    internal readonly IStreamAbortFeature _streamAbortFeature = default!;
    internal readonly IProtocolErrorCodeFeature _errorCodeFeature;
    internal KestrelTrace Log => _context.ServiceContext.Log;

    internal WebTransportBaseStream(Http3StreamContext context)
    {
        _context = context;
        _streamIdFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamIdFeature>();
        _streamAbortFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamAbortFeature>();
        _errorCodeFeature = context.ConnectionFeatures.GetRequiredFeature<IProtocolErrorCodeFeature>();

        context.StreamContext.ConnectionClosed.Register(state =>
        {
            var stream = (WebTransportBaseStream)state!;
            stream._context.WebTransportSession.TryRemoveStream(stream._streamIdFeature.StreamId);
        }, this);
    }

    long IHttp3Stream.StreamId => _streamIdFeature.StreamId;

    bool IHttp3Stream.IsReceivingHeader => throw new NotImplementedException(); // todo remove this

    bool IHttp3Stream.IsDraining => throw new NotImplementedException(); // todo remove this

    bool IHttp3Stream.IsRequestStream => throw new NotImplementedException(); // todo remove this

    string IHttp3Stream.TraceIdentifier => throw new NotImplementedException(); // todo remove this

    long IHttp3Stream.StreamTimeoutTicks { get => throw new NotImplementedException(); set => throw new NotImplementedException(); } // todo remove this

    /// <summary>
    /// Aborts the stream using a generic exception and no error code. Also logs a message
    /// </summary>
    public void Abort()
    {
        AbortCore(new(), Http3ErrorCode.NoError); // todo why is this cast necessary?
    }

    void IHttp3Stream.Abort(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        AbortCore(abortReason, errorCode);
    }

    internal virtual void AbortCore(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        Log.Http3StreamAbort(_context.ConnectionId, errorCode, abortReason);
        _errorCodeFeature.Error = (long)errorCode;

        _streamAbortFeature.AbortRead((long)errorCode, abortReason);
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

    /// <summary>
    /// Message loop. TODO remove from public API
    /// </summary>
    public virtual void Execute() { }
}
