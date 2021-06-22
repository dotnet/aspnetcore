// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.HPack;
using System.Net.Http.QPack;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class KestrelTrace : IKestrelTrace
    {
        private static readonly Action<ILogger, string, Exception?> _connectionStart =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "ConnectionStart"), @"Connection id ""{ConnectionId}"" started.");

        private static readonly Action<ILogger, string, Exception?> _connectionStop =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, "ConnectionStop"), @"Connection id ""{ConnectionId}"" stopped.");

        private static readonly Action<ILogger, string, Exception?> _connectionPause =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, "ConnectionPause"), @"Connection id ""{ConnectionId}"" paused.");

        private static readonly Action<ILogger, string, Exception?> _connectionResume =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, "ConnectionResume"), @"Connection id ""{ConnectionId}"" resumed.");

        private static readonly Action<ILogger, string, Exception?> _connectionKeepAlive =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(9, "ConnectionKeepAlive"), @"Connection id ""{ConnectionId}"" completed keep alive response.");

        private static readonly Action<ILogger, string, Exception?> _connectionDisconnect =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(10, "ConnectionDisconnect"), @"Connection id ""{ConnectionId}"" disconnecting.");

        private static readonly Action<ILogger, string, string, Exception> _applicationError =
            LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(13, "ApplicationError"), @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": An unhandled exception was thrown by the application.");

        private static readonly Action<ILogger, Exception?> _notAllConnectionsClosedGracefully =
            LoggerMessage.Define(LogLevel.Debug, new EventId(16, "NotAllConnectionsClosedGracefully"), "Some connections failed to close gracefully during server shutdown.");

        private static readonly Action<ILogger, string, string, Exception> _connectionBadRequest =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(17, "ConnectionBadRequest"), @"Connection id ""{ConnectionId}"" bad request data: ""{message}""");

        private static readonly Action<ILogger, string, long, Exception?> _connectionHeadResponseBodyWrite =
            LoggerMessage.Define<string, long>(LogLevel.Debug, new EventId(18, "ConnectionHeadResponseBodyWrite"), @"Connection id ""{ConnectionId}"" write of ""{count}"" body bytes to non-body HEAD response.");

        private static readonly Action<ILogger, string, Exception> _requestProcessingError =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(20, "RequestProcessingError"), @"Connection id ""{ConnectionId}"" request processing ended abnormally.");

        private static readonly Action<ILogger, Exception?> _notAllConnectionsAborted =
            LoggerMessage.Define(LogLevel.Debug, new EventId(21, "NotAllConnectionsAborted"), "Some connections failed to abort during server shutdown.");

        private static readonly Action<ILogger, DateTimeOffset, TimeSpan, TimeSpan, Exception?> _heartbeatSlow =
            LoggerMessage.Define<DateTimeOffset, TimeSpan, TimeSpan>(LogLevel.Warning, new EventId(22, "HeartbeatSlow"), @"As of ""{now}"", the heartbeat has been running for ""{heartbeatDuration}"" which is longer than ""{interval}"". This could be caused by thread pool starvation.");

        private static readonly Action<ILogger, string, Exception?> _applicationNeverCompleted =
            LoggerMessage.Define<string>(LogLevel.Critical, new EventId(23, "ApplicationNeverCompleted"), @"Connection id ""{ConnectionId}"" application never completed.");

        private static readonly Action<ILogger, string, Exception?> _connectionRejected =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(24, "ConnectionRejected"), @"Connection id ""{ConnectionId}"" rejected because the maximum number of concurrent connections has been reached.");

        private static readonly Action<ILogger, string, string, Exception?> _requestBodyStart =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(25, "RequestBodyStart"), @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": started reading request body.", skipEnabledCheck: true);

        private static readonly Action<ILogger, string, string, Exception?> _requestBodyDone =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(26, "RequestBodyDone"), @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": done reading request body.", skipEnabledCheck: true);

        private static readonly Action<ILogger, string, string?, double, Exception?> _requestBodyMinimumDataRateNotSatisfied =
            LoggerMessage.Define<string, string?, double>(LogLevel.Debug, new EventId(27, "RequestBodyMinimumDataRateNotSatisfied"), @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the request timed out because it was not sent by the client at a minimum of {Rate} bytes/second.");

        private static readonly Action<ILogger, string, string?, Exception?> _responseMinimumDataRateNotSatisfied =
            LoggerMessage.Define<string, string?>(LogLevel.Debug, new EventId(28, "ResponseMinimumDataRateNotSatisfied"), @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the connection was closed because the response was not read by the client at the specified minimum data rate.");

        private static readonly Action<ILogger, string, Exception> _http2ConnectionError =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(29, "Http2ConnectionError"), @"Connection id ""{ConnectionId}"": HTTP/2 connection error.");

        private static readonly Action<ILogger, string, Exception> _http2StreamError =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(30, "Http2StreamError"), @"Connection id ""{ConnectionId}"": HTTP/2 stream error.");

        private static readonly Action<ILogger, string, int, Exception> _hpackDecodingError =
            LoggerMessage.Define<string, int>(LogLevel.Debug, new EventId(31, "HPackDecodingError"), @"Connection id ""{ConnectionId}"": HPACK decoding error while decoding headers for stream ID {StreamId}.");

        private static readonly Action<ILogger, string, string, Exception?> _requestBodyNotEntirelyRead =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(32, "RequestBodyNotEntirelyRead"), @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the application completed without reading the entire request body.");

        private static readonly Action<ILogger, string, string, Exception?> _requestBodyDrainTimedOut =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(33, "RequestBodyDrainTimedOut"), @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": automatic draining of the request body timed out after taking over 5 seconds.");

        private static readonly Action<ILogger, string, string, Exception?> _applicationAbortedConnection =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(34, "ApplicationAbortedConnection"), @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the application aborted the connection.");

        private static readonly Action<ILogger, string, Http2ErrorCode, Exception> _http2StreamResetAbort =
            LoggerMessage.Define<string, Http2ErrorCode>(LogLevel.Debug, new EventId(35, "Http2StreamResetAbort"),
                @"Trace id ""{TraceIdentifier}"": HTTP/2 stream error ""{error}"". A Reset is being sent to the stream.");

        private static readonly Action<ILogger, string, Exception?> _http2ConnectionClosing =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(36, "Http2ConnectionClosing"),
                @"Connection id ""{ConnectionId}"" is closing.");

        private static readonly Action<ILogger, string, int, Exception?> _http2ConnectionClosed =
            LoggerMessage.Define<string, int>(LogLevel.Debug, new EventId(48, "Http2ConnectionClosed"),
                @"Connection id ""{ConnectionId}"" is closed. The last processed stream ID was {HighestOpenedStreamId}.");

        private static readonly Action<ILogger, string, Http2FrameType, int, int, object, Exception?> _http2FrameReceived =
            LoggerMessage.Define<string, Http2FrameType, int, int, object>(LogLevel.Trace, new EventId(37, "Http2FrameReceived"),
                @"Connection id ""{ConnectionId}"" received {type} frame for stream ID {id} with length {length} and flags {flags}.",
                skipEnabledCheck: true);

        private static readonly Action<ILogger, string, Http2FrameType, int, int, object, Exception?> _http2FrameSending =
            LoggerMessage.Define<string, Http2FrameType, int, int, object>(LogLevel.Trace, new EventId(49, "Http2FrameSending"),
                @"Connection id ""{ConnectionId}"" sending {type} frame for stream ID {id} with length {length} and flags {flags}.",
                skipEnabledCheck: true);

        private static readonly Action<ILogger, string, int, Exception> _hpackEncodingError =
            LoggerMessage.Define<string, int>(LogLevel.Information, new EventId(38, "HPackEncodingError"),
                @"Connection id ""{ConnectionId}"": HPACK encoding error while encoding headers for stream ID {StreamId}.");

        private static readonly Action<ILogger, string, Exception?> _connectionAccepted =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(39, "ConnectionAccepted"), @"Connection id ""{ConnectionId}"" accepted.");

        private static readonly Action<ILogger, string, Exception?> _http2MaxConcurrentStreamsReached =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(40, "Http2MaxConcurrentStreamsReached"),
                @"Connection id ""{ConnectionId}"" reached the maximum number of concurrent HTTP/2 streams allowed.");

        private static readonly Action<ILogger, Exception?> _invalidResponseHeaderRemoved =
            LoggerMessage.Define(LogLevel.Warning, new EventId(41, "InvalidResponseHeaderRemoved"),
                "One or more of the following response headers have been removed because they are invalid for HTTP/2 and HTTP/3 responses: 'Connection', 'Transfer-Encoding', 'Keep-Alive', 'Upgrade' and 'Proxy-Connection'.");

        private static readonly Action<ILogger, string, Exception> _http3ConnectionError =
               LoggerMessage.Define<string>(LogLevel.Debug, new EventId(42, "Http3ConnectionError"), @"Connection id ""{ConnectionId}"": HTTP/3 connection error.");

        private static readonly Action<ILogger, string, Exception?> _http3ConnectionClosing =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(43, "Http3ConnectionClosing"),
                @"Connection id ""{ConnectionId}"" is closing.");

        private static readonly Action<ILogger, string, long, Exception?> _http3ConnectionClosed =
            LoggerMessage.Define<string, long>(LogLevel.Debug, new EventId(44, "Http3ConnectionClosed"),
                @"Connection id ""{ConnectionId}"" is closed. The last processed stream ID was {HighestOpenedStreamId}.");

        private static readonly Action<ILogger, string, string, Exception> _http3StreamAbort =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(45, "Http3StreamAbort"),
                @"Trace id ""{TraceIdentifier}"": HTTP/3 stream error ""{error}"". An abort is being sent to the stream.",
                skipEnabledCheck: true);

        private static readonly Action<ILogger, string, string, long, long, Exception?> _http3FrameReceived =
            LoggerMessage.Define<string, string, long, long>(LogLevel.Trace, new EventId(46, "Http3FrameReceived"),
                @"Connection id ""{ConnectionId}"" received {type} frame for stream ID {id} with length {length}.",
                skipEnabledCheck: true);

        private static readonly Action<ILogger, string, string, long, long, Exception?> _http3FrameSending =
            LoggerMessage.Define<string, string, long, long>(LogLevel.Trace, new EventId(47, "Http3FrameSending"),
                @"Connection id ""{ConnectionId}"" sending {type} frame for stream ID {id} with length {length}.",
                skipEnabledCheck: true);

        private static readonly Action<ILogger, string, long, Exception> _qpackDecodingError =
            LoggerMessage.Define<string, long>(LogLevel.Debug, new EventId(48, "QPackDecodingError"),
                @"Connection id ""{ConnectionId}"": QPACK decoding error while decoding headers for stream ID {StreamId}.");

        private static readonly Action<ILogger, string, long, Exception> _qpackEncodingError =
            LoggerMessage.Define<string, long>(LogLevel.Information, new EventId(49, "QPackEncodingError"),
                @"Connection id ""{ConnectionId}"": QPACK encoding error while encoding headers for stream ID {StreamId}.");

        protected readonly ILogger _generalLogger;
        protected readonly ILogger _badRequestsLogger;
        protected readonly ILogger _connectionsLogger;
        protected readonly ILogger _http2Logger;
        protected readonly ILogger _http3Logger;

        public KestrelTrace(ILoggerFactory loggerFactory)
        {
            _generalLogger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel");
            _badRequestsLogger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.BadRequests");
            _connectionsLogger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Connections");
            _http2Logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Http2");
            _http3Logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Http3");
        }

        public virtual void ConnectionAccepted(string connectionId)
        {
            _connectionAccepted(_connectionsLogger, connectionId, null);
        }

        public virtual void ConnectionStart(string connectionId)
        {
            _connectionStart(_connectionsLogger, connectionId, null);
        }

        public virtual void ConnectionStop(string connectionId)
        {
            _connectionStop(_connectionsLogger, connectionId, null);
        }

        public virtual void ConnectionPause(string connectionId)
        {
            _connectionPause(_connectionsLogger, connectionId, null);
        }

        public virtual void ConnectionResume(string connectionId)
        {
            _connectionResume(_connectionsLogger, connectionId, null);
        }

        public virtual void ConnectionKeepAlive(string connectionId)
        {
            _connectionKeepAlive(_connectionsLogger, connectionId, null);
        }

        public virtual void ConnectionRejected(string connectionId)
        {
            _connectionRejected(_connectionsLogger, connectionId, null);
        }

        public virtual void ConnectionDisconnect(string connectionId)
        {
            _connectionDisconnect(_connectionsLogger, connectionId, null);
        }

        public virtual void ApplicationError(string connectionId, string traceIdentifier, Exception ex)
        {
            _applicationError(_generalLogger, connectionId, traceIdentifier, ex);
        }

        public virtual void ConnectionHeadResponseBodyWrite(string connectionId, long count)
        {
            _connectionHeadResponseBodyWrite(_generalLogger, connectionId, count, null);
        }

        public virtual void NotAllConnectionsClosedGracefully()
        {
            _notAllConnectionsClosedGracefully(_connectionsLogger, null);
        }

        public virtual void ConnectionBadRequest(string connectionId, Microsoft.AspNetCore.Http.BadHttpRequestException ex)
        {
            _connectionBadRequest(_badRequestsLogger, connectionId, ex.Message, ex);
        }

        public virtual void RequestProcessingError(string connectionId, Exception ex)
        {
            _requestProcessingError(_badRequestsLogger, connectionId, ex);
        }

        public virtual void NotAllConnectionsAborted()
        {
            _notAllConnectionsAborted(_connectionsLogger, null);
        }

        public virtual void HeartbeatSlow(TimeSpan heartbeatDuration, TimeSpan interval, DateTimeOffset now)
        {
            // while the heartbeat does loop over connections, this log is usually an indicator of threadpool starvation
            _heartbeatSlow(_generalLogger, now, heartbeatDuration, interval, null);
        }

        public virtual void ApplicationNeverCompleted(string connectionId)
        {
            _applicationNeverCompleted(_generalLogger, connectionId, null);
        }

        public virtual void RequestBodyStart(string connectionId, string traceIdentifier)
        {
            _requestBodyStart(_generalLogger, connectionId, traceIdentifier, null);
        }

        public virtual void RequestBodyDone(string connectionId, string traceIdentifier)
        {
            _requestBodyDone(_generalLogger, connectionId, traceIdentifier, null);
        }

        public virtual void RequestBodyMinimumDataRateNotSatisfied(string connectionId, string? traceIdentifier, double rate)
        {
            _requestBodyMinimumDataRateNotSatisfied(_badRequestsLogger, connectionId, traceIdentifier, rate, null);
        }

        public virtual void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier)
        {
            _requestBodyNotEntirelyRead(_generalLogger, connectionId, traceIdentifier, null);
        }

        public virtual void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier)
        {
            _requestBodyDrainTimedOut(_generalLogger, connectionId, traceIdentifier, null);
        }

        public virtual void ResponseMinimumDataRateNotSatisfied(string connectionId, string? traceIdentifier)
        {
            _responseMinimumDataRateNotSatisfied(_generalLogger, connectionId, traceIdentifier, null);
        }

        public virtual void ApplicationAbortedConnection(string connectionId, string traceIdentifier)
        {
            _applicationAbortedConnection(_connectionsLogger, connectionId, traceIdentifier, null);
        }

        public virtual void Http2ConnectionError(string connectionId, Http2ConnectionErrorException ex)
        {
            _http2ConnectionError(_http2Logger, connectionId, ex);
        }

        public virtual void Http2ConnectionClosing(string connectionId)
        {
            _http2ConnectionClosing(_http2Logger, connectionId, null);
        }

        public virtual void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId)
        {
            _http2ConnectionClosed(_http2Logger, connectionId, highestOpenedStreamId, null);
        }

        public virtual void Http2StreamError(string connectionId, Http2StreamErrorException ex)
        {
            _http2StreamError(_http2Logger, connectionId, ex);
        }

        public void Http2StreamResetAbort(string traceIdentifier, Http2ErrorCode error, ConnectionAbortedException abortReason)
        {
            _http2StreamResetAbort(_http2Logger, traceIdentifier, error, abortReason);
        }

        public virtual void HPackDecodingError(string connectionId, int streamId, HPackDecodingException ex)
        {
            _hpackDecodingError(_http2Logger, connectionId, streamId, ex);
        }

        public virtual void HPackEncodingError(string connectionId, int streamId, HPackEncodingException ex)
        {
            _hpackEncodingError(_http2Logger, connectionId, streamId, ex);
        }

        public void Http2FrameReceived(string connectionId, Http2Frame frame)
        {
            if (_http2Logger.IsEnabled(LogLevel.Trace))
            {
                _http2FrameReceived(_http2Logger, connectionId, frame.Type, frame.StreamId, frame.PayloadLength, frame.ShowFlags(), null);
            }
        }

        public void Http2FrameSending(string connectionId, Http2Frame frame)
        {
            if (_http2Logger.IsEnabled(LogLevel.Trace))
            {
                _http2FrameSending(_http2Logger, connectionId, frame.Type, frame.StreamId, frame.PayloadLength, frame.ShowFlags(), null);
            }
        }

        public void Http2MaxConcurrentStreamsReached(string connectionId)
        {
            _http2MaxConcurrentStreamsReached(_http2Logger, connectionId, null);
        }

        public void InvalidResponseHeaderRemoved()
        {
            _invalidResponseHeaderRemoved(_generalLogger, null);
        }

        public void Http3ConnectionError(string connectionId, Http3ConnectionErrorException ex)
        {
            _http3ConnectionError(_http3Logger, connectionId, ex);
        }

        public void Http3ConnectionClosing(string connectionId)
        {
            _http3ConnectionClosing(_http3Logger, connectionId, null);
        }

        public void Http3ConnectionClosed(string connectionId, long highestOpenedStreamId)
        {
            _http3ConnectionClosed(_http3Logger, connectionId, highestOpenedStreamId, null);
        }

        public void Http3StreamAbort(string traceIdentifier, Http3ErrorCode error, ConnectionAbortedException abortReason)
        {
            if (_http3Logger.IsEnabled(LogLevel.Debug))
            {
                _http3StreamAbort(_http3Logger, traceIdentifier, Http3Formatting.ToFormattedErrorCode(error), abortReason);
            }
        }

        public void Http3FrameReceived(string connectionId, long streamId, Http3RawFrame frame)
        {
            if (_http3Logger.IsEnabled(LogLevel.Trace))
            {
                _http3FrameReceived(_http3Logger, connectionId, Http3Formatting.ToFormattedType(frame.Type), streamId, frame.Length, null);
            }
        }

        public void Http3FrameSending(string connectionId, long streamId, Http3RawFrame frame)
        {
            if (_http3Logger.IsEnabled(LogLevel.Trace))
            {
                _http3FrameSending(_http3Logger, connectionId, Http3Formatting.ToFormattedType(frame.Type), streamId, frame.Length, null);
            }
        }

        public virtual void QPackDecodingError(string connectionId, long streamId, QPackDecodingException ex)
        {
            _qpackDecodingError(_http3Logger, connectionId, streamId, ex);
        }

        public virtual void QPackEncodingError(string connectionId, long streamId, QPackEncodingException ex)
        {
            _qpackEncodingError(_http3Logger, connectionId, streamId, ex);
        }

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _generalLogger.Log(logLevel, eventId, state, exception, formatter);

        public virtual bool IsEnabled(LogLevel logLevel) => _generalLogger.IsEnabled(logLevel);

        public virtual IDisposable BeginScope<TState>(TState state) => _generalLogger.BeginScope(state);
    }
}
