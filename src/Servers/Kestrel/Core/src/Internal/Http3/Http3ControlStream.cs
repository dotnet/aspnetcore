// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal abstract class Http3ControlStream : IHttp3Stream, IThreadPoolWorkItem
{
    private const int ControlStreamTypeId = 0;
    private const int EncoderStreamTypeId = 2;
    private const int DecoderStreamTypeId = 3;

    private readonly Http3FrameWriter _frameWriter;
    private readonly Http3StreamContext _context;
    private readonly Http3PeerSettings _serverPeerSettings;
    private readonly IStreamIdFeature _streamIdFeature;
    private readonly IStreamClosedFeature _streamClosedFeature;
    private readonly IProtocolErrorCodeFeature _errorCodeFeature;
    private readonly Http3RawFrame _incomingFrame = new Http3RawFrame();
    private volatile int _isClosed;
    private long _headerType;
    private readonly Lock _completionLock = new();

    private bool _haveReceivedSettingsFrame;
    private StreamCompletionFlags _completionState;

    public bool EndStreamReceived => (_completionState & StreamCompletionFlags.EndStreamReceived) == StreamCompletionFlags.EndStreamReceived;
    public bool IsAborted => (_completionState & StreamCompletionFlags.Aborted) == StreamCompletionFlags.Aborted;
    public bool IsCompleted => (_completionState & StreamCompletionFlags.Completed) == StreamCompletionFlags.Completed;

    public long StreamId => _streamIdFeature.StreamId;

    public Http3ControlStream(Http3StreamContext context, long? headerType)
    {
        var httpLimits = context.ServiceContext.ServerOptions.Limits;
        _context = context;
        _serverPeerSettings = context.ServerPeerSettings;
        _streamIdFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamIdFeature>();
        _streamClosedFeature = context.ConnectionFeatures.GetRequiredFeature<IStreamClosedFeature>();
        _errorCodeFeature = context.ConnectionFeatures.GetRequiredFeature<IProtocolErrorCodeFeature>();
        _headerType = headerType ?? -1;

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

    private void OnStreamClosed()
    {
        ApplyCompletionFlag(StreamCompletionFlags.Completed);
    }

    public PipeReader Input => _context.Transport.Input;
    public KestrelTrace Log => _context.ServiceContext.Log;

    public long StreamTimeoutTimestamp { get; set; }
    public bool IsReceivingHeader => _headerType == -1;
    public bool IsDraining => false;
    public bool IsRequestStream => false;
    public string TraceIdentifier => _context.StreamContext.ConnectionId;

    public void Abort(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        lock (_completionLock)
        {
            if (IsCompleted || IsAborted)
            {
                return;
            }

            var (oldState, newState) = ApplyCompletionFlag(StreamCompletionFlags.Aborted);

            if (oldState == newState)
            {
                return;
            }

            Log.Http3StreamAbort(TraceIdentifier, errorCode, abortReason);

            _errorCodeFeature.Error = (long)errorCode;
            _frameWriter.Abort(abortReason);

            Input.Complete(abortReason);
        }
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

    private (StreamCompletionFlags OldState, StreamCompletionFlags NewState) ApplyCompletionFlag(StreamCompletionFlags completionState)
    {
        lock (_completionLock)
        {
            var oldCompletionState = _completionState;
            _completionState |= completionState;

            return (oldCompletionState, _completionState);
        }
    }

    internal async ValueTask ProcessOutboundSendsAsync(long id)
    {
        _streamClosedFeature.OnClosed(static state =>
        {
            var stream = (Http3ControlStream)state!;
            stream.OnStreamClosed();
        }, this);

        await _frameWriter.WriteStreamIdAsync(id);
        await _frameWriter.WriteSettingsAsync(_serverPeerSettings.GetNonProtocolDefaults());
    }

    internal ValueTask<FlushResult> SendGoAway(long id)
    {
        Log.Http3GoAwayStreamId(_context.ConnectionId, id);
        return _frameWriter.WriteGoAway(id);
    }

    private async ValueTask<long> TryReadStreamHeaderAsync()
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

    public async Task ProcessRequestAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        try
        {
            // todo: the _headerType should be read earlier
            // and by the Http3PendingStream. However, to
            // avoid perf issues with the current implementation
            // we can defer the reading until now
            // (https://github.com/dotnet/aspnetcore/issues/42789)
            if (_headerType == -1)
            {
                _headerType = await TryReadStreamHeaderAsync();
            }

            _context.StreamLifetimeHandler.OnStreamHeaderReceived(this);

            switch (_headerType)
            {
                case ControlStreamTypeId:
                    if (!_context.StreamLifetimeHandler.OnInboundControlStream(this))
                    {
                        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-6.2.1
                        throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ControlStreamErrorMultipleInboundStreams("control"), Http3ErrorCode.StreamCreationError, ConnectionEndReason.StreamCreationError);
                    }

                    await HandleControlStream();
                    break;
                case EncoderStreamTypeId:
                    if (!_context.StreamLifetimeHandler.OnInboundEncoderStream(this))
                    {
                        // https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#section-4.2
                        throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ControlStreamErrorMultipleInboundStreams("encoder"), Http3ErrorCode.StreamCreationError, ConnectionEndReason.StreamCreationError);
                    }

                    await HandleEncodingDecodingTask();
                    break;
                case DecoderStreamTypeId:
                    if (!_context.StreamLifetimeHandler.OnInboundDecoderStream(this))
                    {
                        // https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#section-4.2
                        throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ControlStreamErrorMultipleInboundStreams("decoder"), Http3ErrorCode.StreamCreationError, ConnectionEndReason.StreamCreationError);
                    }
                    await HandleEncodingDecodingTask();
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
            ApplyCompletionFlag(StreamCompletionFlags.Completed);
            _context.StreamLifetimeHandler.OnStreamCompleted(this);
        }
    }

    private async Task HandleControlStream()
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
                        await ProcessHttp3ControlStream(framePayload);
                    }
                }

                if (result.IsCompleted)
                {
                    return;
                }
            }
            finally
            {
                Input.AdvanceTo(consumed, examined);
            }
        }
    }

    private async ValueTask HandleEncodingDecodingTask()
    {
        // Noop encoding and decoding task. Settings make it so we don't need to read content of encoder and decoder.
        // An endpoint MUST allow its peer to create an encoder stream and a
        // decoder stream even if the connection's settings prevent their use.

        while (_isClosed == 0)
        {
            var result = await Input.ReadAsync();
            var readableBuffer = result.Buffer;
            Input.AdvanceTo(readableBuffer.End);
        }
    }

    private ValueTask ProcessHttp3ControlStream(in ReadOnlySequence<byte> payload)
    {
        switch (_incomingFrame.Type)
        {
            case Http3FrameType.Data:
            case Http3FrameType.Headers:
            case Http3FrameType.PushPromise:
                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-7.2
                throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ErrorUnsupportedFrameOnControlStream(_incomingFrame.FormattedType), Http3ErrorCode.UnexpectedFrame, ConnectionEndReason.UnexpectedFrame);
            case Http3FrameType.Settings:
                return ProcessSettingsFrameAsync(payload);
            case Http3FrameType.GoAway:
                return ProcessGoAwayFrameAsync();
            case Http3FrameType.CancelPush:
                return ProcessCancelPushFrameAsync();
            case Http3FrameType.MaxPushId:
                return ProcessMaxPushIdFrameAsync();
            default:
                return ProcessUnknownFrameAsync(_incomingFrame.Type);
        }
    }

    private ValueTask ProcessSettingsFrameAsync(ReadOnlySequence<byte> payload)
    {
        if (_haveReceivedSettingsFrame)
        {
            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#name-settings
            throw new Http3ConnectionErrorException(CoreStrings.Http3ErrorControlStreamMultipleSettingsFrames, Http3ErrorCode.UnexpectedFrame, ConnectionEndReason.UnexpectedFrame);
        }

        _haveReceivedSettingsFrame = true;
        _streamClosedFeature.OnClosed(static state =>
        {
            var stream = (Http3ControlStream)state!;
            stream.OnStreamClosed();
        }, this);

        while (true)
        {
            var id = VariableLengthIntegerHelper.GetInteger(payload, out var consumed, out _);
            if (id == -1)
            {
                break;
            }

            payload = payload.Slice(consumed);

            var value = VariableLengthIntegerHelper.GetInteger(payload, out consumed, out _);
            if (value == -1)
            {
                break;
            }

            payload = payload.Slice(consumed);
            ProcessSetting(id, value);
        }

        return default;
    }

    private void ProcessSetting(long id, long value)
    {
        // These are client settings, for outbound traffic.
        switch (id)
        {
            case 0x0:
            case 0x2:
            case 0x3:
            case 0x4:
            case 0x5:
                // HTTP/2 settings are reserved.
                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-7.2.4.1-5
                var message = CoreStrings.FormatHttp3ErrorControlStreamReservedSetting("0x" + id.ToString("X", CultureInfo.InvariantCulture));
                throw new Http3ConnectionErrorException(message, Http3ErrorCode.SettingsError, ConnectionEndReason.InvalidSettings);
            case (long)Http3SettingType.QPackMaxTableCapacity:
            case (long)Http3SettingType.MaxFieldSectionSize:
            case (long)Http3SettingType.QPackBlockedStreams:
            case (long)Http3SettingType.EnableWebTransport:
            case (long)Http3SettingType.H3Datagram:
                _context.StreamLifetimeHandler.OnInboundControlStreamSetting((Http3SettingType)id, value);
                break;
            default:
                // Ignore all unknown settings.
                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-7.2.4
                break;
        }
    }

    private ValueTask ProcessGoAwayFrameAsync()
    {
        EnsureSettingsFrame(Http3FrameType.GoAway);

        // StopProcessingNextRequest must be called before RequestClose to ensure it's considered client initiated.
        _context.Connection.StopProcessingNextRequest(serverInitiated: false, ConnectionEndReason.ClientGoAway);
        _context.ConnectionContext.Features.Get<IConnectionLifetimeNotificationFeature>()?.RequestClose();

        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#name-goaway
        // PUSH is not implemented so nothing to do.

        // TODO: Double check the connection remains open.
        return default;
    }

    private ValueTask ProcessCancelPushFrameAsync()
    {
        EnsureSettingsFrame(Http3FrameType.CancelPush);

        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#name-cancel_push
        // PUSH is not implemented so nothing to do.

        return default;
    }

    private ValueTask ProcessMaxPushIdFrameAsync()
    {
        EnsureSettingsFrame(Http3FrameType.MaxPushId);

        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#name-cancel_push
        // PUSH is not implemented so nothing to do.

        return default;
    }

    private ValueTask ProcessUnknownFrameAsync(Http3FrameType frameType)
    {
        EnsureSettingsFrame(frameType);

        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-9
        // Unknown frames must be explicitly ignored.
        return default;
    }

    private void EnsureSettingsFrame(Http3FrameType frameType)
    {
        if (!_haveReceivedSettingsFrame)
        {
            var message = CoreStrings.FormatHttp3ErrorControlStreamFrameReceivedBeforeSettings(Http3Formatting.ToFormattedType(frameType));
            throw new Http3ConnectionErrorException(message, Http3ErrorCode.MissingSettings, ConnectionEndReason.InvalidSettings);
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
