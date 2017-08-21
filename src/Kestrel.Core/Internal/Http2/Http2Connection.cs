// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public class Http2Connection : ITimeoutControl, IHttp2StreamLifetimeHandler
    {
        public static byte[] ClientPreface { get; } = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private readonly Http2ConnectionContext _context;
        private readonly Http2FrameWriter _frameWriter;
        private readonly HPackDecoder _hpackDecoder;

        private readonly Http2PeerSettings _serverSettings = new Http2PeerSettings();
        private readonly Http2PeerSettings _clientSettings = new Http2PeerSettings();

        private readonly Http2Frame _incomingFrame = new Http2Frame();

        private Http2Stream _currentHeadersStream;
        private int _lastStreamId;

        private bool _stopping;

        private readonly ConcurrentDictionary<int, Http2Stream> _streams = new ConcurrentDictionary<int, Http2Stream>();

        public Http2Connection(Http2ConnectionContext context)
        {
            _context = context;
            _frameWriter = new Http2FrameWriter(context.Transport.Output, context.Application.Input);
            _hpackDecoder = new HPackDecoder();
        }

        public string ConnectionId => _context.ConnectionId;

        public IPipeReader Input => _context.Transport.Input;

        public IKestrelTrace Log => _context.ServiceContext.Log;

        bool ITimeoutControl.TimedOut => throw new NotImplementedException();

        public void Abort(Exception ex)
        {
            _stopping = true;
            _frameWriter.Abort(ex);
        }

        public void Stop()
        {
            _stopping = true;
            Input.CancelPendingRead();
        }

        public async Task ProcessAsync<TContext>(IHttpApplication<TContext> application)
        {
            Exception error = null;
            var errorCode = Http2ErrorCode.NO_ERROR;

            try
            {
                while (!_stopping)
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
                                break;
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            return;
                        }
                    }
                    finally
                    {
                        Input.Advance(consumed, examined);
                    }
                }

                if (!_stopping)
                {
                    await _frameWriter.WriteSettingsAsync(_serverSettings);
                }

                while (!_stopping)
                {
                    var result = await Input.ReadAsync();
                    var readableBuffer = result.Buffer;
                    var consumed = readableBuffer.Start;
                    var examined = readableBuffer.End;

                    try
                    {
                        if (!readableBuffer.IsEmpty)
                        {
                            if (Http2FrameReader.ReadFrame(readableBuffer, _incomingFrame, out consumed, out examined))
                            {
                                Log.LogTrace($"Connection id {ConnectionId} received {_incomingFrame.Type} frame with flags 0x{_incomingFrame.Flags:x} and length {_incomingFrame.Length} for stream ID {_incomingFrame.StreamId}");
                                await ProcessFrameAsync<TContext>(application);
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            return;
                        }
                    }
                    finally
                    {
                        Input.Advance(consumed, examined);
                    }
                }
            }
            catch (ConnectionAbortedException ex)
            {
                // TODO: log
                error = ex;
            }
            catch (Http2ConnectionErrorException ex)
            {
                // TODO: log
                error = ex;
                errorCode = ex.ErrorCode;
            }
            catch (Exception ex)
            {
                // TODO: log
                error = ex;
                errorCode = Http2ErrorCode.INTERNAL_ERROR;
            }
            finally
            {
                try
                {
                    foreach (var stream in _streams.Values)
                    {
                        stream.Abort(error);
                    }

                    await _frameWriter.WriteGoAwayAsync(_lastStreamId, errorCode);
                }
                finally
                {
                    Input.Complete();
                    _frameWriter.Abort(ex: null);
                }
            }
        }

        private bool ParsePreface(ReadableBuffer readableBuffer, out ReadCursor consumed, out ReadCursor examined)
        {
            consumed = readableBuffer.Start;
            examined = readableBuffer.End;

            if (readableBuffer.Length < ClientPreface.Length)
            {
                return false;
            }

            var span = readableBuffer.IsSingleSpan
                ? readableBuffer.First.Span
                : readableBuffer.ToSpan();

            for (var i = 0; i < ClientPreface.Length; i++)
            {
                if (ClientPreface[i] != span[i])
                {
                    throw new Exception("Invalid HTTP/2 connection preface.");
                }
            }

            consumed = examined = readableBuffer.Move(readableBuffer.Start, ClientPreface.Length);
            return true;
        }

        private Task ProcessFrameAsync<TContext>(IHttpApplication<TContext> application)
        {
            switch (_incomingFrame.Type)
            {
                case Http2FrameType.DATA:
                    return ProcessDataFrameAsync();
                case Http2FrameType.HEADERS:
                    return ProcessHeadersFrameAsync<TContext>(application);
                case Http2FrameType.PRIORITY:
                    return ProcessPriorityFrameAsync();
                case Http2FrameType.RST_STREAM:
                    return ProcessRstStreamFrameAsync();
                case Http2FrameType.SETTINGS:
                    return ProcessSettingsFrameAsync();
                case Http2FrameType.PING:
                    return ProcessPingFrameAsync();
                case Http2FrameType.GOAWAY:
                    return ProcessGoAwayFrameAsync();
                case Http2FrameType.WINDOW_UPDATE:
                    return ProcessWindowUpdateFrameAsync();
                case Http2FrameType.CONTINUATION:
                    return ProcessContinuationFrameAsync<TContext>(application);
            }

            return Task.CompletedTask;
        }

        private Task ProcessDataFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.DataHasPadding && _incomingFrame.DataPadLength >= _incomingFrame.Length)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream) && !stream.MessageBody.IsCompleted)
            {
                return stream.MessageBody.OnDataAsync(_incomingFrame.DataPayload,
                    endStream: (_incomingFrame.DataFlags & Http2DataFrameFlags.END_STREAM) == Http2DataFrameFlags.END_STREAM);
            }

            return _frameWriter.WriteRstStreamAsync(_incomingFrame.StreamId, Http2ErrorCode.STREAM_CLOSED);
        }

        private Task ProcessHeadersFrameAsync<TContext>(IHttpApplication<TContext> application)
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.HeadersHasPadding && _incomingFrame.HeadersPadLength >= _incomingFrame.Length)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            _currentHeadersStream = new Http2Stream<TContext>(application, new Http2StreamContext
            {
                ConnectionId = ConnectionId,
                StreamId =  _incomingFrame.StreamId,
                ServiceContext = _context.ServiceContext,
                PipeFactory = _context.PipeFactory,
                LocalEndPoint = _context.LocalEndPoint,
                RemoteEndPoint = _context.RemoteEndPoint,
                StreamLifetimeHandler = this,
                FrameWriter = _frameWriter
            });
            _currentHeadersStream.ExpectBody = (_incomingFrame.HeadersFlags & Http2HeadersFrameFlags.END_STREAM) == 0;
            _currentHeadersStream.Reset();

            _streams[_incomingFrame.StreamId] = _currentHeadersStream;

            _hpackDecoder.Decode(_incomingFrame.HeadersPayload, _currentHeadersStream.RequestHeaders);

            if ((_incomingFrame.HeadersFlags & Http2HeadersFrameFlags.END_HEADERS) == Http2HeadersFrameFlags.END_HEADERS)
            {
                _lastStreamId = _incomingFrame.StreamId;
                _ = _currentHeadersStream.ProcessRequestAsync();
                _currentHeadersStream = null;
            }

            return Task.CompletedTask;
        }

        private Task ProcessPriorityFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.Length != 5)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            return Task.CompletedTask;
        }

        private Task ProcessRstStreamFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.Length != 4)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
            {
                stream.Abort(error: null);
            }
            else if (_incomingFrame.StreamId > _lastStreamId)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            return Task.CompletedTask;
        }

        private Task ProcessSettingsFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.StreamId != 0)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if ((_incomingFrame.SettingsFlags & Http2SettingsFrameFlags.ACK) == Http2SettingsFrameFlags.ACK && _incomingFrame.Length != 0)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            if (_incomingFrame.Length % 6 != 0)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            try
            {
                _clientSettings.ParseFrame(_incomingFrame);
                return _frameWriter.WriteSettingsAckAsync();
            }
            catch (Http2SettingsParameterOutOfRangeException ex)
            {
                throw new Http2ConnectionErrorException(ex.Parameter == Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE
                    ? Http2ErrorCode.FLOW_CONTROL_ERROR
                    : Http2ErrorCode.PROTOCOL_ERROR);
            }
        }

        private Task ProcessPingFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.Length != 8)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            return _frameWriter.WritePingAsync(Http2PingFrameFlags.ACK, _incomingFrame.Payload);
        }

        private Task ProcessGoAwayFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            Stop();
            return Task.CompletedTask;
        }

        private Task ProcessWindowUpdateFrameAsync()
        {
            if (_currentHeadersStream != null)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_incomingFrame.Length != 4)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.FRAME_SIZE_ERROR);
            }

            if (_incomingFrame.StreamId == 0)
            {
                if (_incomingFrame.WindowUpdateSizeIncrement == 0)
                {
                    throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
                }
            }
            else
            {
                if (_incomingFrame.WindowUpdateSizeIncrement == 0)
                {
                    return _frameWriter.WriteRstStreamAsync(_incomingFrame.StreamId, Http2ErrorCode.PROTOCOL_ERROR);
                }
            }

            return Task.CompletedTask;
        }

        private Task ProcessContinuationFrameAsync<TContext>(IHttpApplication<TContext> application)
        {
            if (_currentHeadersStream == null || _incomingFrame.StreamId != _currentHeadersStream.StreamId)
            {
                throw new Http2ConnectionErrorException(Http2ErrorCode.PROTOCOL_ERROR);
            }

            _hpackDecoder.Decode(_incomingFrame.HeadersPayload, _currentHeadersStream.RequestHeaders);

            if ((_incomingFrame.ContinuationFlags & Http2ContinuationFrameFlags.END_HEADERS) == Http2ContinuationFrameFlags.END_HEADERS)
            {
                _lastStreamId = _currentHeadersStream.StreamId;
                _ = _currentHeadersStream.ProcessRequestAsync();
                _currentHeadersStream = null;
            }

            return Task.CompletedTask;
        }

        void IHttp2StreamLifetimeHandler.OnStreamCompleted(int streamId)
        {
            _streams.TryRemove(streamId, out _);
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
