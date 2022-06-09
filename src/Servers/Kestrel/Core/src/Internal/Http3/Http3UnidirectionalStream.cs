// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal abstract class Http3UnidirectionalStream : IHttp3Stream, IThreadPoolWorkItem
{
    protected readonly Http3FrameWriter _frameWriter;
    protected readonly Http3StreamContext _context;
    protected readonly IStreamIdFeature _streamIdFeature;
    protected readonly IProtocolErrorCodeFeature _errorCodeFeature;
    protected readonly Http3RawFrame _incomingFrame = new Http3RawFrame();
    protected volatile int _isClosed;
    protected int _gracefulCloseInitiator;
    protected long _headerType;

    public long StreamId => _streamIdFeature.StreamId;

    public Http3UnidirectionalStream(Http3StreamContext context)
    {
        var httpLimits = context.ServiceContext.ServerOptions.Limits;
        _context = context;
        _streamIdFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamIdFeature>();
        _errorCodeFeature = context.ConnectionFeatures.GetRequiredFeature<IProtocolErrorCodeFeature>();
        _headerType = -1;

        _frameWriter = new Http3FrameWriter(
            context.StreamContext,
            context.TimeoutControl,
            httpLimits.MinResponseDataRate,
            context.MemoryPool,
            context.ServiceContext.Log,
            _streamIdFeature,
            context.ClientPeerSettings,
            this);
        _frameWriter.Reset(context.Transport.Output, context.ConnectionId);
    }

    protected void OnStreamClosed()
    {
        Abort(new ConnectionAbortedException("HTTP_CLOSED_CRITICAL_STREAM"), Http3ErrorCode.InternalError);
    }

    public PipeReader Input => _context.Transport.Input;
    public KestrelTrace Log => _context.ServiceContext.Log;

    public long StreamTimeoutTicks { get; set; }
    public bool IsReceivingHeader => _headerType == -1;
    public bool IsDraining => false;
    public bool IsRequestStream => false;
    public string TraceIdentifier => _context.StreamContext.ConnectionId;

    public void Abort(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        Log.Http3StreamAbort(_context.ConnectionId, errorCode, abortReason);

        _errorCodeFeature.Error = (long)errorCode;
        _frameWriter.Abort(abortReason);

        Input.Complete(abortReason);
    }

    public void OnInputOrOutputCompleted()
    {
        TryClose();
    }

    private bool TryClose()
    {
        if (Interlocked.Exchange(ref _isClosed, 1) == 0)
        {
            Input.Complete();
            return true;
        }

        return false;
    }

    internal async ValueTask SendStreamIdAsync(long id)
    {
        await _frameWriter.WriteStreamIdAsync(id);
    }

    protected async ValueTask<long> TryReadStreamHeaderAsync()
    {
        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-6.2
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
                    var id = VariableLengthIntegerHelper.GetInteger(readableBuffer, out consumed, out examined);
                    if (id != -1)
                    {
                        return id;
                    }
                }

                if (result.IsCompleted)
                {
                    return -1;
                }
            }
            finally
            {
                Input.AdvanceTo(consumed, examined);
            }
        }

        return -1;
    }

    public virtual async Task ProcessRequestAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        try
        {
            _headerType = await TryReadStreamHeaderAsync();
            _context.StreamLifetimeHandler.OnStreamHeaderReceived(this);

            switch (_headerType)
            {
                case (long)Http3StreamType.WebTransportUnidirectional: // todo is this switch even necessary?
                    await HandleStream();
                    break;
                default:
                    // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-6.2-6
                    throw new Http3StreamErrorException(CoreStrings.FormatHttp3ControlStreamErrorUnsupportedType(_headerType), Http3ErrorCode.StreamCreationError);
            }
        }
        catch (Http3StreamErrorException ex)
        {
            Abort(new ConnectionAbortedException(ex.Message), ex.ErrorCode);
        }
        catch (Http3ConnectionErrorException ex)
        {
            _errorCodeFeature.Error = (long)ex.ErrorCode;
            _context.StreamLifetimeHandler.OnStreamConnectionError(ex);
        }
        finally
        {
            _context.StreamLifetimeHandler.OnStreamCompleted(this);
        }
    }

    private async Task HandleStream()
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
                        await ProcessStream(framePayload);
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
    }

    private async ValueTask ProcessStream(ReadOnlySequence<byte> payload)
    {
        Log.LogInformation(System.Text.Encoding.ASCII.GetString(payload.ToArray()));
        await Task.CompletedTask;
    }

    public void StopProcessingNextRequest()
        => StopProcessingNextRequest(serverInitiated: true);

    public void StopProcessingNextRequest(bool serverInitiated)
    {
        var initiator = serverInitiated ? GracefulCloseInitiator.Server : GracefulCloseInitiator.Client;

        if (Interlocked.CompareExchange(ref _gracefulCloseInitiator, initiator, GracefulCloseInitiator.None) == GracefulCloseInitiator.None)
        {
            Input.CancelPendingRead();
        }
    }

    /// <summary>
    /// Used to kick off the request processing loop by derived classes.
    /// </summary>
    public abstract void Execute();

    private static class GracefulCloseInitiator
    {
        public const int None = 0;
        public const int Server = 1;
        public const int Client = 2;
    }
}
