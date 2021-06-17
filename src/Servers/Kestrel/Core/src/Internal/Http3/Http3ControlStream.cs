// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal abstract class Http3ControlStream : IHttp3Stream, IThreadPoolWorkItem
    {
        private const int ControlStreamTypeId = 0;
        private const int EncoderStreamTypeId = 2;
        private const int DecoderStreamTypeId = 3;

        private readonly Http3FrameWriter _frameWriter;
        private readonly Http3StreamContext _context;
        private readonly Http3PeerSettings _serverPeerSettings;
        private readonly IStreamIdFeature _streamIdFeature;
        private readonly IProtocolErrorCodeFeature _errorCodeFeature;
        private readonly Http3RawFrame _incomingFrame = new Http3RawFrame();
        private volatile int _isClosed;
        private int _gracefulCloseInitiator;
        private long _headerType;

        private bool _haveReceivedSettingsFrame;

        public long StreamId => _streamIdFeature.StreamId;

        public Http3ControlStream(Http3StreamContext context)
        {
            var httpLimits = context.ServiceContext.ServerOptions.Limits;
            _context = context;
            _serverPeerSettings = context.ServerPeerSettings;
            _streamIdFeature = context.ConnectionFeatures.Get<IStreamIdFeature>()!;
            _errorCodeFeature = context.ConnectionFeatures.Get<IProtocolErrorCodeFeature>()!;
            _headerType = -1;

            _frameWriter = new Http3FrameWriter(
                context.Transport.Output,
                context.StreamContext,
                context.TimeoutControl,
                httpLimits.MinResponseDataRate,
                context.ConnectionId,
                context.MemoryPool,
                context.ServiceContext.Log,
                _streamIdFeature,
                context.ClientPeerSettings,
                this);
        }

        private void OnStreamClosed()
        {
            Abort(new ConnectionAbortedException("HTTP_CLOSED_CRITICAL_STREAM"), Http3ErrorCode.InternalError);
        }

        public PipeReader Input => _context.Transport.Input;
        public IKestrelTrace Log => _context.ServiceContext.Log;

        public long HeaderTimeoutTicks { get; set; }
        public bool ReceivedHeader => _headerType >= 0;

        public bool IsRequestStream => false;

        public void Abort(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
        {
            // TODO - Should there be a check here to track abort state to avoid
            // running twice for a request?

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

        internal ValueTask<FlushResult> SendGoAway(long id)
        {
            return _frameWriter.WriteGoAway(id);
        }

        internal async ValueTask SendSettingsFrameAsync()
        {
            await _frameWriter.WriteSettingsAsync(_serverPeerSettings.GetNonProtocolDefaults());
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
                _headerType = await TryReadStreamHeaderAsync();
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
                if (id == -1)
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
}
