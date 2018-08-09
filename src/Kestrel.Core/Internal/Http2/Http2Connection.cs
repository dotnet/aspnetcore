// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
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
    public class Http2Connection : ITimeoutControl, IHttp2StreamLifetimeHandler, IHttpHeadersHandler, IRequestProcessor
    {
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

        private readonly Http2ConnectionContext _context;
        private readonly Http2FrameWriter _frameWriter;
        private readonly HPackDecoder _hpackDecoder;
        private readonly InputFlowControl _inputFlowControl = new InputFlowControl(Http2PeerSettings.DefaultInitialWindowSize, Http2PeerSettings.DefaultInitialWindowSize / 2);
        private readonly OutputFlowControl _outputFlowControl = new OutputFlowControl(Http2PeerSettings.DefaultInitialWindowSize);

        private readonly Http2PeerSettings _serverSettings = new Http2PeerSettings();
        private readonly Http2PeerSettings _clientSettings = new Http2PeerSettings();

        private readonly Http2Frame _incomingFrame = new Http2Frame();

        private Http2Stream _currentHeadersStream;
        private RequestHeaderParsingState _requestHeaderParsingState;
        private PseudoHeaderFields _parsedPseudoHeaderFields;
        private Http2HeadersFrameFlags _headerFlags;
        private bool _isMethodConnect;
        private readonly object _stateLock = new object();
        private int _highestOpenedStreamId;
        private Http2ConnectionState _state = Http2ConnectionState.Open;
        private readonly TaskCompletionSource<object> _streamsCompleted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly ConcurrentDictionary<int, Http2Stream> _streams = new ConcurrentDictionary<int, Http2Stream>();

        public Http2Connection(Http2ConnectionContext context)
        {
            _context = context;
            _frameWriter = new Http2FrameWriter(context.Transport.Output, context.Application.Input, _outputFlowControl, this, context.ConnectionId, context.ServiceContext.Log);
            _hpackDecoder = new HPackDecoder((int)_serverSettings.HeaderTableSize);
        }

        public string ConnectionId => _context.ConnectionId;

        public PipeReader Input => _context.Transport.Input;

        public IKestrelTrace Log => _context.ServiceContext.Log;

        public IFeatureCollection ConnectionFeatures => _context.ConnectionFeatures;

        public void OnInputOrOutputCompleted()
        {
            lock (_stateLock)
            {
                if (_state != Http2ConnectionState.Closed)
                {
                    _frameWriter.WriteGoAwayAsync(_highestOpenedStreamId, Http2ErrorCode.NO_ERROR);
                    UpdateState(Http2ConnectionState.Closed);
                }
            }

            _frameWriter.Complete();
        }

        public void Abort(ConnectionAbortedException ex)
        {
            lock (_stateLock)
            {
                if (_state != Http2ConnectionState.Closed)
                {
                    _frameWriter.WriteGoAwayAsync(_highestOpenedStreamId, Http2ErrorCode.INTERNAL_ERROR);
                    UpdateState(Http2ConnectionState.Closed);
                }
            }

            _frameWriter.Abort(ex);
        }

        public void StopProcessingNextRequest()
            => StopProcessingNextRequest(true);

        public void StopProcessingNextRequest(bool sendGracefulGoAway = false)
        {
            lock (_stateLock)
            {
                if (_state == Http2ConnectionState.Open)
                {
                    if (_streams.IsEmpty)
                    {
                        _frameWriter.WriteGoAwayAsync(_highestOpenedStreamId, Http2ErrorCode.NO_ERROR);
                        UpdateState(Http2ConnectionState.Closed);

                        // Wake up request processing loop so the connection can complete if there are no pending requests
                        Input.CancelPendingRead();
                    }
                    else
                    {
                        if (sendGracefulGoAway)
                        {
                            _frameWriter.WriteGoAwayAsync(Int32.MaxValue, Http2ErrorCode.NO_ERROR);
                        }

                        UpdateState(Http2ConnectionState.Closing);
                    }
                }
            }
        }

        public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application)
        {
            Exception error = null;
            var errorCode = Http2ErrorCode.NO_ERROR;

            try
            {
                ValidateTlsRequirements();

                if (!await TryReadPrefaceAsync())
                {
                    return;
                }

                if (_state != Http2ConnectionState.Closed)
                {
                    await _frameWriter.WriteSettingsAsync(_serverSettings);
                }

                while (_state != Http2ConnectionState.Closed)
                {
                    var result = await Input.ReadAsync();
                    var readableBuffer = result.Buffer;
                    var consumed = readableBuffer.Start;
                    var examined = readableBuffer.End;

                    try
                    {
                        if (!readableBuffer.IsEmpty)
                        {
                            if (Http2FrameReader.ReadFrame(readableBuffer, _incomingFrame, _serverSettings.MaxFrameSize, out consumed, out examined))
                            {
                                Log.Http2FrameReceived(ConnectionId, _incomingFrame);
                                await ProcessFrameAsync(application);
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
                        AbortStream(_incomingFrame.StreamId, new ConnectionAbortedException(ex.Message, ex));
                        await _frameWriter.WriteRstStreamAsync(ex.StreamId, ex.ErrorCode);
                    }
                    finally
                    {
                        Input.AdvanceTo(consumed, examined);
                    }
                }
            }
            catch (ConnectionResetException ex)
            {
                // Don't log ECONNRESET errors when there are no active streams on the connection. Browsers like IE will reset connections regularly.
                if (_streams.Count > 0)
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
                    lock (_stateLock)
                    {
                        if (_state != Http2ConnectionState.Closed)
                        {
                            _frameWriter.WriteGoAwayAsync(_highestOpenedStreamId, errorCode);
                            UpdateState(Http2ConnectionState.Closed);
                        }

                        if (_streams.IsEmpty)
                        {
                            _streamsCompleted.TrySetResult(null);
                        }
                    }

                    // Ensure aborting each stream doesn't result in unnecessary WINDOW_UPDATE frames being sent.
                    _inputFlowControl.StopWindowUpdates();

                    foreach (var stream in _streams.Values)
                    {
                        stream.Abort(connectionError);
                    }

                    await _streamsCompleted.Task;

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
            while (_state != Http2ConnectionState.Closed)
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
                }
            }

            return false;
        }

        private bool ParsePreface(ReadOnlySequence<byte> readableBuffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = readableBuffer.Start;
            examined = readableBuffer.End;

            if (readableBuffer.Length < ClientPreface.Length)
            {
                return false;
            }

            var span = readableBuffer.IsSingleSegment
                ? readableBuffer.First.Span
                : readableBuffer.ToSpan();

            for (var i = 0; i < ClientPreface.Length; i++)
            {
                if (ClientPreface[i] != span[i])
                {
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorInvalidPreface, Http2ErrorCode.PROTOCOL_ERROR);
                }
            }

            consumed = examined = readableBuffer.GetPosition(ClientPreface.Length);
            return true;
        }

        private Task ProcessFrameAsync<TContext>(IHttpApplication<TContext> application)
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
                    return ProcessDataFrameAsync();
                case Http2FrameType.HEADERS:
                    return ProcessHeadersFrameAsync(application);
                case Http2FrameType.PRIORITY:
                    return ProcessPriorityFrameAsync();
                case Http2FrameType.RST_STREAM:
                    return ProcessRstStreamFrameAsync();
                case Http2FrameType.SETTINGS:
                    return ProcessSettingsFrameAsync();
                case Http2FrameType.PUSH_PROMISE:
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorPushPromiseReceived, Http2ErrorCode.PROTOCOL_ERROR);
                case Http2FrameType.PING:
                    return ProcessPingFrameAsync();
                case Http2FrameType.GOAWAY:
                    return ProcessGoAwayFrameAsync();
                case Http2FrameType.WINDOW_UPDATE:
                    return ProcessWindowUpdateFrameAsync();
                case Http2FrameType.CONTINUATION:
                    return ProcessContinuationFrameAsync(application);
                default:
                    return ProcessUnknownFrameAsync();
            }
        }

        private Task ProcessDataFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.DataHasPadding && _incomingFrame.DataPadLength >= _incomingFrame.Length)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorPaddingTooLong(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            ThrowIfIncomingFrameSentToIdleStream();

            if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
            {
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

                return stream.OnDataAsync(_incomingFrame);
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

        private async Task ProcessHeadersFrameAsync<TContext>(IHttpApplication<TContext> application)
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.HeadersHasPadding && _incomingFrame.HeadersPadLength >= _incomingFrame.Length)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorPaddingTooLong(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.HeadersHasPriority && _incomingFrame.HeadersStreamDependency == _incomingFrame.StreamId)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamSelfDependency(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
            {
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
                if ((_incomingFrame.HeadersFlags & Http2HeadersFrameFlags.END_STREAM) == 0)
                {
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorHeadersWithTrailersNoEndStream, Http2ErrorCode.PROTOCOL_ERROR);
                }

                // Since we found an active stream, this HEADERS frame contains trailers
                _currentHeadersStream = stream;
                _requestHeaderParsingState = RequestHeaderParsingState.Trailers;

                var endHeaders = (_incomingFrame.HeadersFlags & Http2HeadersFrameFlags.END_HEADERS) == Http2HeadersFrameFlags.END_HEADERS;
                await DecodeTrailersAsync(endHeaders, _incomingFrame.HeadersPayload);
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
                // Start a new stream
                _currentHeadersStream = new Http2Stream(new Http2StreamContext
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
                    FrameWriter = _frameWriter,
                    ConnectionInputFlowControl = _inputFlowControl,
                    ConnectionOutputFlowControl = _outputFlowControl,
                    TimeoutControl = this,
                });

                _currentHeadersStream.Reset();
                _headerFlags = _incomingFrame.HeadersFlags;

                var endHeaders = (_incomingFrame.HeadersFlags & Http2HeadersFrameFlags.END_HEADERS) == Http2HeadersFrameFlags.END_HEADERS;
                await DecodeHeadersAsync(application, endHeaders, _incomingFrame.HeadersPayload);
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

            if (_incomingFrame.Length != 5)
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

            if (_incomingFrame.Length != 4)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(_incomingFrame.Type, 4), Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            ThrowIfIncomingFrameSentToIdleStream();
            AbortStream(_incomingFrame.StreamId, new ConnectionAbortedException(CoreStrings.Http2StreamResetByClient));

            return Task.CompletedTask;
        }

        private Task ProcessSettingsFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId != 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdNotZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if ((_incomingFrame.SettingsFlags & Http2SettingsFrameFlags.ACK) == Http2SettingsFrameFlags.ACK)
            {
                if (_incomingFrame.Length != 0)
                {
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorSettingsAckLengthNotZero, Http2ErrorCode.FRAME_SIZE_ERROR);
                }

                return Task.CompletedTask;
            }

            if (_incomingFrame.Length % 6 != 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorSettingsLengthNotMultipleOfSix, Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            try
            {
                // ParseFrame will not parse an InitialWindowSize > int.MaxValue.
                var previousInitialWindowSize = (int)_clientSettings.InitialWindowSize;

                _clientSettings.ParseFrame(_incomingFrame);

                var ackTask = _frameWriter.WriteSettingsAckAsync(); // Ack before we update the windows, they could send data immediately.

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

                return ackTask;
            }
            catch (Http2SettingsParameterOutOfRangeException ex)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorSettingsParameterOutOfRange(ex.Parameter), ex.Parameter == Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE
                    ? Http2ErrorCode.FLOW_CONTROL_ERROR
                    : Http2ErrorCode.PROTOCOL_ERROR);
            }
        }

        private Task ProcessPingFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId != 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdNotZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.Length != 8)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(_incomingFrame.Type, 8), Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            if ((_incomingFrame.PingFlags & Http2PingFrameFlags.ACK) == Http2PingFrameFlags.ACK)
            {
                // TODO: verify that payload is equal to the outgoing PING frame
                return Task.CompletedTask;
            }

            return _frameWriter.WritePingAsync(Http2PingFrameFlags.ACK, _incomingFrame.Payload);
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

            StopProcessingNextRequest(sendGracefulGoAway: false);

            return Task.CompletedTask;
        }

        private Task ProcessWindowUpdateFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.Length != 4)
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

        private Task ProcessContinuationFrameAsync<TContext>(IHttpApplication<TContext> application)
        {
            if (_currentHeadersStream == null)
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorContinuationWithNoHeaders, Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId != _currentHeadersStream.StreamId)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR);
            }

            var endHeaders = (_incomingFrame.ContinuationFlags & Http2ContinuationFrameFlags.END_HEADERS) == Http2ContinuationFrameFlags.END_HEADERS;

            if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
            {
                return DecodeTrailersAsync(endHeaders, _incomingFrame.Payload);
            }
            else
            {
                return DecodeHeadersAsync(application, endHeaders, _incomingFrame.Payload);
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

        private Task DecodeHeadersAsync<TContext>(IHttpApplication<TContext> application, bool endHeaders, Span<byte> payload)
        {
            try
            {
                lock (_stateLock)
                {
                    _highestOpenedStreamId = _currentHeadersStream.StreamId;
                    _hpackDecoder.Decode(payload, endHeaders, handler: this);

                    if (endHeaders)
                    {
                        if (_state != Http2ConnectionState.Closed)
                        {
                            StartStream(application);
                        }

                        ResetRequestHeaderParsingState();
                    }
                }
            }
            catch (Http2StreamErrorException)
            {
                ResetRequestHeaderParsingState();
                throw;
            }

            return Task.CompletedTask;
        }

        private Task DecodeTrailersAsync(bool endHeaders, Span<byte> payload)
        {
            _hpackDecoder.Decode(payload, endHeaders, handler: this);

            if (endHeaders)
            {
                _currentHeadersStream.OnEndStreamReceived();
                ResetRequestHeaderParsingState();
            }

            return Task.CompletedTask;
        }

        private void StartStream<TContext>(IHttpApplication<TContext> application)
        {
            if (!_isMethodConnect && (_parsedPseudoHeaderFields & _mandatoryRequestPseudoHeaderFields) != _mandatoryRequestPseudoHeaderFields)
            {
                // All HTTP/2 requests MUST include exactly one valid value for the :method, :scheme, and :path pseudo-header
                // fields, unless it is a CONNECT request (Section 8.3). An HTTP request that omits mandatory pseudo-header
                // fields is malformed (Section 8.1.2.6).
                throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2ErrorMissingMandatoryPseudoHeaderFields, Http2ErrorCode.PROTOCOL_ERROR);
            }

            // This must be initialized before we offload the request or else we may start processing request body frames without it.
            _currentHeadersStream.InputRemaining = _currentHeadersStream.RequestHeaders.ContentLength;

            // This must wait until we've received all of the headers so we can verify the content-length.
            if ((_headerFlags & Http2HeadersFrameFlags.END_STREAM) == Http2HeadersFrameFlags.END_STREAM)
            {
                _currentHeadersStream.OnEndStreamReceived();
            }

            _streams[_incomingFrame.StreamId] = _currentHeadersStream;
            // Must not allow app code to block the connection handling loop.
            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                var (app, currentStream) = (Tuple<IHttpApplication<TContext>, Http2Stream>)state;
                _ = currentStream.ProcessRequestsAsync(app);
            },
            new Tuple<IHttpApplication<TContext>, Http2Stream>(application, _currentHeadersStream));
        }

        private void ResetRequestHeaderParsingState()
        {
            _currentHeadersStream = null;
            _requestHeaderParsingState = RequestHeaderParsingState.Ready;
            _parsedPseudoHeaderFields = PseudoHeaderFields.None;
            _headerFlags = Http2HeadersFrameFlags.NONE;
            _isMethodConnect = false;
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

        private void AbortStream(int streamId, ConnectionAbortedException error)
        {
            if (_streams.TryGetValue(streamId, out var stream))
            {
                stream.Abort(error);
            }
        }

        void IHttp2StreamLifetimeHandler.OnStreamCompleted(int streamId)
        {
            lock (_stateLock)
            {
                _streams.TryRemove(streamId, out _);

                if (_streams.IsEmpty)
                {
                    if (_state == Http2ConnectionState.Closing)
                    {
                        _frameWriter.WriteGoAwayAsync(_highestOpenedStreamId, Http2ErrorCode.NO_ERROR);
                        UpdateState(Http2ConnectionState.Closed);

                        // Wake up request processing loop so the connection can complete if there are no pending requests
                        Input.CancelPendingRead();
                    }

                    // Complete the task waiting on all streams to finish
                    _streamsCompleted.TrySetResult(null);
                }
            }
        }

        public void OnHeader(Span<byte> name, Span<byte> value)
        {
            ValidateHeader(name, value);
            _currentHeadersStream.OnHeader(name, value);
        }

        private void ValidateHeader(Span<byte> name, Span<byte> value)
        {
            // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2.1
            if (IsPseudoHeaderField(name, out var headerField))
            {
                if (_requestHeaderParsingState == RequestHeaderParsingState.Headers)
                {
                    // All pseudo-header fields MUST appear in the header block before regular header fields.
                    // Any request or response that contains a pseudo-header field that appears in a header
                    // block after a regular header field MUST be treated as malformed (Section 8.1.2.6).
                    throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2ErrorPseudoHeaderFieldAfterRegularHeaders, Http2ErrorCode.PROTOCOL_ERROR);
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
                    throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2ErrorUnknownPseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR);
                }

                if (headerField == PseudoHeaderFields.Status)
                {
                    // Pseudo-header fields defined for requests MUST NOT appear in responses; pseudo-header fields
                    // defined for responses MUST NOT appear in requests.
                    throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2ErrorResponsePseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR);
                }

                if ((_parsedPseudoHeaderFields & headerField) == headerField)
                {
                    // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2.3
                    // All HTTP/2 requests MUST include exactly one valid value for the :method, :scheme, and :path pseudo-header fields
                    throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2ErrorDuplicatePseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR);
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
                throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2ErrorConnectionSpecificHeaderField, Http2ErrorCode.PROTOCOL_ERROR);
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
                        throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2ErrorHeaderNameUppercase, Http2ErrorCode.PROTOCOL_ERROR);
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

        private void UpdateState(Http2ConnectionState state)
        {
            _state = state;
            if (state == Http2ConnectionState.Closing)
            {
                Log.Http2ConnectionClosing(_context.ConnectionId);
            }
            else if (state == Http2ConnectionState.Closed)
            {
                Log.Http2ConnectionClosed(_context.ConnectionId, _highestOpenedStreamId);
            }
        }

        void ITimeoutControl.SetTimeout(long ticks, TimeoutAction timeoutAction)
        {
        }

        void ITimeoutControl.ResetTimeout(long ticks, TimeoutAction timeoutAction)
        {
        }

        void ITimeoutControl.CancelTimeout()
        {
        }

        void ITimeoutControl.StartTimingReads()
        {
        }

        void ITimeoutControl.PauseTimingReads()
        {
        }

        void ITimeoutControl.ResumeTimingReads()
        {
        }

        void ITimeoutControl.StopTimingReads()
        {
        }

        void ITimeoutControl.BytesRead(long count)
        {
        }

        void ITimeoutControl.StartTimingWrite(long size)
        {
        }

        void ITimeoutControl.StopTimingWrite()
        {
        }
    }
}
