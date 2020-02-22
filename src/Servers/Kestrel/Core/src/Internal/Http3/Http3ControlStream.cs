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
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal abstract class Http3ControlStream : IThreadPoolWorkItem
    {
        private const int ControlStream = 0;
        private const int EncoderStream = 2;
        private const int DecoderStream = 3;

        private Http3FrameWriter _frameWriter;
        private readonly Http3Connection _http3Connection;
        private HttpConnectionContext _context;
        private readonly Http3RawFrame _incomingFrame = new Http3RawFrame();
        private volatile int _isClosed;
        private int _gracefulCloseInitiator;

        private bool _haveReceivedSettingsFrame;

        public Http3ControlStream(Http3Connection http3Connection, HttpConnectionContext context)
        {
            var httpLimits = context.ServiceContext.ServerOptions.Limits;

            _http3Connection = http3Connection;
            _context = context;

            _frameWriter = new Http3FrameWriter(
                context.Transport.Output,
                context.ConnectionContext,
                context.TimeoutControl,
                httpLimits.MinResponseDataRate,
                context.ConnectionId,
                context.MemoryPool,
                context.ServiceContext.Log);
        }

        private void OnStreamClosed()
        {
            Abort(new ConnectionAbortedException("HTTP_CLOSED_CRITICAL_STREAM"));
        }

        public PipeReader Input => _context.Transport.Input;


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
            //Log.ConnectionBadRequest(ConnectionId, BadHttpRequestException.GetException(RequestRejectionReason.RequestHeadersTimeout));
            Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestHeadersTimeout));
        }

        public void OnInputOrOutputCompleted()
        {
            TryClose();
            _frameWriter.Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient));
        }

        private bool TryClose()
        {
            if (Interlocked.Exchange(ref _isClosed, 1) == 0)
            {
                return true;
            }

            // TODO make this actually close the Http3Stream by telling quic to close the stream.
            return false;
        }

        private async ValueTask HandleEncodingTask()
        {
            var encoder = new EncoderStreamReader(10000); // TODO get value from limits
            while (_isClosed == 0)
            {
                var result = await Input.ReadAsync();
                var readableBuffer = result.Buffer;
                if (!readableBuffer.IsEmpty)
                {
                    // This should always read all bytes in the input no matter what.
                    encoder.Read(readableBuffer);
                }
                Input.AdvanceTo(readableBuffer.End);
            }
        }

        private async ValueTask HandleDecodingTask()
        {
            var decoder = new DecoderStreamReader();
            while (_isClosed == 0)
            {
                var result = await Input.ReadAsync();
                var readableBuffer = result.Buffer;
                var consumed = readableBuffer.Start;
                var examined = readableBuffer.Start;
                if (!readableBuffer.IsEmpty)
                {
                    decoder.Read(readableBuffer);
                }
                Input.AdvanceTo(readableBuffer.End);
            }
        }

        internal async ValueTask SendStreamIdAsync(long id)
        {
            await _frameWriter.WriteStreamIdAsync(id);
        }

        internal async ValueTask SendGoAway(long id)
        {
            await _frameWriter.WriteGoAway(id);
        }

        internal async ValueTask SendSettingsFrameAsync()
        {
            await _frameWriter.WriteSettingsAsync(null);
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

        public async Task ProcessRequestAsync<TContext>(IHttpApplication<TContext> application)
        {
            var streamType = await TryReadStreamIdAsync();

            if (streamType == -1)
            {
                return;
            }

            if (streamType == ControlStream)
            {
                if (_http3Connection.ControlStream != null)
                {
                    throw new Http3ConnectionException("HTTP_STREAM_CREATION_ERROR");
                }

                await HandleControlStream();
            }
            else if (streamType == EncoderStream)
            {
                if (_http3Connection.EncoderStream != null)
                {
                    throw new Http3ConnectionException("HTTP_STREAM_CREATION_ERROR");
                }
                await HandleEncodingTask();
                return;
            }
            else if (streamType == DecoderStream)
            {
                if (_http3Connection.DecoderStream != null)
                {
                    throw new Http3ConnectionException("HTTP_STREAM_CREATION_ERROR");
                }
                await HandleDecodingTask();
            }
            else
            {
                // TODO Close the control stream as it's unexpected.
            }
            return;
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
                        while (Http3FrameReader.TryReadFrame(ref readableBuffer, _incomingFrame, 16 * 1024, out var framePayload))
                        {
                            //Log.Http2FrameReceived(ConnectionId, _incomingFrame);
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
            using var closedRegistration = _context.ConnectionContext.ConnectionClosed.Register(state => ((Http3ControlStream)state).OnStreamClosed(), this);

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
