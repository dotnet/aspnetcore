// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal partial class KestrelTrace : IKestrelTrace
    {
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

        [LoggerMessage(39, LogLevel.Debug, @"Connection id ""{ConnectionId}"" accepted.", EventName = "ConnectionAccepted")]
        private static partial void ConnectionAccepted(ILogger logger, string connectionId);

        public virtual void ConnectionAccepted(string connectionId)
        {
            ConnectionAccepted(_connectionsLogger, connectionId);
        }

        [LoggerMessage(1, LogLevel.Debug, @"Connection id ""{ConnectionId}"" started.", EventName = "ConnectionStart")]
        private static partial void ConnectionStart(ILogger logger, string connectionId);

        public virtual void ConnectionStart(string connectionId)
        {
            ConnectionStart(_connectionsLogger, connectionId);
        }

        [LoggerMessage(2, LogLevel.Debug, @"Connection id ""{ConnectionId}"" stopped.", EventName = "ConnectionStop")]
        private static partial void ConnectionStop(ILogger logger, string connectionId);

        public virtual void ConnectionStop(string connectionId)
        {
            ConnectionStop(_connectionsLogger, connectionId);
        }

        [LoggerMessage(4, LogLevel.Debug, @"Connection id ""{ConnectionId}"" paused.", EventName = "ConnectionPause")]
        private static partial void ConnectionPause(ILogger logger, string connectionId);

        public virtual void ConnectionPause(string connectionId)
        {
            ConnectionPause(_connectionsLogger, connectionId);
        }

        [LoggerMessage(5, LogLevel.Debug, @"Connection id ""{ConnectionId}"" resumed.", EventName = "ConnectionResume")]
        private static partial void ConnectionResume(ILogger logger, string connectionId);

        public virtual void ConnectionResume(string connectionId)
        {
            ConnectionResume(_connectionsLogger, connectionId);
        }

        [LoggerMessage(9, LogLevel.Debug, @"Connection id ""{ConnectionId}"" completed keep alive response.", EventName = "ConnectionKeepAlive")]
        private static partial void ConnectionKeepAlive(ILogger logger, string connectionId);

        public virtual void ConnectionKeepAlive(string connectionId)
        {
            ConnectionKeepAlive(_connectionsLogger, connectionId);
        }

        [LoggerMessage(24, LogLevel.Warning, @"Connection id ""{ConnectionId}"" rejected because the maximum number of concurrent connections has been reached.", EventName = "ConnectionRejected")]
        private static partial void ConnectionRejected(ILogger logger, string connectionId);

        public virtual void ConnectionRejected(string connectionId)
        {
            ConnectionRejected(_connectionsLogger, connectionId);
        }

        [LoggerMessage(10, LogLevel.Debug, @"Connection id ""{ConnectionId}"" disconnecting.", EventName = "ConnectionDisconnect")]
        private static partial void ConnectionDisconnect(ILogger logger, string connectionId);

        public virtual void ConnectionDisconnect(string connectionId)
        {
            ConnectionDisconnect(_connectionsLogger, connectionId);
        }

        [LoggerMessage(13, LogLevel.Error, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": An unhandled exception was thrown by the application.", EventName = "ApplicationError")]
        private static partial void ApplicationError(ILogger logger, string connectionId, string traceIdentifier, Exception ex);

        public virtual void ApplicationError(string connectionId, string traceIdentifier, Exception ex)
        {
            ApplicationError(_generalLogger, connectionId, traceIdentifier, ex);
        }

        [LoggerMessage(18, LogLevel.Debug, @"Connection id ""{ConnectionId}"" write of ""{count}"" body bytes to non-body HEAD response.", EventName = "ConnectionHeadResponseBodyWrite")]
        private static partial void ConnectionHeadResponseBodyWrite(ILogger logger, string connectionId, long count);

        public virtual void ConnectionHeadResponseBodyWrite(string connectionId, long count)
        {
            ConnectionHeadResponseBodyWrite(_generalLogger, connectionId, count);
        }

        [LoggerMessage(16, LogLevel.Debug, "Some connections failed to close gracefully during server shutdown.", EventName = "NotAllConnectionsClosedGracefully")]
        private static partial void NotAllConnectionsClosedGracefully(ILogger logger);

        public virtual void NotAllConnectionsClosedGracefully()
        {
            NotAllConnectionsClosedGracefully(_connectionsLogger);
        }

        [LoggerMessage(17, LogLevel.Debug, @"Connection id ""{ConnectionId}"" bad request data: ""{message}""", EventName = "ConnectionBadRequest")]
        private static partial void ConnectionBadRequest(ILogger logger, string connectionId, string message, Microsoft.AspNetCore.Http.BadHttpRequestException ex);

        public virtual void ConnectionBadRequest(string connectionId, Microsoft.AspNetCore.Http.BadHttpRequestException ex)
        {
            ConnectionBadRequest(_badRequestsLogger, connectionId, ex.Message, ex);
        }

        [LoggerMessage(20, LogLevel.Debug, @"Connection id ""{ConnectionId}"" request processing ended abnormally.", EventName = "RequestProcessingError")]
        private static partial void RequestProcessingError(ILogger logger, string connectionId, Exception ex);

        public virtual void RequestProcessingError(string connectionId, Exception ex)
        {
            RequestProcessingError(_badRequestsLogger, connectionId, ex);
        }

        [LoggerMessage(21, LogLevel.Debug, "Some connections failed to abort during server shutdown.", EventName = "NotAllConnectionsAborted")]
        private static partial void NotAllConnectionsAborted(ILogger logger);

        public virtual void NotAllConnectionsAborted()
        {
            NotAllConnectionsAborted(_connectionsLogger);
        }

        [LoggerMessage(22, LogLevel.Warning, @"As of ""{now}"", the heartbeat has been running for ""{heartbeatDuration}"" which is longer than ""{interval}"". This could be caused by thread pool starvation.", EventName = "HeartbeatSlow")]
        private static partial void HeartbeatSlow(ILogger logger, DateTimeOffset now, TimeSpan heartbeatDuration, TimeSpan interval);

        public virtual void HeartbeatSlow(TimeSpan heartbeatDuration, TimeSpan interval, DateTimeOffset now)
        {
            // while the heartbeat does loop over connections, this log is usually an indicator of threadpool starvation
            HeartbeatSlow(_generalLogger, now, heartbeatDuration, interval);
        }

        [LoggerMessage(23, LogLevel.Critical, @"Connection id ""{ConnectionId}"" application never completed.", EventName = "ApplicationNeverCompleted")]
        private static partial void ApplicationNeverCompleted(ILogger logger, string connectionId);

        public virtual void ApplicationNeverCompleted(string connectionId)
        {
            ApplicationNeverCompleted(_generalLogger, connectionId);
        }

        [LoggerMessage(25, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": started reading request body.", EventName = "RequestBodyStart", SkipEnabledCheck = true)]
        private static partial void RequestBodyStart(ILogger logger, string connectionId, string traceIdentifier);

        public virtual void RequestBodyStart(string connectionId, string traceIdentifier)
        {
            RequestBodyStart(_generalLogger, connectionId, traceIdentifier);
        }

        [LoggerMessage(26, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": done reading request body.", EventName = "RequestBodyDone", SkipEnabledCheck = true)]
        private static partial void RequestBodyDone(ILogger logger, string connectionId, string traceIdentifier);

        public virtual void RequestBodyDone(string connectionId, string traceIdentifier)
        {
            RequestBodyDone(_generalLogger, connectionId, traceIdentifier);
        }

        [LoggerMessage(27, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the request timed out because it was not sent by the client at a minimum of {Rate} bytes/second.", EventName = "RequestBodyMinimumDataRateNotSatisfied")]
        private static partial void RequestBodyMinimumDataRateNotSatisfied(ILogger logger, string connectionId, string? traceIdentifier, double rate);

        public virtual void RequestBodyMinimumDataRateNotSatisfied(string connectionId, string? traceIdentifier, double rate)
        {
            RequestBodyMinimumDataRateNotSatisfied(_badRequestsLogger, connectionId, traceIdentifier, rate);
        }

        [LoggerMessage(32, LogLevel.Information, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the application completed without reading the entire request body.", EventName = "RequestBodyNotEntirelyRead")]
        private static partial void RequestBodyNotEntirelyRead(ILogger logger, string connectionId, string traceIdentifier);

        public virtual void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier)
        {
            RequestBodyNotEntirelyRead(_generalLogger, connectionId, traceIdentifier);
        }

        [LoggerMessage(33, LogLevel.Information, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": automatic draining of the request body timed out after taking over 5 seconds.", EventName = "RequestBodyDrainTimedOut")]
        private static partial void RequestBodyDrainTimedOut(ILogger logger, string connectionId, string traceIdentifier);

        public virtual void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier)
        {
            RequestBodyDrainTimedOut(_generalLogger, connectionId, traceIdentifier);
        }

        [LoggerMessage(28, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the connection was closed because the response was not read by the client at the specified minimum data rate.", EventName = "ResponseMinimumDataRateNotSatisfied")]
        private static partial void ResponseMinimumDataRateNotSatisfied(ILogger logger, string connectionId, string? traceIdentifier);

        public virtual void ResponseMinimumDataRateNotSatisfied(string connectionId, string? traceIdentifier)
        {
            ResponseMinimumDataRateNotSatisfied(_badRequestsLogger, connectionId, traceIdentifier);
        }

        [LoggerMessage(34, LogLevel.Information, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the application aborted the connection.", EventName = "ApplicationAbortedConnection")]
        private static partial void ApplicationAbortedConnection(ILogger logger, string connectionId, string traceIdentifier);

        public virtual void ApplicationAbortedConnection(string connectionId, string traceIdentifier)
        {
            ApplicationAbortedConnection(_connectionsLogger, connectionId, traceIdentifier);
        }

        [LoggerMessage(29, LogLevel.Debug, @"Connection id ""{ConnectionId}"": HTTP/2 connection error.", EventName = "Http2ConnectionError")]
        private static partial void Http2ConnectionError(ILogger logger, string connectionId, Http2ConnectionErrorException ex);

        public virtual void Http2ConnectionError(string connectionId, Http2ConnectionErrorException ex)
        {
            Http2ConnectionError(_http2Logger, connectionId, ex);
        }

        [LoggerMessage(36, LogLevel.Debug, @"Connection id ""{ConnectionId}"" is closing.", EventName = "Http2ConnectionClosing")]
        private static partial void Http2ConnectionClosing(ILogger logger, string connectionId);

        public virtual void Http2ConnectionClosing(string connectionId)
        {
            Http2ConnectionClosing(_http2Logger, connectionId);
        }

        [LoggerMessage(48, LogLevel.Debug, @"Connection id ""{ConnectionId}"" is closed. The last processed stream ID was {HighestOpenedStreamId}.", EventName = "Http2ConnectionClosed")]
        private static partial void Http2ConnectionClosed(ILogger logger, string connectionId, int highestOpenedStreamId);

        public virtual void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId)
        {
            Http2ConnectionClosed(_http2Logger, connectionId, highestOpenedStreamId);
        }

        [LoggerMessage(30, LogLevel.Debug, @"Connection id ""{ConnectionId}"": HTTP/2 stream error.", EventName = "Http2StreamError")]
        private static partial void Http2StreamError(ILogger logger, string connectionId, Http2StreamErrorException ex);

        public virtual void Http2StreamError(string connectionId, Http2StreamErrorException ex)
        {
            Http2StreamError(_http2Logger, connectionId, ex);
        }

        [LoggerMessage(35, LogLevel.Debug, @"Trace id ""{TraceIdentifier}"": HTTP/2 stream error ""{error}"". A Reset is being sent to the stream.", EventName = "Http2StreamResetAbort")]
        private static partial void Http2StreamResetAbort(ILogger logger, string traceIdentifier, Http2ErrorCode error, ConnectionAbortedException abortReason);

        public void Http2StreamResetAbort(string traceIdentifier, Http2ErrorCode error, ConnectionAbortedException abortReason)
        {
            Http2StreamResetAbort(_http2Logger, traceIdentifier, error, abortReason);
        }

        [LoggerMessage(31, LogLevel.Debug, @"Connection id ""{ConnectionId}"": HPACK decoding error while decoding headers for stream ID {StreamId}.", EventName = "HPackDecodingError")]
        private static partial void HPackDecodingError(ILogger logger, string connectionId, int streamId, Exception ex);

        public virtual void HPackDecodingError(string connectionId, int streamId, Exception ex)
        {
            HPackDecodingError(_http2Logger, connectionId, streamId, ex);
        }

        [LoggerMessage(38, LogLevel.Information, @"Connection id ""{ConnectionId}"": HPACK encoding error while encoding headers for stream ID {StreamId}.", EventName = "HPackEncodingError")]
        private static partial void HPackEncodingError(ILogger logger, string connectionId, int streamId, Exception ex);

        public virtual void HPackEncodingError(string connectionId, int streamId, Exception ex)
        {
            HPackEncodingError(_http2Logger, connectionId, streamId, ex);
        }

        [LoggerMessage(37, LogLevel.Trace, @"Connection id ""{ConnectionId}"" received {type} frame for stream ID {id} with length {length} and flags {flags}.", EventName = "Http2FrameReceived", SkipEnabledCheck = true)]
        private static partial void Http2FrameReceived(ILogger logger, string connectionId, Http2FrameType type, int id, int length, object flags);

        public void Http2FrameReceived(string connectionId, Http2Frame frame)
        {
            if (_http2Logger.IsEnabled(LogLevel.Trace))
            {
                Http2FrameReceived(_http2Logger, connectionId, frame.Type, frame.StreamId, frame.PayloadLength, frame.ShowFlags());
            }
        }

        [LoggerMessage(49, LogLevel.Trace, @"Connection id ""{ConnectionId}"" sending {type} frame for stream ID {id} with length {length} and flags {flags}.", EventName = "Http2FrameSending", SkipEnabledCheck = true)]
        private static partial void Http2FrameSending(ILogger logger, string connectionId, Http2FrameType type, int id, int length, object flags);

        public void Http2FrameSending(string connectionId, Http2Frame frame)
        {
            if (_http2Logger.IsEnabled(LogLevel.Trace))
            {
                Http2FrameSending(_http2Logger, connectionId, frame.Type, frame.StreamId, frame.PayloadLength, frame.ShowFlags());
            }
        }

        [LoggerMessage(40, LogLevel.Debug, @"Connection id ""{ConnectionId}"" reached the maximum number of concurrent HTTP/2 streams allowed.", EventName = "Http2MaxConcurrentStreamsReached")]
        private static partial void Http2MaxConcurrentStreamsReached(ILogger logger, string connectionId);

        public void Http2MaxConcurrentStreamsReached(string connectionId)
        {
            Http2MaxConcurrentStreamsReached(_http2Logger, connectionId);
        }

        [LoggerMessage(41, LogLevel.Warning, "One or more of the following response headers have been removed because they are invalid for HTTP/2 and HTTP/3 responses: 'Connection', 'Transfer-Encoding', 'Keep-Alive', 'Upgrade' and 'Proxy-Connection'.", EventName = "InvalidResponseHeaderRemoved")]
        private static partial void InvalidResponseHeaderRemoved(ILogger logger);

        public void InvalidResponseHeaderRemoved()
        {
            InvalidResponseHeaderRemoved(_generalLogger);
        }

        [LoggerMessage(42, LogLevel.Debug, @"Connection id ""{ConnectionId}"": HTTP/3 connection error.", EventName = "Http3ConnectionError")]
        private static partial void Http3ConnectionError(ILogger logger, string connectionId, Http3ConnectionErrorException ex);

        public void Http3ConnectionError(string connectionId, Http3ConnectionErrorException ex)
        {
            Http3ConnectionError(_http3Logger, connectionId, ex);
        }

        [LoggerMessage(43, LogLevel.Debug, @"Connection id ""{ConnectionId}"" is closing.", EventName = "Http3ConnectionClosing")]
        private static partial void Http3ConnectionClosing(ILogger logger, string connectionId);

        public void Http3ConnectionClosing(string connectionId)
        {
            Http3ConnectionClosing(_http3Logger, connectionId);
        }

        [LoggerMessage(44, LogLevel.Debug, @"Connection id ""{ConnectionId}"" is closed. The last processed stream ID was {HighestOpenedStreamId}.", EventName = "Http3ConnectionClosed")]
        private static partial void Http3ConnectionClosed(ILogger logger, string connectionId, long highestOpenedStreamId);

        public void Http3ConnectionClosed(string connectionId, long highestOpenedStreamId)
        {
            Http3ConnectionClosed(_http3Logger, connectionId, highestOpenedStreamId);
        }

        [LoggerMessage(45, LogLevel.Debug, @"Trace id ""{TraceIdentifier}"": HTTP/3 stream error ""{error}"". An abort is being sent to the stream.", EventName = "Http3StreamAbort", SkipEnabledCheck = true)]
        private static partial void Http3StreamAbort(ILogger logger, string traceIdentifier, string error, ConnectionAbortedException abortReason);

        public void Http3StreamAbort(string traceIdentifier, Http3ErrorCode error, ConnectionAbortedException abortReason)
        {
            if (_http3Logger.IsEnabled(LogLevel.Debug))
            {
                Http3StreamAbort(_http3Logger, traceIdentifier, Http3Formatting.ToFormattedErrorCode(error), abortReason);
            }
        }

        [LoggerMessage(46, LogLevel.Trace, @"Connection id ""{ConnectionId}"" received {type} frame for stream ID {id} with length {length}.", EventName = "Http3FrameReceived", SkipEnabledCheck = true)]
        private static partial void Http3FrameReceived(ILogger logger, string connectionId, string type, long id, long length);

        public void Http3FrameReceived(string connectionId, long streamId, Http3RawFrame frame)
        {
            if (_http3Logger.IsEnabled(LogLevel.Trace))
            {
                Http3FrameReceived(_http3Logger, connectionId, Http3Formatting.ToFormattedType(frame.Type), streamId, frame.Length);
            }
        }

        [LoggerMessage(47, LogLevel.Trace, @"Connection id ""{ConnectionId}"" sending {type} frame for stream ID {id} with length {length}.", EventName = "Http3FrameSending", SkipEnabledCheck = true)]
        private static partial void Http3FrameSending(ILogger logger, string connectionId, string type, long id, long length);

        public void Http3FrameSending(string connectionId, long streamId, Http3RawFrame frame)
        {
            if (_http3Logger.IsEnabled(LogLevel.Trace))
            {
                Http3FrameSending(_http3Logger, connectionId, Http3Formatting.ToFormattedType(frame.Type), streamId, frame.Length);
            }
        }

        [LoggerMessage(50, LogLevel.Debug, @"Connection id ""{ConnectionId}"": Unexpected error when initializing outbound control stream.", EventName = "Http3OutboundControlStreamError")]
        private static partial void Http3OutboundControlStreamError(ILogger logger, string connectionId, Exception ex);

        public void Http3OutboundControlStreamError(string connectionId, Exception ex)
        {
            Http3OutboundControlStreamError(_http3Logger, connectionId, ex);
        }

        [LoggerMessage(51, LogLevel.Debug, @"Connection id ""{ConnectionId}"": QPACK decoding error while decoding headers for stream ID {StreamId}.", EventName = "QPackDecodingError")]
        private static partial void QPackDecodingError(ILogger logger, string connectionId, long streamId, Exception ex);

        public virtual void QPackDecodingError(string connectionId, long streamId, Exception ex)
        {
            QPackDecodingError(_http3Logger, connectionId, streamId, ex);
        }

        [LoggerMessage(52, LogLevel.Information, @"Connection id ""{ConnectionId}"": QPACK encoding error while encoding headers for stream ID {StreamId}.", EventName = "QPackEncodingError")]
        private static partial void QPackEncodingError(ILogger logger, string connectionId, long streamId, Exception ex);

        public virtual void QPackEncodingError(string connectionId, long streamId, Exception ex)
        {
            QPackEncodingError(_http3Logger, connectionId, streamId, ex);
        }

        [LoggerMessage(53, LogLevel.Debug, @"Connection id ""{ConnectionId}"": Highest opened stream ID {HighestOpenedStreamId} in GOAWAY.", EventName = "Http3GoAwayHighestOpenedStreamId")]
        private static partial void Http3GoAwayHighestOpenedStreamId(ILogger logger, string connectionId, long highestOpenedStreamId);

        public void Http3GoAwayHighestOpenedStreamId(string connectionId, long highestOpenedStreamId)
        {
            Http3GoAwayHighestOpenedStreamId(_http3Logger, connectionId, highestOpenedStreamId);
        }

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _generalLogger.Log(logLevel, eventId, state, exception, formatter);

        public virtual bool IsEnabled(LogLevel logLevel) => _generalLogger.IsEnabled(logLevel);

        public virtual IDisposable BeginScope<TState>(TState state) => _generalLogger.BeginScope(state);
    }
}
