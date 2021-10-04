// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal sealed partial class LibuvTrace : ILogger
    {
        private readonly ILogger _logger;

        public LibuvTrace(ILogger logger)
        {
            _logger = logger;
        }

        public void ConnectionRead(string connectionId, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 3
        }

        [LoggerMessage(6, LogLevel.Debug, @"Connection id ""{ConnectionId}"" received FIN.", EventName = nameof(ConnectionReadFin))]
        public partial void ConnectionReadFin(string connectionId);

        [LoggerMessage(7, LogLevel.Debug, @"Connection id ""{ConnectionId}"" sending FIN because: ""{Reason}""", EventName = nameof(ConnectionWriteFin))]
        public partial void ConnectionWriteFin(string connectionId, string reason);

        public void ConnectionWrite(string connectionId, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 11
        }

        public void ConnectionWriteCallback(string connectionId, int status)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 12
        }

        [LoggerMessage(14, LogLevel.Debug, @"Connection id ""{ConnectionId}"" communication error.", EventName = nameof(ConnectionError))]
        public partial void ConnectionError(string connectionId, Exception ex);

        [LoggerMessage(19, LogLevel.Debug, @"Connection id ""{ConnectionId}"" reset.", EventName = nameof(ConnectionReset))]
        public partial void ConnectionReset(string connectionId);

        [LoggerMessage(4, LogLevel.Debug, @"Connection id ""{ConnectionId}"" paused.", EventName = nameof(ConnectionPause))]
        public partial void ConnectionPause(string connectionId);

        [LoggerMessage(5, LogLevel.Debug, @"Connection id ""{ConnectionId}"" resumed.", EventName = nameof(ConnectionResume))]
        public partial void ConnectionResume(string connectionId);

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
