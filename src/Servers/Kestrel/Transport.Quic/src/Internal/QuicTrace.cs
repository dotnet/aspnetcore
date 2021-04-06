// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic.Internal
{
    internal class QuicTrace : IQuicTrace
    {
        private static readonly Action<ILogger, string, Exception?> _acceptedConnection =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "AcceptedConnection"), @"Connection id ""{ConnectionId}"" accepted.", skipEnabledCheck: true);
        private static readonly Action<ILogger, string, Exception?> _acceptedStream =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, "AcceptedStream"), @"Stream id ""{ConnectionId}"" accepted.", skipEnabledCheck: true);
        private static readonly Action<ILogger, string, Exception?> _connectionError =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, "ConnectionError"), @"Connection id ""{ConnectionId}"" unexpected error.", skipEnabledCheck: true);
        private static readonly Action<ILogger, string, Exception?> _streamError =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, "StreamError"), @"Stream id ""{ConnectionId}"" unexpected error.", skipEnabledCheck: true);
        private static readonly Action<ILogger, string, Exception?> _streamPause =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, "StreamPause"), @"Stream id ""{ConnectionId}"" paused.", skipEnabledCheck: true);
        private static readonly Action<ILogger, string, Exception?> _streamResume =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6, "StreamResume"), @"Stream id ""{ConnectionId}"" resumed.", skipEnabledCheck: true);
        private static readonly Action<ILogger, string, string, Exception?> _streamShutdownWrite =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(7, "StreamShutdownWrite"), @"Stream id ""{ConnectionId}"" shutting down writes because: ""{Reason}"".", skipEnabledCheck: true);
        private static readonly Action<ILogger, string, string, Exception?> _streamAborted =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(8, "StreamAbort"), @"Stream id ""{ConnectionId}"" aborted by application because: ""{Reason}"".", skipEnabledCheck: true);

        private ILogger _logger;

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
                _acceptedStream(_logger, streamContext.ConnectionId, null);
            }
        }

        public void ConnectionError(BaseConnectionContext connection, Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _connectionError(_logger, connection.ConnectionId, ex);
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

        public void StreamAbort(QuicStreamContext streamContext, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _streamAborted(_logger, streamContext.ConnectionId, reason, null);
            }
        }
    }
}
