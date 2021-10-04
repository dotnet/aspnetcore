// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed partial class SocketsTrace : ILogger
    {
        private readonly ILogger _logger;

        public SocketsTrace(ILogger logger)
        {
            _logger = logger;
        }

        public void ConnectionRead(SocketConnection connection, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 3
        }

        [LoggerMessage(6, LogLevel.Debug, @"Connection id ""{ConnectionId}"" received FIN.", EventName = "ConnectionReadFin", SkipEnabledCheck = true)]
        private static partial void ConnectionReadFin(ILogger logger, string connectionId);

        public void ConnectionReadFin(SocketConnection connection)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionReadFin(_logger, connection.ConnectionId);
            }
        }

        [LoggerMessage(7, LogLevel.Debug, @"Connection id ""{ConnectionId}"" sending FIN because: ""{Reason}""", EventName = "ConnectionWriteFin", SkipEnabledCheck = true)]
        private static partial void ConnectionWriteFin(ILogger logger, string connectionId, string reason);

        public void ConnectionWriteFin(SocketConnection connection, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionWriteFin(_logger, connection.ConnectionId, reason);
            }
        }

        public void ConnectionWrite(SocketConnection connection, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 11
        }

        public void ConnectionWriteCallback(SocketConnection connection, int status)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 12
        }

        [LoggerMessage(14, LogLevel.Debug, @"Connection id ""{ConnectionId}"" communication error.", EventName = "ConnectionError", SkipEnabledCheck = true)]
        private static partial void ConnectionError(ILogger logger, string connectionId, Exception ex);

        public void ConnectionError(SocketConnection connection, Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionError(_logger, connection.ConnectionId, ex);
            }
        }

        [LoggerMessage(19, LogLevel.Debug, @"Connection id ""{ConnectionId}"" reset.", EventName = "ConnectionReset", SkipEnabledCheck = true)]
        public partial void ConnectionReset(string connectionId);

        public void ConnectionReset(SocketConnection connection)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionReset(connection.ConnectionId);
            }
        }

        [LoggerMessage(4, LogLevel.Debug, @"Connection id ""{ConnectionId}"" paused.", EventName = "ConnectionPause", SkipEnabledCheck = true)]
        private static partial void ConnectionPause(ILogger logger, string connectionId);

        public void ConnectionPause(SocketConnection connection)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionPause(_logger, connection.ConnectionId);
            }
        }

        [LoggerMessage(5, LogLevel.Debug, @"Connection id ""{ConnectionId}"" resumed.", EventName = "ConnectionResume", SkipEnabledCheck = true)]
        private static partial void ConnectionResume(ILogger logger, string connectionId);

        public void ConnectionResume(SocketConnection connection)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                ConnectionResume(_logger, connection.ConnectionId);
            }
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
