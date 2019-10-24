// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3ControlStream : IHttp3Stream
    {
        private Http3FrameWriter _frameWriter;
        private readonly Http3Connection _http3Connection;
        private HttpConnectionContext _context;
        private readonly Http3Frame _incomingFrame = new Http3Frame();
        private int _isClosed;
        private int _gracefulCloseInitiator;

        private bool _haveReceivedSettingsFrame;

        public Http3ControlStream(Http3Connection http3Connection, HttpConnectionContext context)
        {
            var httpLimits = context.ServiceContext.ServerOptions.Limits;

            _http3Connection = http3Connection;
            _context = context;

            // Todo framewriter.
            _frameWriter = new Http3FrameWriter(
                context.Transport.Output,
                context.ConnectionContext,
                this,
                context.TimeoutControl,
                httpLimits.MinResponseDataRate,
                context.ConnectionId,
                context.MemoryPool,
                context.ServiceContext.Log);
            var closedRegistration = _context.ConnectionContext.ConnectionClosed.Register(state => ((Http3ControlStream)state).OnStreamClosed(), this);
        }

        private void OnStreamClosed()
        {
            // TODO how to pass Abort in.
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

            // TODO make this actually close the Http3Stream by telling msquic to close the stream.
            return false;
        }

        private async Task HandleEncodingTask()
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

        private async Task HandleDecodingTask()
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

        private async Task SendStreamIdAsync(int id)
        {
            await _frameWriter.WriteStreamIdAsync(id);
        }

        private async Task SendSettingsFrameAsync()
        {
            await _frameWriter.WriteSettingsAsync(null);
        }

        private async Task<Http3ControlStream> CreateNewControlStreamAsync()
        {
            var connectionContext = await _http3Connection.Context.ConnectionFeatures.Get<IQuicCreateStreamFeature>().StartUnidirectionalStreamAsync();
            var httpConnectionContext = new HttpConnectionContext
            {
                ConnectionId = connectionContext.ConnectionId,
                ConnectionContext = connectionContext,
                Protocols = _context.Protocols,
                ServiceContext = _context.ServiceContext,
                ConnectionFeatures = connectionContext.Features,
                MemoryPool = _context.MemoryPool,
                Transport = connectionContext.Transport,
                TimeoutControl = _context.TimeoutControl,
                LocalEndPoint = connectionContext.LocalEndPoint as IPEndPoint,
                RemoteEndPoint = connectionContext.RemoteEndPoint as IPEndPoint
            };

            // TODO think about whether we need a new stream or not here.
            return new Http3ControlStream(_http3Connection, httpConnectionContext);
        }

        private async Task<long> TryReadStreamIdAsync()
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
                        var id = VariableIntHelper.GetVariableIntFromReadOnlySequence(readableBuffer, out consumed, out examined);
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
                // Error dawg
                return;
            }

            // idk what to do here.
            if (streamType == 0)
            {
                // This control stream may need to be available to everyone.
                // GOAWAY is sent on it.
                if (_http3Connection.SettingsStream != null)
                {
                    throw new Http3ConnectionException("HTTP_STREAM_CREATION_ERROR");
                }

                var stream = await CreateNewControlStreamAsync();
                _http3Connection.SettingsStream = stream;
                await stream.SendStreamIdAsync(id: 0);
                await stream.SendSettingsFrameAsync();
                await HandleControlStream();
                // loop here.
            }
            else if (streamType == 2)
            {
                if (_http3Connection.EncoderStream != null)
                {
                    throw new Http3ConnectionException("HTTP_STREAM_CREATION_ERROR");
                }
                var stream = await CreateNewControlStreamAsync();
                _http3Connection.EncoderStream = stream;

                await stream.SendStreamIdAsync(id: 2);
                await HandleEncodingTask();
                return;
                // encode qpack
            }
            else if (streamType == 3)
            {
                // decode qpack
                if (_http3Connection.DecoderStream != null)
                {
                    throw new Http3ConnectionException("HTTP_STREAM_CREATION_ERROR");
                }
                var stream = await CreateNewControlStreamAsync();
                _http3Connection.DecoderStream = stream;
                await stream.SendStreamIdAsync(id: 3);
                await HandleDecodingTask();
            }
            else
            {
                // Abort the stream.
                // returning should be fine?
                // Need to figure out what to call on msquic.
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
                        while (Http3FrameReader.ReadFrame(ref readableBuffer, _incomingFrame, 16 * 1024, out var framePayload))
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
                    //Log.Http2StreamError(ConnectionId, ex);
                    //// The client doesn't know this error is coming, allow draining additional frames for now.
                    //AbortStream(_incomingFrame.StreamId, new IOException(ex.Message, ex));
                    //await _frameWriter.WriteRstStreamAsync(ex.StreamId, ex.ErrorCode);
                }
                finally
                {
                    Input.AdvanceTo(consumed, examined);

                    //UpdateConnectionState();
                }
            }
        }

        private Task ProcessHttp3ControlStream(in ReadOnlySequence<byte> payload)
        {
            // Two things:
            // settings must be sent as the first frame of each control stream by each peer
            // Can't send more than two settings frames.
            switch (_incomingFrame.Type)
            {
                case Http3FrameType.DATA:
                case Http3FrameType.HEADERS:
                case Http3FrameType.DUPLICATE_PUSH:
                case Http3FrameType.PUSH_PROMISE:
                    throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
                case Http3FrameType.SETTINGS:
                    return ProcessSettingsFrameAsync(payload);
                case Http3FrameType.GOAWAY:
                    return ProcessGoAwayFrameAsync();
                case Http3FrameType.CANCEL_PUSH:
                    return ProcessCancelPushFrameAsync();
                case Http3FrameType.MAX_PUSH_ID:
                    return ProcessMaxPushIdFrameAsync();
                default:
                    return ProcessUnknownFrameAsync();
            }
        }

        private Task ProcessSettingsFrameAsync(ReadOnlySequence<byte> payload)
        {
            if (_haveReceivedSettingsFrame)
            {
                throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
            }
            _haveReceivedSettingsFrame = true;
            // process a bunch of kvp of settings
            while (true)
            {
                var id = VariableIntHelper.GetVariableIntFromReadOnlySequence(payload, out var consumed, out var examinded);
                if (id == -1)
                {
                    break;
                }
                payload = payload.Slice(consumed);

                var value = VariableIntHelper.GetVariableIntFromReadOnlySequence(payload, out consumed, out examinded);
                if (id == -1)
                {
                    break;
                }
                payload = payload.Slice(consumed);
                ProcessSetting(id, value);
            }

            return Task.CompletedTask;
        }

        private void ProcessSetting(long id, long value)
        {
            switch (id)
            {
                case (long)Http3SettingType.QPACK_MAX_TABLE_CAPACITY:
                    _http3Connection.ApplyMaxTableCapacity(value);
                    break;
                case (long)Http3SettingType.MAX_HEADER_LIST_SIZE:
                    _http3Connection.ApplyMaxHeaderListSize(value);
                    break;
                case (long)Http3SettingType.QPACK_BLOCKED_STREAMS:
                    _http3Connection.ApplyBlockedStream(value);
                    break;
                default:
                    // Ignore all unknown settings.
                    break;
            }
        }

        private Task ProcessGoAwayFrameAsync()
        {
            if (!_haveReceivedSettingsFrame)
            {
                throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
            }
            return Task.CompletedTask;
        }

        private Task ProcessCancelPushFrameAsync()
        {
            if (!_haveReceivedSettingsFrame)
            {
                throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
            }
            // This should just noop.
            return Task.CompletedTask;
        }

        private Task ProcessMaxPushIdFrameAsync()
        {
            if (!_haveReceivedSettingsFrame)
            {
                throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
            }
            return Task.CompletedTask;
        }

        private Task ProcessUnknownFrameAsync()
        {
            if (!_haveReceivedSettingsFrame)
            {
                throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
            }
            return Task.CompletedTask;
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

        public void Tick(DateTimeOffset now)
        {
        }

        // TODO need a way to receive abort from MSQUIC

        public void AbortStream(ConnectionAbortedException ex)
        {
            Abort(ex);
        }

        private static class GracefulCloseInitiator
        {
            public const int None = 0;
            public const int Server = 1;
            public const int Client = 2;
        }
    }
}
