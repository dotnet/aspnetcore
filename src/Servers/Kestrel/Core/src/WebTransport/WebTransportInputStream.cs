// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

/// <summary>
/// Represents an inbound unidirectional stream for the WebTransport protocol.
/// </summary>
[RequiresPreviewFeatures("WebTransport is a preview feature")]
public class WebTransportInputStream : WebTransportBaseStream // todo add QPackDecoder for messages?
{
    private readonly Http3FrameWriter _frameWriter;
    private readonly Http3RawFrame _incomingFrame = new();
    private volatile int _isClosed;
    private Pipe RequestBodyPipe { get; set; } = default!;
    private PipeReader Input => _context.Transport.Input;

    internal WebTransportInputStream(Http3StreamContext context) : base(context)
    {
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
    internal override void AbortCore(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        _isClosed = 1;

        base.AbortCore(abortReason, errorCode);

        _frameWriter.Abort(abortReason);

        Input.Complete(abortReason);
    }

    /// <summary>
    /// The message processing loop. TODO make this not public API
    /// </summary>
    /// <exception cref="Http3ConnectionErrorException">If the stream is completed and closed by the client</exception>
    public override void Execute()
    {
        _ = Task.Run(async () =>
        {
            while (_isClosed == 0)
            {
                var result = await Input.ReadAsync();
                var readableBuffer = result.Buffer;
                var consumed = readableBuffer.Start;
                var examined = readableBuffer.End;

                try
                {
                    if (!readableBuffer.IsEmpty)
                    {
                        // need to kick off httpprotocol process request async here.
                        while (Http3FrameReader.TryReadFrame(ref readableBuffer, _incomingFrame, out var framePayload))
                        {
                            Log.Http3FrameReceived(_context.ConnectionId, _streamIdFeature.StreamId, _incomingFrame);

                            consumed = examined = framePayload.End;
                            await ProcessDataFrameAsync(framePayload);
                        }
                    }

                    if (result.IsCompleted)
                    {
                        if (!_context.StreamContext.ConnectionClosed.IsCancellationRequested)
                        {
                            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-6.2.1-2
                            throw new Http3ConnectionErrorException(CoreStrings.Http3ErrorControlStreamClientClosedInbound, Http3ErrorCode.ClosedCriticalStream);
                        }

                        return;
                    }
                }
                finally
                {
                    Input.AdvanceTo(consumed, examined);
                }
            }
        });
    }

    private Task ProcessDataFrameAsync(in ReadOnlySequence<byte> payload)
    {
        foreach (var segment in payload)
        {
            RequestBodyPipe.Writer.Write(segment.Span); // todo surface to application
        }

        return RequestBodyPipe.Writer.FlushAsync().GetAsTask();
    }
}
