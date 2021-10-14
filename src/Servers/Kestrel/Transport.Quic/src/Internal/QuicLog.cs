// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal static partial class QuicLog
    {
        [LoggerMessage(1, LogLevel.Debug, @"Connection id ""{ConnectionId}"" accepted.", EventName = "AcceptedConnection", SkipEnabledCheck = true)]
        private static partial void AcceptedConnectionCore(ILogger logger, string connectionId);

        public static void AcceptedConnection(ILogger logger, BaseConnectionContext connection)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                AcceptedConnectionCore(logger, connection.ConnectionId);
            }
        }

        [LoggerMessage(2, LogLevel.Debug, @"Stream id ""{ConnectionId}"" type {StreamType} accepted.", EventName = "AcceptedStream", SkipEnabledCheck = true)]
        private static partial void AcceptedStreamCore(ILogger logger, string connectionId, StreamType streamType);

        public static void AcceptedStream(ILogger logger, QuicStreamContext streamContext)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                AcceptedStreamCore(logger, streamContext.ConnectionId, GetStreamType(streamContext));
            }
        }

        [LoggerMessage(3, LogLevel.Debug, @"Stream id ""{ConnectionId}"" type {StreamType} connected.", EventName = "ConnectedStream", SkipEnabledCheck = true)]
        private static partial void ConnectedStreamCore(ILogger logger, string connectionId, StreamType streamType);

        public static void ConnectedStream(ILogger logger, QuicStreamContext streamContext)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ConnectedStreamCore(logger, streamContext.ConnectionId, GetStreamType(streamContext));
            }
        }

        [LoggerMessage(4, LogLevel.Debug, @"Connection id ""{ConnectionId}"" unexpected error.", EventName = "ConnectionError", SkipEnabledCheck = true)]
        private static partial void ConnectionErrorCore(ILogger logger, string connectionId, Exception ex);

        public static void ConnectionError(ILogger logger, BaseConnectionContext connection, Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionErrorCore(logger, connection.ConnectionId, ex);
            }
        }

        [LoggerMessage(5, LogLevel.Debug, @"Connection id ""{ConnectionId}"" aborted by peer with error code {ErrorCode}.", EventName = "ConnectionAborted", SkipEnabledCheck = true)]
        private static partial void ConnectionAbortedCore(ILogger logger, string connectionId, long errorCode, Exception ex);

        public static void ConnectionAborted(ILogger logger, BaseConnectionContext connection, long errorCode, Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionAbortedCore(logger, connection.ConnectionId, errorCode, ex);
            }
        }

        [LoggerMessage(6, LogLevel.Debug, @"Connection id ""{ConnectionId}"" aborted by application with error code {ErrorCode} because: ""{Reason}"".", EventName = "ConnectionAbort", SkipEnabledCheck = true)]
        private static partial void ConnectionAbortCore(ILogger logger, string connectionId, long errorCode, string reason);

        public static void ConnectionAbort(ILogger logger, BaseConnectionContext connection, long errorCode, string reason)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionAbortCore(logger, connection.ConnectionId, errorCode, reason);
            }
        }

        [LoggerMessage(7, LogLevel.Debug, @"Stream id ""{ConnectionId}"" unexpected error.", EventName = "StreamError", SkipEnabledCheck = true)]
        private static partial void StreamErrorCore(ILogger logger, string connectionId, Exception ex);

        public static void StreamError(ILogger logger, QuicStreamContext streamContext, Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StreamErrorCore(logger, streamContext.ConnectionId, ex);
            }
        }

        [LoggerMessage(8, LogLevel.Debug, @"Stream id ""{ConnectionId}"" paused.", EventName = "StreamPause", SkipEnabledCheck = true)]
        private static partial void StreamPauseCore(ILogger logger, string connectionId);

        public static void StreamPause(ILogger logger, QuicStreamContext streamContext)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StreamPauseCore(logger, streamContext.ConnectionId);
            }
        }

        [LoggerMessage(9, LogLevel.Debug, @"Stream id ""{ConnectionId}"" resumed.", EventName = "StreamResume", SkipEnabledCheck = true)]
        private static partial void StreamResumeCore(ILogger logger, string connectionId);

        public static void StreamResume(ILogger logger, QuicStreamContext streamContext)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StreamResumeCore(logger, streamContext.ConnectionId);
            }
        }

        [LoggerMessage(10, LogLevel.Debug, @"Stream id ""{ConnectionId}"" shutting down writes because: ""{Reason}"".", EventName = "StreamShutdownWrite", SkipEnabledCheck = true)]
        private static partial void StreamShutdownWriteCore(ILogger logger, string connectionId, string reason);

        public static void StreamShutdownWrite(ILogger logger, QuicStreamContext streamContext, string reason)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StreamShutdownWriteCore(logger, streamContext.ConnectionId, reason);
            }
        }

        [LoggerMessage(11, LogLevel.Debug, @"Stream id ""{ConnectionId}"" read aborted by peer with error code {ErrorCode}.", EventName = "StreamAborted", SkipEnabledCheck = true)]
        private static partial void StreamAbortedReadCore(ILogger logger, string connectionId, long errorCode);

        public static void StreamAbortedRead(ILogger logger, QuicStreamContext streamContext, long errorCode)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StreamAbortedReadCore(logger, streamContext.ConnectionId, errorCode);
            }
        }

        [LoggerMessage(12, LogLevel.Debug, @"Stream id ""{ConnectionId}"" write aborted by peer with error code {ErrorCode}.", EventName = "StreamAborted", SkipEnabledCheck = true)]
        private static partial void StreamAbortedWriteCore(ILogger logger, string connectionId, long errorCode);

        public static void StreamAbortedWrite(ILogger logger, QuicStreamContext streamContext, long errorCode)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StreamAbortedWriteCore(logger, streamContext.ConnectionId, errorCode);
            }
        }

        [LoggerMessage(13, LogLevel.Debug, @"Stream id ""{ConnectionId}"" aborted by application with error code {ErrorCode} because: ""{Reason}"".", EventName = "StreamAbort", SkipEnabledCheck = true)]
        private static partial void StreamAbortCore(ILogger logger, string connectionId, long errorCode, string reason);

        public static void StreamAbort(ILogger logger, QuicStreamContext streamContext, long errorCode, string reason)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StreamAbortCore(logger, streamContext.ConnectionId, errorCode, reason);
            }
        }

        [LoggerMessage(14, LogLevel.Debug, @"Stream id ""{ConnectionId}"" read side aborted by application with error code {ErrorCode} because: ""{Reason}"".", SkipEnabledCheck = true)]
        private static partial void StreamAbortReadCore(ILogger logger, string connectionId, long errorCode, string reason);

        public static void StreamAbortRead(ILogger logger, QuicStreamContext streamContext, long errorCode, string reason)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StreamAbortReadCore(logger, streamContext.ConnectionId, errorCode, reason);
            }
        }

        [LoggerMessage(15, LogLevel.Debug, @"Stream id ""{ConnectionId}"" write side aborted by application with error code {ErrorCode} because: ""{Reason}"".", SkipEnabledCheck = true)]
        private static partial void StreamAbortWriteCore(ILogger logger, string connectionId, long errorCode, string reason);
        
        public static void StreamAbortWrite(ILogger logger, QuicStreamContext streamContext, long errorCode, string reason)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StreamAbortWriteCore(logger, streamContext.ConnectionId, errorCode, reason);
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


