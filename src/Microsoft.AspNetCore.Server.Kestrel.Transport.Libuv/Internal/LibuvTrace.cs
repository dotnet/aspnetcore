// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public class LibuvTrace : ILibuvTrace
    {
        // ConnectionRead: Reserved: 3

        private static readonly Action<ILogger, string, Exception> _connectionReadFin =
            LoggerMessage.Define<string>(LogLevel.Debug, 6, @"Connection id ""{ConnectionId}"" received FIN.");

        private static readonly Action<ILogger, string, Exception> _connectionWriteFin =
            LoggerMessage.Define<string>(LogLevel.Debug, 7, @"Connection id ""{ConnectionId}"" sending FIN.");

        private static readonly Action<ILogger, string, int, Exception> _connectionWroteFin =
            LoggerMessage.Define<string, int>(LogLevel.Debug, 8, @"Connection id ""{ConnectionId}"" sent FIN with status ""{Status}"".");

        // ConnectionWrite: Reserved: 11

        // ConnectionWriteCallback: Reserved: 12

        private static readonly Action<ILogger, string, Exception> _connectionError =
            LoggerMessage.Define<string>(LogLevel.Information, 14, @"Connection id ""{ConnectionId}"" communication error.");

        private static readonly Action<ILogger, string, Exception> _connectionReset =
            LoggerMessage.Define<string>(LogLevel.Debug, 19, @"Connection id ""{ConnectionId}"" reset.");

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

        public void ConnectionReadFin(string connectionId)
        {
            _connectionReadFin(_logger, connectionId, null);
        }

        public void ConnectionWriteFin(string connectionId)
        {
            _connectionWriteFin(_logger, connectionId, null);
        }

        public void ConnectionWroteFin(string connectionId, int status)
        {
            _connectionWroteFin(_logger, connectionId, status, null);
        }

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

        public void ConnectionError(string connectionId, Exception ex)
        {
            _connectionError(_logger, connectionId, ex);
        }

        public void ConnectionReset(string connectionId)
        {
            _connectionReset(_logger, connectionId, null);
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
