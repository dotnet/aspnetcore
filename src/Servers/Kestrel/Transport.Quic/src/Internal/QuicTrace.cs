// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal class QuicTrace : IQuicTrace
    {
        private static readonly LogDefineOptions SkipEnabledCheckLogOptions = new() { SkipEnabledCheck = true };

        private static readonly Action<ILogger, string, Exception?> _acceptedConnection =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "AcceptedConnection"), @"Connection id ""{ConnectionId}"" accepted.", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, StreamType, Exception?> _acceptedStream =
            LoggerMessage.Define<string, StreamType>(LogLevel.Debug, new EventId(2, "AcceptedStream"), @"Stream id ""{ConnectionId}"" type {StreamType} accepted.", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, StreamType, Exception?> _connectedStream =
            LoggerMessage.Define<string, StreamType>(LogLevel.Debug, new EventId(3, "ConnectedStream"), @"Stream id ""{ConnectionId}"" type {StreamType} connected.", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, Exception?> _connectionError =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, "ConnectionError"), @"Connection id ""{ConnectionId}"" unexpected error.", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, Exception?> _connectionAborted =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, "ConnectionAborted"), @"Connection id ""{ConnectionId}"" aborted by peer.", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, string, Exception?> _connectionAbort =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(6, "ConnectionAbort"), @"Connection id ""{ConnectionId}"" aborted by application because: ""{Reason}"".", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, Exception?> _streamError =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(7, "StreamError"), @"Stream id ""{ConnectionId}"" unexpected error.", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, Exception?> _streamPause =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(8, "StreamPause"), @"Stream id ""{ConnectionId}"" paused.", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, Exception?> _streamResume =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(9, "StreamResume"), @"Stream id ""{ConnectionId}"" resumed.", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, string, Exception?> _streamShutdownWrite =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(10, "StreamShutdownWrite"), @"Stream id ""{ConnectionId}"" shutting down writes because: ""{Reason}"".", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, Exception?> _streamAborted =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(11, "StreamAborted"), @"Stream id ""{ConnectionId}"" aborted by peer.", SkipEnabledCheckLogOptions);
        private static readonly Action<ILogger, string, string, Exception?> _streamAbort =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(12, "StreamAbort"), @"Stream id ""{ConnectionId}"" aborted by application because: ""{Reason}"".", SkipEnabledCheckLogOptions);

        private readonly ILogger _logger;

        public QuicTrace(ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);

        public void AcceptedConnection(BaseConnectionContext connection)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _acceptedConnection(_logger, connection.ConnectionId, null);
            }
        }

        public void AcceptedStream(QuicStreamContext streamContext)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _acceptedStream(_logger, streamContext.ConnectionId, GetStreamType(streamContext), null);
            }
        }

        public void ConnectedStream(QuicStreamContext streamContext)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _connectedStream(_logger, streamContext.ConnectionId, GetStreamType(streamContext), null);
            }
        }

        public void ConnectionError(BaseConnectionContext connection, Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _connectionError(_logger, connection.ConnectionId, ex);
            }
        }

        public void ConnectionAborted(BaseConnectionContext connection, Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _connectionAborted(_logger, connection.ConnectionId, ex);
            }
        }

        public void ConnectionAbort(BaseConnectionContext connection, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _connectionAbort(_logger, connection.ConnectionId, reason, null);
            }
        }

        public void StreamError(QuicStreamContext streamContext, Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _streamError(_logger, streamContext.ConnectionId, ex);
            }
        }

        public void StreamPause(QuicStreamContext streamContext)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _streamPause(_logger, streamContext.ConnectionId, null);
            }
        }

        public void StreamResume(QuicStreamContext streamContext)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _streamResume(_logger, streamContext.ConnectionId, null);
            }
        }

        public void StreamShutdownWrite(QuicStreamContext streamContext, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _streamShutdownWrite(_logger, streamContext.ConnectionId, reason, null);
            }
        }

        public void StreamAborted(QuicStreamContext streamContext, Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _streamAborted(_logger, streamContext.ConnectionId, ex);
            }
        }

        public void StreamAbort(QuicStreamContext streamContext, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _streamAbort(_logger, streamContext.ConnectionId, reason, null);
            }
        }

        private StreamType GetStreamType(QuicStreamContext streamContext) =>
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
