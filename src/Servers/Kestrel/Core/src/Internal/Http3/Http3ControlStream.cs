// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal abstract class Http3ControlStream : Http3UnidirectionalStream
{
    private readonly Http3PeerSettings _serverPeerSettings;
    private bool _haveReceivedSettingsFrame;

    public Http3ControlStream(Http3StreamContext context) : base(context)
    {
        _serverPeerSettings = context.ServerPeerSettings;
    }

    internal ValueTask<FlushResult> SendGoAway(long id)
    {
        Log.Http3GoAwayStreamId(_context.ConnectionId, id);
        return _frameWriter.WriteGoAway(id);
    }

    internal async ValueTask SendSettingsFrameAsync()
    {
        await _frameWriter.WriteSettingsAsync(_serverPeerSettings.GetNonProtocolDefaults());
    }

    public override async Task ProcessRequestAsync<TContext>(IHttpApplication<TContext> application)
    {
        try
        {
            _headerType = await TryReadStreamHeaderAsync();
            _context.StreamLifetimeHandler.OnStreamHeaderReceived(this);

            switch (_headerType)
            {
                case (long)Http3StreamType.Control:
                    if (!_context.StreamLifetimeHandler.OnInboundControlStream(this))
                    {
                        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-6.2.1
                        throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ControlStreamErrorMultipleInboundStreams("control"), Http3ErrorCode.StreamCreationError);
                    }

                    await HandleControlStream();
                    break;
                case (long)Http3StreamType.Encoder:
                    if (!_context.StreamLifetimeHandler.OnInboundEncoderStream(this))
                    {
                        // https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#section-4.2
                        throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ControlStreamErrorMultipleInboundStreams("encoder"), Http3ErrorCode.StreamCreationError);
                    }

                    await HandleEncodingDecodingTask();
                    break;
                case (long)Http3StreamType.Decoder:
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
                throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3ErrorUnsupportedFrameOnControlStream(_incomingFrame.FormattedType), Http3ErrorCode.UnexpectedFrame);
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
            throw new Http3ConnectionErrorException(CoreStrings.Http3ErrorControlStreamMultipleSettingsFrames, Http3ErrorCode.UnexpectedFrame);
        }

        _haveReceivedSettingsFrame = true;
        using var closedRegistration = _context.StreamContext.ConnectionClosed.Register(state => ((Http3ControlStream)state!).OnStreamClosed(), this);

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

    private ValueTask ProcessGoAwayFrameAsync()
    {
        EnsureSettingsFrame(Http3FrameType.GoAway);

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
            throw new Http3ConnectionErrorException(message, Http3ErrorCode.MissingSettings);
        }
    }
}
