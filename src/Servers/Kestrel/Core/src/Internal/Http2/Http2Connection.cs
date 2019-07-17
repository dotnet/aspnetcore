// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal class Http2Connection : IHttp2StreamLifetimeHandler, IHttpHeadersHandler, IRequestProcessor
    {
        public static byte[] ClientPreface { get; } = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private static readonly PseudoHeaderFields _mandatoryRequestPseudoHeaderFields =
            PseudoHeaderFields.Method | PseudoHeaderFields.Path | PseudoHeaderFields.Scheme;

        private static readonly byte[] _authorityBytes = Encoding.ASCII.GetBytes(HeaderNames.Authority);
        private static readonly byte[] _methodBytes = Encoding.ASCII.GetBytes(HeaderNames.Method);
        private static readonly byte[] _pathBytes = Encoding.ASCII.GetBytes(HeaderNames.Path);
        private static readonly byte[] _schemeBytes = Encoding.ASCII.GetBytes(HeaderNames.Scheme);
        private static readonly byte[] _statusBytes = Encoding.ASCII.GetBytes(HeaderNames.Status);
        private static readonly byte[] _connectionBytes = Encoding.ASCII.GetBytes("connection");
        private static readonly byte[] _teBytes = Encoding.ASCII.GetBytes("te");
        private static readonly byte[] _trailersBytes = Encoding.ASCII.GetBytes("trailers");
        private static readonly byte[] _connectBytes = Encoding.ASCII.GetBytes("CONNECT");

        private readonly HttpConnectionContext _context;
        private readonly Http2FrameWriter _frameWriter;
        private readonly HPackDecoder _hpackDecoder;
        private readonly InputFlowControl _inputFlowControl;
        private readonly OutputFlowControl _outputFlowControl = new OutputFlowControl(Http2PeerSettings.DefaultInitialWindowSize);

        private readonly Http2PeerSettings _serverSettings = new Http2PeerSettings();
        private readonly Http2PeerSettings _clientSettings = new Http2PeerSettings();

        private readonly Http2Frame _incomingFrame = new Http2Frame();

        private Http2Stream _currentHeadersStream;
        private RequestHeaderParsingState _requestHeaderParsingState;
        private PseudoHeaderFields _parsedPseudoHeaderFields;
        private Http2HeadersFrameFlags _headerFlags;
        private int _totalParsedHeaderSize;
        private bool _isMethodConnect;
        private int _highestOpenedStreamId;
        private bool _gracefulCloseStarted;

        private readonly Dictionary<int, Http2Stream> _streams = new Dictionary<int, Http2Stream>();
        private int _activeStreamCount = 0;

        // The following are the only fields that can be modified outside of the ProcessRequestsAsync loop.
        private readonly ConcurrentQueue<Http2Stream> _completedStreams = new ConcurrentQueue<Http2Stream>();
        private readonly StreamCloseAwaitable _streamCompletionAwaitable = new StreamCloseAwaitable();
        private int _gracefulCloseInitiator;
        private int _isClosed;

        public Http2Connection(HttpConnectionContext context)
        {
            var httpLimits = context.ServiceContext.ServerOptions.Limits;
            var http2Limits = httpLimits.Http2;

            _context = context;

            _frameWriter = new Http2FrameWriter(
                context.Transport.Output,
                context.ConnectionContext,
                this,
                _outputFlowControl,
                context.TimeoutControl,
                httpLimits.MinResponseDataRate,
                context.ConnectionId,
                context.MemoryPool,
                context.ServiceContext.Log);

            _hpackDecoder = new HPackDecoder(http2Limits.HeaderTableSize, http2Limits.MaxRequestHeaderFieldSize);

            var connectionWindow = (uint)http2Limits.InitialConnectionWindowSize;
            _inputFlowControl = new InputFlowControl(connectionWindow, connectionWindow / 2);

            _serverSettings.MaxConcurrentStreams = (uint)http2Limits.MaxStreamsPerConnection;
            _serverSettings.MaxFrameSize = (uint)http2Limits.MaxFrameSize;
            _serverSettings.HeaderTableSize = (uint)http2Limits.HeaderTableSize;
            _serverSettings.MaxHeaderListSize = (uint)httpLimits.MaxRequestHeadersTotalSize;
            _serverSettings.InitialWindowSize = (uint)http2Limits.InitialStreamWindowSize;
        }

        public string ConnectionId => _context.ConnectionId;
        public PipeReader Input => _context.Transport.Input;
        public IKestrelTrace Log => _context.ServiceContext.Log;
        public IFeatureCollection ConnectionFeatures => _context.ConnectionFeatures;
        public ISystemClock SystemClock => _context.ServiceContext.SystemClock;
        public ITimeoutControl TimeoutControl => _context.TimeoutControl;
        public KestrelServerLimits Limits => _context.ServiceContext.ServerOptions.Limits;

        internal Http2PeerSettings ServerSettings => _serverSettings;

        public void OnInputOrOutputCompleted()
        {
            TryClose();
            _frameWriter.Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient));
        }

        public void Abort(ConnectionAbortedException ex)
        {
            if (TryClose())
            {
                _frameWriter.WriteGoAwayAsync(int.MaxValue, Http2ErrorCode.INTERNAL_ERROR);
            }

            _frameWriter.Abort(ex);
        }

        public void StopProcessingNextRequest()
            => StopProcessingNextRequest(serverInitiated: true);

        public void HandleRequestHeadersTimeout()
        {
            Log.ConnectionBadRequest(ConnectionId, BadHttpRequestException.GetException(RequestRejectionReason.RequestHeadersTimeout));
            Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestHeadersTimeout));
        }

        public void HandleReadDataRateTimeout()
        {
            Log.RequestBodyMinimumDataRateNotSatisfied(ConnectionId, null, Limits.MinRequestBodyDataRate.BytesPerSecond);
            Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestBodyTimeout));
        }

        public void StopProcessingNextRequest(bool serverInitiated)
        {
            var initiator = serverInitiated ? GracefulCloseInitiator.Server : GracefulCloseInitiator.Client;

            if (Interlocked.CompareExchange(ref _gracefulCloseInitiator, initiator, GracefulCloseInitiator.None) == GracefulCloseInitiator.None)
            {
                Input.CancelPendingRead();
            }
        }

        public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application)
        {
            Exception error = null;
            var errorCode = Http2ErrorCode.NO_ERROR;

            try
            {
                ValidateTlsRequirements();

                TimeoutControl.InitializeHttp2(_inputFlowControl);
                TimeoutControl.SetTimeout(Limits.KeepAliveTimeout.Ticks, TimeoutReason.KeepAlive);

                if (!await TryReadPrefaceAsync())
                {
                    return;
                }

                if (_isClosed == 0)
                {
                    await _frameWriter.WriteSettingsAsync(_serverSettings.GetNonProtocolDefaults());
                    // Inform the client that the connection window is larger than the default. It can't be lowered here,
                    // It can only be lowered by not issuing window updates after data is received.
                    var connectionWindow = _context.ServiceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
                    var diff = connectionWindow - (int)Http2PeerSettings.DefaultInitialWindowSize;
                    if (diff > 0)
                    {
                        await _frameWriter.WriteWindowUpdateAsync(0, diff);
                    }
                }

                while (_isClosed == 0)
                {
                    var result = await Input.ReadAsync();
                    var readableBuffer = result.Buffer;
                    var consumed = readableBuffer.Start;
                    var examined = readableBuffer.Start;

                    // Call UpdateCompletedStreams() prior to frame processing in order to remove any streams that have exceded their drain timeouts.
                    UpdateCompletedStreams();

                    try
                    {
                        if (!readableBuffer.IsEmpty)
                        {
                            if (Http2FrameReader.ReadFrame(readableBuffer, _incomingFrame, _serverSettings.MaxFrameSize, out var framePayload))
                            {
                                Log.Http2FrameReceived(ConnectionId, _incomingFrame);
                                consumed = examined = framePayload.End;
                                await ProcessFrameAsync(application, framePayload);
                            }
                            else
                            {
                                examined = readableBuffer.End;
                            }
                        }

                        if (result.IsCompleted)
                        {
                            return;
                        }
                    }
                    catch (Http2StreamErrorException ex)
                    {
                        Log.Http2StreamError(ConnectionId, ex);
                        // The client doesn't know this error is coming, allow draining additional frames for now.
                        AbortStream(_incomingFrame.StreamId, new IOException(ex.Message, ex));
                        await _frameWriter.WriteRstStreamAsync(ex.StreamId, ex.ErrorCode);
                    }
                    finally
                    {
                        Input.AdvanceTo(consumed, examined);

                        UpdateConnectionState();
                    }
                }
            }
            catch (ConnectionResetException ex)
            {
                // Don't log ECONNRESET errors when there are no active streams on the connection. Browsers like IE will reset connections regularly.
                if (_activeStreamCount > 0)
                {
                    Log.RequestProcessingError(ConnectionId, ex);
                }

                error = ex;
            }
            catch (IOException ex)
            {
                Log.RequestProcessingError(ConnectionId, ex);
                error = ex;
            }
            catch (Http2ConnectionErrorException ex)
            {
                Log.Http2ConnectionError(ConnectionId, ex);
                error = ex;
                errorCode = ex.ErrorCode;
            }
            catch (HPackDecodingException ex)
            {
                Log.HPackDecodingError(ConnectionId, _currentHeadersStream.StreamId, ex);
                error = ex;
                errorCode = Http2ErrorCode.COMPRESSION_ERROR;
            }
            catch (Exception ex)
            {
                Log.LogWarning(0, ex, CoreStrings.RequestProcessingEndError);
                error = ex;
                errorCode = Http2ErrorCode.INTERNAL_ERROR;
            }
            finally
            {
                var connectionError = error as ConnectionAbortedException
                    ?? new ConnectionAbortedException(CoreStrings.Http2ConnectionFaulted, error);

                try
                {
                    if (TryClose())
                    {
                        await _frameWriter.WriteGoAwayAsync(_highestOpenedStreamId, errorCode);
                    }

                    // Ensure aborting each stream doesn't result in unnecessary WINDOW_UPDATE frames being sent.
                    _inputFlowControl.StopWindowUpdates();

                    foreach (var stream in _streams.Values)
                    {
                        stream.Abort(new IOException(CoreStrings.Http2StreamAborted, connectionError));
                    }

                    while (_activeStreamCount > 0)
                    {
                        await _streamCompletionAwaitable;
                        UpdateCompletedStreams();
                    }

                    // This cancels keep-alive and request header timeouts, but not the response drain timeout.
                    TimeoutControl.CancelTimeout();
                    TimeoutControl.StartDrainTimeout(Limits.MinResponseDataRate, Limits.MaxResponseBufferSize);

                    _frameWriter.Complete();
                }
                catch
                {
                    _frameWriter.Abort(connectionError);
                    throw;
                }
                finally
                {
                    Input.Complete();
                }
            }
        }

        // https://tools.ietf.org/html/rfc7540#section-9.2
        // Some of these could not be checked in advance. Fail before using the connection.
        private void ValidateTlsRequirements()
        {
            var tlsFeature = ConnectionFeatures.Get<ITlsHandshakeFeature>();
            if (tlsFeature == null)
            {
                // Not using TLS at all.
                return;
            }

            if (tlsFeature.Protocol < SslProtocols.Tls12)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorMinTlsVersion(tlsFeature.Protocol), Http2ErrorCode.INADEQUATE_SECURITY);
            }
        }

        private async Task<bool> TryReadPrefaceAsync()
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
                        if (ParsePreface(readableBuffer, out consumed, out examined))
                        {
                            return true;
                        }
                    }

                    if (result.IsCompleted)
                    {
                        return false;
                    }
                }
                finally
                {
                    Input.AdvanceTo(consumed, examined);

                    UpdateConnectionState();
                }
            }

            return false;
        }

        private bool ParsePreface(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;
            examined = buffer.End;

            if (buffer.Length < ClientPreface.Length)
            {
                return false;
            }

            var preface = buffer.Slice(0, ClientPreface.Length);
            var span = preface.ToSpan();

            if (!span.SequenceEqual(ClientPreface))
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorInvalidPreface, Http2ErrorCode.PROTOCOL_ERROR);
            }

            consumed = examined = preface.End;
            return true;
        }

        private Task ProcessFrameAsync<TContext>(IHttpApplication<TContext> application, in ReadOnlySequence<byte> payload)
        {
            // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1.1
            // Streams initiated by a client MUST use odd-numbered stream identifiers; ...
            // An endpoint that receives an unexpected stream identifier MUST respond with
            // a connection error (Section 5.4.1) of type PROTOCOL_ERROR.
            if (_incomingFrame.StreamId != 0 && (_incomingFrame.StreamId & 1) == 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdEven(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            switch (_incomingFrame.Type)
            {
                case Http2FrameType.DATA:
                    return ProcessDataFrameAsync(payload);
                case Http2FrameType.HEADERS:
                    return ProcessHeadersFrameAsync(application, payload);
                case Http2FrameType.PRIORITY:
                    return ProcessPriorityFrameAsync();
                case Http2FrameType.RST_STREAM:
                    return ProcessRstStreamFrameAsync();
                case Http2FrameType.SETTINGS:
                    return ProcessSettingsFrameAsync(payload);
                case Http2FrameType.PUSH_PROMISE:
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorPushPromiseReceived, Http2ErrorCode.PROTOCOL_ERROR);
                case Http2FrameType.PING:
                    return ProcessPingFrameAsync(payload);
                case Http2FrameType.GOAWAY:
                    return ProcessGoAwayFrameAsync();
                case Http2FrameType.WINDOW_UPDATE:
                    return ProcessWindowUpdateFrameAsync();
                case Http2FrameType.CONTINUATION:
                    return ProcessContinuationFrameAsync(payload);
                default:
                    return ProcessUnknownFrameAsync();
            }
        }

        private Task ProcessDataFrameAsync(in ReadOnlySequence<byte> payload)
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.DataHasPadding && _incomingFrame.DataPadLength >= _incomingFrame.PayloadLength)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorPaddingTooLong(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            ThrowIfIncomingFrameSentToIdleStream();

            if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
            {
                if (stream.RstStreamReceived)
                {
                    // Hard abort, do not allow any more frames on this stream.
                    throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamAborted(_incomingFrame.Type, stream.StreamId), Http2ErrorCode.STREAM_CLOSED);
                }

                if (stream.EndStreamReceived)
                {
                    // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1
                    //
                    // ...an endpoint that receives any frames after receiving a frame with the
                    // END_STREAM flag set MUST treat that as a connection error (Section 5.4.1)
                    // of type STREAM_CLOSED, unless the frame is permitted as described below.
                    //
                    // (The allowed frame types for this situation are WINDOW_UPDATE, RST_STREAM and PRIORITY)
                    throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(_incomingFrame.Type, stream.StreamId), Http2ErrorCode.STREAM_CLOSED);
                }

                return stream.OnDataAsync(_incomingFrame, payload);
            }

            // If we couldn't find the stream, it was either alive previously but closed with
            // END_STREAM or RST_STREAM, or it was implicitly closed when the client opened
            // a new stream with a higher ID. Per the spec, we should send RST_STREAM if
            // the stream was closed with RST_STREAM or implicitly, but the spec also says
            // in http://httpwg.org/specs/rfc7540.html#rfc.section.5.4.1 that
            //
            // An endpoint can end a connection at any time. In particular, an endpoint MAY
            // choose to treat a stream error as a connection error.
            //
            // We choose to do that here so we don't have to keep state to track implicitly closed
            // streams vs. streams closed with END_STREAM or RST_STREAM.
            throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamClosed(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.STREAM_CLOSED);
        }

        private Task ProcessHeadersFrameAsync<TContext>(IHttpApplication<TContext> application, in ReadOnlySequence<byte> payload)
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.HeadersHasPadding && _incomingFrame.HeadersPadLength >= _incomingFrame.PayloadLength - 1)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorPaddingTooLong(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.HeadersHasPriority && _incomingFrame.HeadersStreamDependency == _incomingFrame.StreamId)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamSelfDependency(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
            {
                if (stream.RstStreamReceived)
                {
                    // Hard abort, do not allow any more frames on this stream.
                    throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamAborted(_incomingFrame.Type, stream.StreamId), Http2ErrorCode.STREAM_CLOSED);
                }

                // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1
                //
                // ...an endpoint that receives any frames after receiving a frame with the
                // END_STREAM flag set MUST treat that as a connection error (Section 5.4.1)
                // of type STREAM_CLOSED, unless the frame is permitted as described below.
                //
                // (The allowed frame types after END_STREAM are WINDOW_UPDATE, RST_STREAM and PRIORITY)
                if (stream.EndStreamReceived)
                {
                    throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(_incomingFrame.Type, stream.StreamId), Http2ErrorCode.STREAM_CLOSED);
                }

                // This is the last chance for the client to send END_STREAM
                if (!_incomingFrame.HeadersEndStream)
                {
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorHeadersWithTrailersNoEndStream, Http2ErrorCode.PROTOCOL_ERROR);
                }

                // Since we found an active stream, this HEADERS frame contains trailers
                _currentHeadersStream = stream;
                _requestHeaderParsingState = RequestHeaderParsingState.Trailers;

                var headersPayload = payload.Slice(0, _incomingFrame.HeadersPayloadLength); // Minus padding
                return DecodeTrailersAsync(_incomingFrame.HeadersEndHeaders, headersPayload);
            }
            else if (_incomingFrame.StreamId <= _highestOpenedStreamId)
            {
                // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1.1
                //
                // The first use of a new stream identifier implicitly closes all streams in the "idle"
                // state that might have been initiated by that peer with a lower-valued stream identifier.
                //
                // If we couldn't find the stream, it was previously closed (either implicitly or with
                // END_STREAM or RST_STREAM).
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamClosed(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.STREAM_CLOSED);
            }
            else
            {
                // Cancel keep-alive timeout and start header timeout if necessary.
                if (TimeoutControl.TimerReason != TimeoutReason.None)
                {
                    Debug.Assert(TimeoutControl.TimerReason == TimeoutReason.KeepAlive, "Non keep-alive timeout set at start of stream.");
                    TimeoutControl.CancelTimeout();
                }

                if (!_incomingFrame.HeadersEndHeaders)
                {
                    TimeoutControl.SetTimeout(Limits.RequestHeadersTimeout.Ticks, TimeoutReason.RequestHeaders);
                }

                // Start a new stream
                _currentHeadersStream = new Http2Stream<TContext>(application, new Http2StreamContext
                {
                    ConnectionId = ConnectionId,
                    StreamId = _incomingFrame.StreamId,
                    ServiceContext = _context.ServiceContext,
                    ConnectionFeatures = _context.ConnectionFeatures,
                    MemoryPool = _context.MemoryPool,
                    LocalEndPoint = _context.LocalEndPoint,
                    RemoteEndPoint = _context.RemoteEndPoint,
                    StreamLifetimeHandler = this,
                    ClientPeerSettings = _clientSettings,
                    ServerPeerSettings = _serverSettings,
                    FrameWriter = _frameWriter,
                    ConnectionInputFlowControl = _inputFlowControl,
                    ConnectionOutputFlowControl = _outputFlowControl,
                    TimeoutControl = TimeoutControl,
                });

                _currentHeadersStream.Reset();
                _headerFlags = _incomingFrame.HeadersFlags;

                var headersPayload = payload.Slice(0, _incomingFrame.HeadersPayloadLength); // Minus padding
                return DecodeHeadersAsync(_incomingFrame.HeadersEndHeaders, headersPayload);
            }
        }

        private Task ProcessPriorityFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.PriorityStreamDependency == _incomingFrame.StreamId)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamSelfDependency(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.PayloadLength != 5)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(_incomingFrame.Type, 5), Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            return Task.CompletedTask;
        }

        private Task ProcessRstStreamFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.PayloadLength != 4)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(_incomingFrame.Type, 4), Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            ThrowIfIncomingFrameSentToIdleStream();

            if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
            {
                // Second reset
                if (stream.RstStreamReceived)
                {
                    // Hard abort, do not allow any more frames on this stream.
                    throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamAborted(_incomingFrame.Type, stream.StreamId), Http2ErrorCode.STREAM_CLOSED);
                }

                // No additional inbound header or data frames are allowed for this stream after receiving a reset.
                stream.AbortRstStreamReceived();
            }

            return Task.CompletedTask;
        }

        private Task ProcessSettingsFrameAsync(in ReadOnlySequence<byte> payload)
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId != 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdNotZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.SettingsAck)
            {
                if (_incomingFrame.PayloadLength != 0)
                {
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorSettingsAckLengthNotZero, Http2ErrorCode.FRAME_SIZE_ERROR);
                }

                return Task.CompletedTask;
            }

            if (_incomingFrame.PayloadLength % 6 != 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorSettingsLengthNotMultipleOfSix, Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            try
            {
                // int.MaxValue is the largest allowed windows size.
                var previousInitialWindowSize = (int)_clientSettings.InitialWindowSize;
                var previousMaxFrameSize = _clientSettings.MaxFrameSize;

                _clientSettings.Update(Http2FrameReader.ReadSettings(payload));

                // Ack before we update the windows, they could send data immediately.
                var ackTask = _frameWriter.WriteSettingsAckAsync();

                if (_clientSettings.MaxFrameSize != previousMaxFrameSize)
                {
                    // Don't let the client choose an arbitrarily large size, this will be used for response buffers.
                    _frameWriter.UpdateMaxFrameSize(Math.Min(_clientSettings.MaxFrameSize, _serverSettings.MaxFrameSize));
                }

                // This difference can be negative.
                var windowSizeDifference = (int)_clientSettings.InitialWindowSize - previousInitialWindowSize;

                if (windowSizeDifference != 0)
                {
                    foreach (var stream in _streams.Values)
                    {
                        if (!stream.TryUpdateOutputWindow(windowSizeDifference))
                        {
                            // This means that this caused a stream window to become larger than int.MaxValue.
                            // This can never happen with a well behaved client and MUST be treated as a connection error.
                            // https://httpwg.org/specs/rfc7540.html#rfc.section.6.9.2
                            throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorInitialWindowSizeInvalid, Http2ErrorCode.FLOW_CONTROL_ERROR);
                        }
                    }
                }

                return ackTask.AsTask();
            }
            catch (Http2SettingsParameterOutOfRangeException ex)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorSettingsParameterOutOfRange(ex.Parameter), ex.Parameter == Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE
                    ? Http2ErrorCode.FLOW_CONTROL_ERROR
                    : Http2ErrorCode.PROTOCOL_ERROR);
            }
        }

        private Task ProcessPingFrameAsync(in ReadOnlySequence<byte> payload)
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId != 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdNotZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.PayloadLength != 8)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(_incomingFrame.Type, 8), Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            if (_incomingFrame.PingAck)
            {
                // TODO: verify that payload is equal to the outgoing PING frame
                return Task.CompletedTask;
            }

            return _frameWriter.WritePingAsync(Http2PingFrameFlags.ACK, payload).AsTask();
        }

        private Task ProcessGoAwayFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId != 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdNotZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            StopProcessingNextRequest(serverInitiated: false);

            return Task.CompletedTask;
        }

        private Task ProcessWindowUpdateFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.PayloadLength != 4)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(_incomingFrame.Type, 4), Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            ThrowIfIncomingFrameSentToIdleStream();

            if (_incomingFrame.WindowUpdateSizeIncrement == 0)
            {
                // http://httpwg.org/specs/rfc7540.html#rfc.section.6.9
                // A receiver MUST treat the receipt of a WINDOW_UPDATE
                // frame with an flow-control window increment of 0 as a
                // stream error (Section 5.4.2) of type PROTOCOL_ERROR;
                // errors on the connection flow-control window MUST be
                // treated as a connection error (Section 5.4.1).
                //
                // http://httpwg.org/specs/rfc7540.html#rfc.section.5.4.1
                // An endpoint can end a connection at any time. In
                // particular, an endpoint MAY choose to treat a stream
                // error as a connection error.
                //
                // Since server initiated stream resets are not yet properly
                // implemented and tested, we treat all zero length window
                // increments as connection errors for now.
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorWindowUpdateIncrementZero, Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                if (!_frameWriter.TryUpdateConnectionWindow(_incomingFrame.WindowUpdateSizeIncrement))
                {
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorWindowUpdateSizeInvalid, Http2ErrorCode.FLOW_CONTROL_ERROR);
                }
            }
            else if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
            {
                if (stream.RstStreamReceived)
                {
                    // Hard abort, do not allow any more frames on this stream.
                    throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamAborted(_incomingFrame.Type, stream.StreamId), Http2ErrorCode.STREAM_CLOSED);
                }

                if (!stream.TryUpdateOutputWindow(_incomingFrame.WindowUpdateSizeIncrement))
                {
                    throw new Http2StreamErrorException(_incomingFrame.StreamId, CoreStrings.Http2ErrorWindowUpdateSizeInvalid, Http2ErrorCode.FLOW_CONTROL_ERROR);
                }
            }
            else
            {
                // The stream was not found in the dictionary which means the stream was probably closed. This can
                // happen when the client sends a window update for a stream right as the server closes the same stream
                // Since this is an unavoidable race, we just ignore the window update frame.
            }

            return Task.CompletedTask;
        }

        private Task ProcessContinuationFrameAsync(in ReadOnlySequence<byte> payload)
        {
            if (_currentHeadersStream == null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorContinuationWithNoHeaders, Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId != _currentHeadersStream.StreamId)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
            {
                return DecodeTrailersAsync(_incomingFrame.ContinuationEndHeaders, payload);
            }
            else
            {
                Debug.Assert(TimeoutControl.TimerReason == TimeoutReason.RequestHeaders, "Received continuation frame without request header timeout being set.");

                if (_incomingFrame.HeadersEndHeaders)
                {
                    TimeoutControl.CancelTimeout();
                }

                return DecodeHeadersAsync(_incomingFrame.ContinuationEndHeaders, payload);
            }
        }

        private Task ProcessUnknownFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            return Task.CompletedTask;
        }

        private Task DecodeHeadersAsync(bool endHeaders, in ReadOnlySequence<byte> payload)
        {
            try
            {
                _highestOpenedStreamId = _currentHeadersStream.StreamId;
                _hpackDecoder.Decode(payload, endHeaders, handler: this);

                if (endHeaders)
                {
                    StartStream();
                    ResetRequestHeaderParsingState();
                }
            }
            catch (Http2StreamErrorException)
            {
                ResetRequestHeaderParsingState();
                throw;
            }

            return Task.CompletedTask;
        }

        private Task DecodeTrailersAsync(bool endHeaders, in ReadOnlySequence<byte> payload)
        {
            _hpackDecoder.Decode(payload, endHeaders, handler: this);

            if (endHeaders)
            {
                _currentHeadersStream.OnEndStreamReceived();
                ResetRequestHeaderParsingState();
            }

            return Task.CompletedTask;
        }

        private void StartStream()
        {
            if (!_isMethodConnect && (_parsedPseudoHeaderFields & _mandatoryRequestPseudoHeaderFields) != _mandatoryRequestPseudoHeaderFields)
            {
                // All HTTP/2 requests MUST include exactly one valid value for the :method, :scheme, and :path pseudo-header
                // fields, unless it is a CONNECT request (Section 8.3). An HTTP request that omits mandatory pseudo-header
                // fields is malformed (Section 8.1.2.6).
                throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2ErrorMissingMandatoryPseudoHeaderFields, Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_activeStreamCount >= _serverSettings.MaxConcurrentStreams)
            {
                throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2ErrorMaxStreams, Http2ErrorCode.REFUSED_STREAM);
            }

            // This must be initialized before we offload the request or else we may start processing request body frames without it.
            _currentHeadersStream.InputRemaining = _currentHeadersStream.RequestHeaders.ContentLength;

            // This must wait until we've received all of the headers so we can verify the content-length.
            if ((_headerFlags & Http2HeadersFrameFlags.END_STREAM) == Http2HeadersFrameFlags.END_STREAM)
            {
                _currentHeadersStream.OnEndStreamReceived();
            }

            _activeStreamCount++;
            _streams[_incomingFrame.StreamId] = _currentHeadersStream;
            // Must not allow app code to block the connection handling loop.
            ThreadPool.UnsafeQueueUserWorkItem(_currentHeadersStream, preferLocal: false);
        }

        private void ResetRequestHeaderParsingState()
        {
            _currentHeadersStream = null;
            _requestHeaderParsingState = RequestHeaderParsingState.Ready;
            _parsedPseudoHeaderFields = PseudoHeaderFields.None;
            _headerFlags = Http2HeadersFrameFlags.NONE;
            _isMethodConnect = false;
            _totalParsedHeaderSize = 0;
        }

        private void ThrowIfIncomingFrameSentToIdleStream()
        {
            // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1
            // 5.1. Stream states
            // ...
            // idle:
            // ...
            // Receiving any frame other than HEADERS or PRIORITY on a stream in this state MUST be
            // treated as a connection error (Section 5.4.1) of type PROTOCOL_ERROR.
            //
            // If the stream ID in the incoming frame is higher than the highest opened stream ID so
            // far, then the incoming frame's target stream is in the idle state, which is the implicit
            // initial state for all streams.
            if (_incomingFrame.StreamId > _highestOpenedStreamId)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdle(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }
        }

        private void AbortStream(int streamId, IOException error)
        {
            if (_streams.TryGetValue(streamId, out var stream))
            {
                stream.Abort(error);
            }
        }

        void IRequestProcessor.Tick(DateTimeOffset now)
        {
            Input.CancelPendingRead();
        }

        void IHttp2StreamLifetimeHandler.OnStreamCompleted(Http2Stream stream)
        {
            _completedStreams.Enqueue(stream);
            _streamCompletionAwaitable.Complete();
        }

        private void UpdateCompletedStreams()
        {
            Http2Stream firstRequedStream = null;
            var now = SystemClock.UtcNowTicks;

            while (_completedStreams.TryDequeue(out var stream))
            {
                if (stream == firstRequedStream)
                {
                    // We've checked every stream that was in _completedStreams by the time
                    // _checkCompletedStreams was unset, so exit the loop.
                    _completedStreams.Enqueue(stream);
                    break;
                }

                if (stream.DrainExpirationTicks == default)
                {
                    // This is our first time checking this stream.
                    _activeStreamCount--;
                    stream.DrainExpirationTicks = now + Constants.RequestBodyDrainTimeout.Ticks;
                }

                if (stream.EndStreamReceived || stream.RstStreamReceived || stream.DrainExpirationTicks < now)
                {
                    if (stream == _currentHeadersStream)
                    {
                        // The drain expired out while receiving trailers. The most recent incoming frame is either a header or continuation frame for the timed out stream.
                        throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamClosed(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.STREAM_CLOSED);
                    }

                    _streams.Remove(stream.StreamId);
                }
                else
                {
                    if (firstRequedStream == null)
                    {
                        firstRequedStream = stream;
                    }

                    _completedStreams.Enqueue(stream);
                }
            }
        }

        private void UpdateConnectionState()
        {
            if (_isClosed != 0)
            {
                return;
            }

            if (_gracefulCloseInitiator != GracefulCloseInitiator.None && !_gracefulCloseStarted)
            {
                _gracefulCloseStarted = true;

                Log.Http2ConnectionClosing(_context.ConnectionId);

                if (_gracefulCloseInitiator == GracefulCloseInitiator.Server && _activeStreamCount > 0)
                {
                    _frameWriter.WriteGoAwayAsync(int.MaxValue, Http2ErrorCode.NO_ERROR);
                }
            }

            if (_activeStreamCount == 0)
            {
                if (_gracefulCloseStarted)
                {
                    if (TryClose())
                    {
                        _frameWriter.WriteGoAwayAsync(_highestOpenedStreamId, Http2ErrorCode.NO_ERROR);
                    }
                }
                else
                {
                    if (TimeoutControl.TimerReason == TimeoutReason.None)
                    {
                        TimeoutControl.SetTimeout(Limits.KeepAliveTimeout.Ticks, TimeoutReason.KeepAlive);
                    }

                    // If we're awaiting headers, either a new stream will be started, or there will be a connection
                    // error possibly due to a request header timeout, so no need to start a keep-alive timeout.
                    Debug.Assert(TimeoutControl.TimerReason == TimeoutReason.RequestHeaders ||
                        TimeoutControl.TimerReason == TimeoutReason.KeepAlive);
                }
            }
        }

        // We can't throw a Http2StreamErrorException here, it interrupts the header decompression state and may corrupt subsequent header frames on other streams.
        // For now these either need to be connection errors or BadRequests. If we want to downgrade any of them to stream errors later then we need to
        // rework the flow so that the remaining headers are drained and the decompression state is maintained.
        public void OnHeader(Span<byte> name, Span<byte> value)
        {
            // https://tools.ietf.org/html/rfc7540#section-6.5.2
            // "The value is based on the uncompressed size of header fields, including the length of the name and value in octets plus an overhead of 32 octets for each header field.";
            _totalParsedHeaderSize += HeaderField.RfcOverhead + name.Length + value.Length;
            if (_totalParsedHeaderSize > _context.ServiceContext.ServerOptions.Limits.MaxRequestHeadersTotalSize)
            {
                throw new Http2ConnectionErrorException(CoreStrings.BadRequest_HeadersExceedMaxTotalSize, Http2ErrorCode.PROTOCOL_ERROR);
            }

            ValidateHeader(name, value);
            try
            {
                if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
                {
                    _currentHeadersStream.OnTrailer(name, value);
                }
                else
                {
                    // Throws BadRequest for header count limit breaches.
                    // Throws InvalidOperation for bad encoding.
                    _currentHeadersStream.OnHeader(name, value);
                }
            }
            catch (BadHttpRequestException bre)
            {
                throw new Http2ConnectionErrorException(bre.Message, Http2ErrorCode.PROTOCOL_ERROR);
            }
            catch (InvalidOperationException)
            {
                throw new Http2ConnectionErrorException(CoreStrings.BadRequest_MalformedRequestInvalidHeaders, Http2ErrorCode.PROTOCOL_ERROR);
            }
        }

        public void OnHeadersComplete()
            => _currentHeadersStream.OnHeadersComplete();

        private void ValidateHeader(Span<byte> name, Span<byte> value)
        {
            // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2.1
            /*
               Intermediaries that process HTTP requests or responses (i.e., any
               intermediary not acting as a tunnel) MUST NOT forward a malformed
               request or response.  Malformed requests or responses that are
               detected MUST be treated as a stream error (Section 5.4.2) of type
               PROTOCOL_ERROR.

               For malformed requests, a server MAY send an HTTP response prior to
               closing or resetting the stream.  Clients MUST NOT accept a malformed
               response.  Note that these requirements are intended to protect
               against several types of common attacks against HTTP; they are
               deliberately strict because being permissive can expose
               implementations to these vulnerabilities.*/
            if (IsPseudoHeaderField(name, out var headerField))
            {
                if (_requestHeaderParsingState == RequestHeaderParsingState.Headers)
                {
                    // All pseudo-header fields MUST appear in the header block before regular header fields.
                    // Any request or response that contains a pseudo-header field that appears in a header
                    // block after a regular header field MUST be treated as malformed (Section 8.1.2.6).
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorPseudoHeaderFieldAfterRegularHeaders, Http2ErrorCode.PROTOCOL_ERROR);
                }

                if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
                {
                    // Pseudo-header fields MUST NOT appear in trailers.
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorTrailersContainPseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR);
                }

                _requestHeaderParsingState = RequestHeaderParsingState.PseudoHeaderFields;

                if (headerField == PseudoHeaderFields.Unknown)
                {
                    // Endpoints MUST treat a request or response that contains undefined or invalid pseudo-header
                    // fields as malformed (Section 8.1.2.6).
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorUnknownPseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR);
                }

                if (headerField == PseudoHeaderFields.Status)
                {
                    // Pseudo-header fields defined for requests MUST NOT appear in responses; pseudo-header fields
                    // defined for responses MUST NOT appear in requests.
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorResponsePseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR);
                }

                if ((_parsedPseudoHeaderFields & headerField) == headerField)
                {
                    // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2.3
                    // All HTTP/2 requests MUST include exactly one valid value for the :method, :scheme, and :path pseudo-header fields
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorDuplicatePseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR);
                }

                if (headerField == PseudoHeaderFields.Method)
                {
                    _isMethodConnect = value.SequenceEqual(_connectBytes);
                }

                _parsedPseudoHeaderFields |= headerField;
            }
            else if (_requestHeaderParsingState != RequestHeaderParsingState.Trailers)
            {
                _requestHeaderParsingState = RequestHeaderParsingState.Headers;
            }

            if (IsConnectionSpecificHeaderField(name, value))
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorConnectionSpecificHeaderField, Http2ErrorCode.PROTOCOL_ERROR);
            }

            // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2
            // A request or response containing uppercase header field names MUST be treated as malformed (Section 8.1.2.6).
            for (var i = 0; i < name.Length; i++)
            {
                if (name[i] >= 65 && name[i] <= 90)
                {
                    if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
                    {
                        throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorTrailerNameUppercase, Http2ErrorCode.PROTOCOL_ERROR);
                    }
                    else
                    {
                        throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorHeaderNameUppercase, Http2ErrorCode.PROTOCOL_ERROR);
                    }
                }
            }
        }

        private bool IsPseudoHeaderField(Span<byte> name, out PseudoHeaderFields headerField)
        {
            headerField = PseudoHeaderFields.None;

            if (name.IsEmpty || name[0] != (byte)':')
            {
                return false;
            }

            if (name.SequenceEqual(_pathBytes))
            {
                headerField = PseudoHeaderFields.Path;
            }
            else if (name.SequenceEqual(_methodBytes))
            {
                headerField = PseudoHeaderFields.Method;
            }
            else if (name.SequenceEqual(_schemeBytes))
            {
                headerField = PseudoHeaderFields.Scheme;
            }
            else if (name.SequenceEqual(_statusBytes))
            {
                headerField = PseudoHeaderFields.Status;
            }
            else if (name.SequenceEqual(_authorityBytes))
            {
                headerField = PseudoHeaderFields.Authority;
            }
            else
            {
                headerField = PseudoHeaderFields.Unknown;
            }

            return true;
        }

        private static bool IsConnectionSpecificHeaderField(Span<byte> name, Span<byte> value)
        {
            return name.SequenceEqual(_connectionBytes) || (name.SequenceEqual(_teBytes) && !value.SequenceEqual(_trailersBytes));
        }

        private bool TryClose()
        {
            if (Interlocked.Exchange(ref _isClosed, 1) == 0)
            {
                Log.Http2ConnectionClosed(_context.ConnectionId, _highestOpenedStreamId);
                return true;
            }

            return false;
        }

        private class StreamCloseAwaitable : ICriticalNotifyCompletion
        {
            private static readonly Action _callbackCompleted = () => { };

            // Initialize to completed so UpdateCompletedStreams runs at least once during connection teardown
            // if there are still active streams.
            private Action _callback = _callbackCompleted;

            public StreamCloseAwaitable GetAwaiter() => this;
            public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

            public void GetResult()
            {
                Debug.Assert(ReferenceEquals(_callback, _callbackCompleted));

                _callback = null;
            }

            public void OnCompleted(Action continuation)
            {
                if (ReferenceEquals(_callback, _callbackCompleted) ||
                    ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
                {
                    Task.Run(continuation);
                }
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            public void Complete()
            {
                Interlocked.Exchange(ref _callback, _callbackCompleted)?.Invoke();
            }
        }

        private enum RequestHeaderParsingState
        {
            Ready,
            PseudoHeaderFields,
            Headers,
            Trailers
        }

        [Flags]
        private enum PseudoHeaderFields
        {
            None = 0x0,
            Authority = 0x1,
            Method = 0x2,
            Path = 0x4,
            Scheme = 0x8,
            Status = 0x10,
            Unknown = 0x40000000
        }

        private static class GracefulCloseInitiator
        {
            public const int None = 0;
            public const int Server = 1;
            public const int Client = 2;
        }
    }
}
