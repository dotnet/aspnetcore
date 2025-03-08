// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
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

    // Arbitrarily chosen max frame length
    // ControlStream frames currently are very small, either a single variable length integer (max 8 bytes), two variable length integers,
    // or in the case of SETTINGS a small collection of two variable length integers
    // We'll use a generous value of 10k in case new optional frame(s) are added that might be a little larger than the current frames.
    private const int MaxFrameSize = 10_000;

    private readonly Http3FrameWriter _frameWriter;
    private readonly Http3StreamContext _context;
    private readonly Http3PeerSettings _serverPeerSettings;
    private readonly IStreamIdFeature _streamIdFeature;
    private readonly IStreamClosedFeature _streamClosedFeature;
    private readonly IProtocolErrorCodeFeature _errorCodeFeature;
    private volatile int _isClosed;
    private long _headerType;
    private readonly object _completionLock = new();

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
                    if (VariableLengthIntegerHelper.TryGetInteger(readableBuffer, out consumed, out var id))
                    {
                        examined = consumed;
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
                        throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ControlStreamErrorMultipleInboundStreams("control"), Http3ErrorCode.StreamCreationError);
                    }

                    await HandleControlStream();
                    break;
                case EncoderStreamTypeId:
                    if (!_context.StreamLifetimeHandler.OnInboundEncoderStream(this))
                    {
                        // https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#section-4.2
                        throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ControlStreamErrorMultipleInboundStreams("encoder"), Http3ErrorCode.StreamCreationError);
                    }

                    await HandleEncodingDecodingTask();
                    break;
                case DecoderStreamTypeId:
                    if (!_context.StreamLifetimeHandler.OnInboundDecoderStream(this))
                    {
                        // https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#section-4.2
                        throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ControlStreamErrorMultipleInboundStreams("decoder"), Http3ErrorCode.StreamCreationError);
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
            await _context.StreamContext.DisposeAsync();

            ApplyCompletionFlag(StreamCompletionFlags.Completed);
            _context.StreamLifetimeHandler.OnStreamCompleted(this);
        }
    }

    private async Task HandleControlStream()
    {
        var incomingFrame = new Http3RawFrame();
        var isContinuedFrame = false;
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
                    while (Http3FrameReader.TryReadFrame(ref readableBuffer, incomingFrame, isContinuedFrame, out var framePayload))
                    {
                        Debug.Assert(incomingFrame.RemainingLength >= framePayload.Length);

                        // Only log when parsing the beginning of the frame
                        if (!isContinuedFrame)
                        {
                            Log.Http3FrameReceived(_context.ConnectionId, _streamIdFeature.StreamId, incomingFrame);
                        }

                        examined = framePayload.End;
                        await ProcessHttp3ControlStream(incomingFrame, isContinuedFrame, framePayload, out consumed);

                        if (incomingFrame.RemainingLength == framePayload.Length)
                        {
                            Debug.Assert(framePayload.Slice(0, consumed).Length == framePayload.Length);

                            incomingFrame.RemainingLength = 0;
                            isContinuedFrame = false;
                        }
                        else
                        {
                            incomingFrame.RemainingLength -= framePayload.Slice(0, consumed).Length;
                            isContinuedFrame = true;

                            Debug.Assert(incomingFrame.RemainingLength > 0);
                        }
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

    private ValueTask ProcessHttp3ControlStream(Http3RawFrame incomingFrame, bool isContinuedFrame, in ReadOnlySequence<byte> payload, out SequencePosition consumed)
    {
        // default to consuming the entire payload, this is so that we don't need to set consumed from all the frame types that aren't implemented yet.
        // individual frame types can set consumed if they're implemented and want to be able to partially consume the payload.
        consumed = payload.End;
        switch (incomingFrame.Type)
        {
            case Http3FrameType.Data:
            case Http3FrameType.Headers:
            case Http3FrameType.PushPromise:
                // https://www.rfc-editor.org/rfc/rfc9114.html#section-8.1-2.12.1
                throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ErrorUnsupportedFrameOnControlStream(incomingFrame.FormattedType), Http3ErrorCode.UnexpectedFrame);
            case Http3FrameType.Settings:
                CheckMaxFrameSize(incomingFrame);
                return ProcessSettingsFrameAsync(isContinuedFrame, payload, out consumed);
            case Http3FrameType.GoAway:
                return ProcessGoAwayFrameAsync(isContinuedFrame, incomingFrame, payload, out consumed);
            case Http3FrameType.CancelPush:
                return ProcessCancelPushFrameAsync(incomingFrame, payload, out consumed);
            case Http3FrameType.MaxPushId:
                return ProcessMaxPushIdFrameAsync(incomingFrame, payload, out consumed);
            default:
                CheckMaxFrameSize(incomingFrame);
                return ProcessUnknownFrameAsync(incomingFrame.Type);
        }

        static void CheckMaxFrameSize(Http3RawFrame http3RawFrame)
        {
            // Not part of the RFC, but it's a good idea to limit the size of frames when we know they're supposed to be small.
            if (http3RawFrame.RemainingLength >= MaxFrameSize)
            {
                throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ControlStreamFrameTooLarge(http3RawFrame.FormattedType), Http3ErrorCode.FrameError);
            }
        }
    }

    private ValueTask ProcessSettingsFrameAsync(bool isContinuedFrame, ReadOnlySequence<byte> payload, out SequencePosition consumed)
    {
        if (!isContinuedFrame)
        {
            if (_haveReceivedSettingsFrame)
            {
                // https://www.rfc-editor.org/rfc/rfc9114.html#section-7.2.4
                throw new Http3ConnectionErrorException(CoreStrings.Http3ErrorControlStreamMultipleSettingsFrames, Http3ErrorCode.UnexpectedFrame);
            }

            _haveReceivedSettingsFrame = true;
            _streamClosedFeature.OnClosed(static state =>
            {
                var stream = (Http3ControlStream)state!;
                stream.OnStreamClosed();
            }, this);
        }

        while (true)
        {
            if (!VariableLengthIntegerHelper.TryGetInteger(payload, out consumed, out var id))
            {
                break;
            }

            if (!VariableLengthIntegerHelper.TryGetInteger(payload.Slice(consumed), out consumed, out var value))
            {
                // Reset consumed to very start even though we successfully read 1 varint. It's because we want to keep the id for when we have the value as well.
                consumed = payload.Start;
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
                throw new Http3ConnectionErrorException(message, Http3ErrorCode.SettingsError);
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

    private ValueTask ProcessGoAwayFrameAsync(bool isContinuedFrame, Http3RawFrame incomingFrame, ReadOnlySequence<byte> payload, out SequencePosition consumed)
    {
        // https://www.rfc-editor.org/rfc/rfc9114.html#name-goaway

        // We've already triggered RequestClose since isContinuedFrame is only true
        // after we've already parsed the frame type and called the processing function at least once.
        if (!isContinuedFrame)
        {
            EnsureSettingsFrame(Http3FrameType.GoAway);

            // StopProcessingNextRequest must be called before RequestClose to ensure it's considered client initiated.
            _context.Connection.StopProcessingNextRequest(serverInitiated: false);
            _context.ConnectionContext.Features.Get<IConnectionLifetimeNotificationFeature>()?.RequestClose();
        }

        // PUSH is not implemented but we still want to parse the frame to do error checking
        ParseVarIntWithFrameLengthValidation(incomingFrame, payload, out consumed);

        // TODO: Double check the connection remains open.
        return default;
    }

    private ValueTask ProcessCancelPushFrameAsync(Http3RawFrame incomingFrame, ReadOnlySequence<byte> payload, out SequencePosition consumed)
    {
        // https://www.rfc-editor.org/rfc/rfc9114.html#section-7.2.3

        EnsureSettingsFrame(Http3FrameType.CancelPush);

        // PUSH is not implemented but we still want to parse the frame to do error checking
        ParseVarIntWithFrameLengthValidation(incomingFrame, payload, out consumed);

        return default;
    }

    private ValueTask ProcessMaxPushIdFrameAsync(Http3RawFrame incomingFrame, ReadOnlySequence<byte> payload, out SequencePosition consumed)
    {
        // https://www.rfc-editor.org/rfc/rfc9114.html#section-7.2.7

        EnsureSettingsFrame(Http3FrameType.MaxPushId);

        // PUSH is not implemented but we still want to parse the frame to do error checking
        ParseVarIntWithFrameLengthValidation(incomingFrame, payload, out consumed);

        return default;
    }

    private ValueTask ProcessUnknownFrameAsync(Http3FrameType frameType)
    {
        EnsureSettingsFrame(frameType);

        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-9
        // Unknown frames must be explicitly ignored.
        return default;
    }

    // Used for frame types that aren't (fully) implemented yet and contain a single var int as part of their framing. (CancelPush, MaxPushId, GoAway)
    // We want to throw an error if the length field of the frame is larger than the spec defined format of the frame.
    private static void ParseVarIntWithFrameLengthValidation(Http3RawFrame incomingFrame, ReadOnlySequence<byte> payload, out SequencePosition consumed)
    {
        if (!VariableLengthIntegerHelper.TryGetInteger(payload, out consumed, out _))
        {
            return;
        }

        if (incomingFrame.RemainingLength > payload.Slice(0, consumed).Length)
        {
            // https://www.rfc-editor.org/rfc/rfc9114.html#section-10.8
            // An implementation MUST ensure that the length of a frame exactly matches the length of the fields it contains.
            throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ControlStreamFrameTooLarge(Http3Formatting.ToFormattedType(incomingFrame.Type)), Http3ErrorCode.FrameError);
        }
    }

    private void EnsureSettingsFrame(Http3FrameType frameType)
    {
        if (!_haveReceivedSettingsFrame)
        {
            var message = CoreStrings.FormatHttp3ErrorControlStreamFrameReceivedBeforeSettings(Http3Formatting.ToFormattedType(frameType));
            throw new Http3ConnectionErrorException(message, Http3ErrorCode.MissingSettings);
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
