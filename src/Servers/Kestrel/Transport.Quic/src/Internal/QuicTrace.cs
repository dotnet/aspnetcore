// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal sealed partial class QuicTrace : ILogger
    {
        private readonly ILogger _logger;

        public QuicTrace(ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);

        [LoggerMessage(1, LogLevel.Debug, @"Connection id ""{ConnectionId}"" accepted.", EventName = "AcceptedConnection", SkipEnabledCheck = true)]
        private static partial void AcceptedConnection(ILogger logger, string connectionId);

        public void AcceptedConnection(BaseConnectionContext connection)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                AcceptedConnection(_logger, connection.ConnectionId);
            }
        }

        [LoggerMessage(2, LogLevel.Debug, @"Stream id ""{ConnectionId}"" type {StreamType} accepted.", EventName = "AcceptedStream", SkipEnabledCheck = true)]
        private static partial void AcceptedStream(ILogger logger, string connectionId, StreamType streamType);

        public void AcceptedStream(QuicStreamContext streamContext)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                AcceptedStream(_logger, streamContext.ConnectionId, GetStreamType(streamContext));
            }
        }

        [LoggerMessage(3, LogLevel.Debug, @"Stream id ""{ConnectionId}"" type {StreamType} connected.", EventName = "ConnectedStream", SkipEnabledCheck = true)]
        private static partial void ConnectedStream(ILogger logger, string connectionId, StreamType streamType);

        public void ConnectedStream(QuicStreamContext streamContext)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                ConnectedStream(_logger, streamContext.ConnectionId, GetStreamType(streamContext));
            }
        }

        [LoggerMessage(4, LogLevel.Debug, @"Connection id ""{ConnectionId}"" unexpected error.", EventName = "ConnectionError", SkipEnabledCheck = true)]
        private static partial void ConnectionError(ILogger logger, string connectionId, Exception ex);

        public void ConnectionError(BaseConnectionContext connection, Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionError(_logger, connection.ConnectionId, ex);
            }
        }

        [LoggerMessage(5, LogLevel.Debug, @"Connection id ""{ConnectionId}"" aborted by peer with error code {ErrorCode}.", EventName = "ConnectionAborted", SkipEnabledCheck = true)]
        private static partial void ConnectionAborted(ILogger logger, string connectionId, long errorCode, Exception ex);

        public void ConnectionAborted(BaseConnectionContext connection, long errorCode, Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionAborted(_logger, connection.ConnectionId, errorCode, ex);
            }
        }

        [LoggerMessage(6, LogLevel.Debug, @"Connection id ""{ConnectionId}"" aborted by application with error code {ErrorCode} because: ""{Reason}"".", EventName = "ConnectionAbort", SkipEnabledCheck = true)]
        private static partial void ConnectionAbort(ILogger logger, string connectionId, long errorCode, string reason);

        public void ConnectionAbort(BaseConnectionContext connection, long errorCode, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionAbort(_logger, connection.ConnectionId, errorCode, reason);
            }
        }

        [LoggerMessage(7, LogLevel.Debug, @"Stream id ""{ConnectionId}"" unexpected error.", EventName = "StreamError", SkipEnabledCheck = true)]
        private static partial void StreamError(ILogger logger, string connectionId, Exception ex);

        public void StreamError(QuicStreamContext streamContext, Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                StreamError(_logger, streamContext.ConnectionId, ex);
            }
        }

        [LoggerMessage(8, LogLevel.Debug, @"Stream id ""{ConnectionId}"" paused.", EventName = "StreamPause", SkipEnabledCheck = true)]
        private static partial void StreamPause(ILogger logger, string connectionId);

        public void StreamPause(QuicStreamContext streamContext)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                StreamPause(_logger, streamContext.ConnectionId);
            }
        }

        [LoggerMessage(9, LogLevel.Debug, @"Stream id ""{ConnectionId}"" resumed.", EventName = "StreamResume", SkipEnabledCheck = true)]
        private static partial void StreamResume(ILogger logger, string connectionId);

        public void StreamResume(QuicStreamContext streamContext)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                StreamResume(_logger, streamContext.ConnectionId);
            }
        }

        [LoggerMessage(10, LogLevel.Debug, @"Stream id ""{ConnectionId}"" shutting down writes because: ""{Reason}"".", EventName = "StreamShutdownWrite", SkipEnabledCheck = true)]
        private static partial void StreamShutdownWrite(ILogger logger, string connectionId, string reason);

        public void StreamShutdownWrite(QuicStreamContext streamContext, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                StreamShutdownWrite(_logger, streamContext.ConnectionId, reason);
            }
        }

        [LoggerMessage(11, LogLevel.Debug, @"Stream id ""{ConnectionId}"" read aborted by peer with error code {ErrorCode}.", EventName = "StreamAborted", SkipEnabledCheck = true)]
        private static partial void StreamAbortedRead(ILogger logger, string connectionId, long errorCode);

        public void StreamAbortedRead(QuicStreamContext streamContext, long errorCode)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                StreamAbortedRead(_logger, streamContext.ConnectionId, errorCode);
            }
        }

        [LoggerMessage(12, LogLevel.Debug, @"Stream id ""{ConnectionId}"" write aborted by peer with error code {ErrorCode}.", EventName = "StreamAborted", SkipEnabledCheck = true)]
        private static partial void StreamAbortedWrite(ILogger logger, string connectionId, long errorCode);

        public void StreamAbortedWrite(QuicStreamContext streamContext, long errorCode)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                StreamAbortedWrite(_logger, streamContext.ConnectionId, errorCode);
            }
        }

        [LoggerMessage(13, LogLevel.Debug, @"Stream id ""{ConnectionId}"" aborted by application with error code {ErrorCode} because: ""{Reason}"".", EventName = "StreamAbort", SkipEnabledCheck = true)]
        private static partial void StreamAbort(ILogger logger, string connectionId, long errorCode, string reason);

        public void StreamAbort(QuicStreamContext streamContext, long errorCode, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                StreamAbort(_logger, streamContext.ConnectionId, errorCode, reason);
            }
        }

        [LoggerMessage(14, LogLevel.Debug, @"Stream id ""{ConnectionId}"" read side aborted by application with error code {ErrorCode} because: ""{Reason}"".", SkipEnabledCheck = true)]
        private static partial void StreamAbortRead(ILogger logger, string connectionId, long errorCode, string reason);

        public void StreamAbortRead(QuicStreamContext streamContext, long errorCode, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                StreamAbortRead(_logger, streamContext.ConnectionId, errorCode, reason);
            }
        }

        [LoggerMessage(15, LogLevel.Debug, @"Stream id ""{ConnectionId}"" write side aborted by application with error code {ErrorCode} because: ""{Reason}"".", SkipEnabledCheck = true)]
        private static partial void StreamAbortWrite(ILogger logger, string connectionId, long errorCode, string reason);
        
        public void StreamAbortWrite(QuicStreamContext streamContext, long errorCode, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                StreamAbortWrite(_logger, streamContext.ConnectionId, errorCode, reason);
            }
        }
        
        private static StreamType GetStreamType(QuicStreamContext streamContext) =>
            streamContext.CanRead && streamContext.CanWrite
                ? StreamType.Bidirectional
                : StreamType.Unidirectional;

        private enum StreamType
        {
            Unidirectional,
            Bidirectional
        }
    }
}


