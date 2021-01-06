// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
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
    internal abstract class Http3ControlStream : IThreadPoolWorkItem
    {
        private const int ControlStream = 0;
        private const int EncoderStream = 2;
        private const int DecoderStream = 3;

        private readonly Http3FrameWriter _frameWriter;
        private readonly Http3Connection _http3Connection;
        private readonly Http3StreamContext _context;
        private readonly Http3PeerSettings _serverPeerSettings;
        private readonly IStreamIdFeature _streamIdFeature;
        private readonly Http3RawFrame _incomingFrame = new Http3RawFrame();
        private volatile int _isClosed;
        private int _gracefulCloseInitiator;

        private bool _haveReceivedSettingsFrame;

        public Http3ControlStream(Http3Connection http3Connection, Http3StreamContext context)
        {
            var httpLimits = context.ServiceContext.ServerOptions.Limits;
            _http3Connection = http3Connection;
            _context = context;
            _serverPeerSettings = context.ServerSettings;
            _streamIdFeature = context.ConnectionFeatures.Get<IStreamIdFeature>()!;

            _frameWriter = new Http3FrameWriter(
                context.Transport.Output,
                context.StreamContext,
                context.TimeoutControl,
                httpLimits.MinResponseDataRate,
                context.ConnectionId,
                context.MemoryPool,
                context.ServiceContext.Log,
                _streamIdFeature);
        }

        private void OnStreamClosed()
        {
            Abort(new ConnectionAbortedException("HTTP_CLOSED_CRITICAL_STREAM"));
        }

        public PipeReader Input => _context.Transport.Input;
        public IKestrelTrace Log => _context.ServiceContext.Log;

        public void Abort(ConnectionAbortedException ex)
        {

        }

        public void HandleReadDataRateTimeout()
        {
            //Log.RequestBodyMinimumDataRateNotSatisfied(ConnectionId, null, Limits.MinRequestBodyDataRate.BytesPerSecond);
            Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestBodyTimeout));
        }

        public void HandleRequestHeadersTimeout()
        {
            //Log.ConnectionBadRequest(ConnectionId, KestrelBadHttpRequestException.GetException(RequestRejectionReason.RequestHeadersTimeout));
            Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestHeadersTimeout));
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

        private async ValueTask<long> TryReadStreamIdAsync()
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
            var streamType = await TryReadStreamIdAsync();

            if (streamType == -1)
            {
                return;
            }

            if (streamType == ControlStream)
            {
                if (!_http3Connection.SetInboundControlStream(this))
                {
                    // TODO propagate these errors to connection.
                    throw new Http3ConnectionException("HTTP_STREAM_CREATION_ERROR");
                }

                await HandleControlStream();
            }
            else if (streamType == EncoderStream)
            {
                if (!_http3Connection.SetInboundEncoderStream(this))
                {
                    throw new Http3ConnectionException("HTTP_STREAM_CREATION_ERROR");
                }

                await HandleEncodingDecodingTask();
            }
            else if (streamType == DecoderStream)
            {
                if (!_http3Connection.SetInboundDecoderStream(this))
                {
                    throw new Http3ConnectionException("HTTP_STREAM_CREATION_ERROR");
                }
                await HandleEncodingDecodingTask();
            }
            else
            {
                // TODO Close the control stream as it's unexpected.
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
                catch (Http3StreamErrorException)
                {
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
            // Two things:
            // settings must be sent as the first frame of each control stream by each peer
            // Can't send more than two settings frames.
            switch (_incomingFrame.Type)
            {
                case Http3FrameType.Data:
                case Http3FrameType.Headers:
                case Http3FrameType.DuplicatePush:
                case Http3FrameType.PushPromise:
                    throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
                case Http3FrameType.Settings:
                    return ProcessSettingsFrameAsync(payload);
                case Http3FrameType.GoAway:
                    return ProcessGoAwayFrameAsync(payload);
                case Http3FrameType.CancelPush:
                    return ProcessCancelPushFrameAsync();
                case Http3FrameType.MaxPushId:
                    return ProcessMaxPushIdFrameAsync();
                default:
                    return ProcessUnknownFrameAsync();
            }
        }

        private ValueTask ProcessSettingsFrameAsync(ReadOnlySequence<byte> payload)
        {
            if (_haveReceivedSettingsFrame)
            {
                throw new Http3ConnectionException("H3_SETTINGS_ERROR");
            }

            _haveReceivedSettingsFrame = true;
            using var closedRegistration = _context.StreamContext.ConnectionClosed.Register(state => ((Http3ControlStream)state!).OnStreamClosed(), this);

            while (true)
            {
                var id = VariableLengthIntegerHelper.GetInteger(payload, out var consumed, out var examinded);
                if (id == -1)
                {
                    break;
                }

                payload = payload.Slice(consumed);

                var value = VariableLengthIntegerHelper.GetInteger(payload, out consumed, out examinded);
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
                case (long)Http3SettingType.QPackMaxTableCapacity:
                    _http3Connection.ApplyMaxTableCapacity(value);
                    break;
                case (long)Http3SettingType.MaxHeaderListSize:
                    _http3Connection.ApplyMaxHeaderListSize(value);
                    break;
                case (long)Http3SettingType.QPackBlockedStreams:
                    _http3Connection.ApplyBlockedStream(value);
                    break;
                default:
                    // Ignore all unknown settings.
                    break;
            }
        }

        private ValueTask ProcessGoAwayFrameAsync(ReadOnlySequence<byte> payload)
        {
             throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
        }

        private ValueTask ProcessCancelPushFrameAsync()
        {
            if (!_haveReceivedSettingsFrame)
            {
                throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
            }

            return default;
        }

        private ValueTask ProcessMaxPushIdFrameAsync()
        {
            if (!_haveReceivedSettingsFrame)
            {
                throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
            }

            return default;
        }

        private ValueTask ProcessUnknownFrameAsync()
        {
            if (!_haveReceivedSettingsFrame)
            {
                throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
            }

            return default;
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
