// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal
{
    /// <summary>
    /// Summary description for KestrelTrace
    /// </summary>
    public class KestrelTrace : IKestrelTrace
    {
        private static readonly Action<ILogger, string, Exception> _connectionStart =
            LoggerMessage.Define<string>(LogLevel.Debug, 1, @"Connection id ""{ConnectionId}"" started.");

        private static readonly Action<ILogger, string, Exception> _connectionStop =
            LoggerMessage.Define<string>(LogLevel.Debug, 2, @"Connection id ""{ConnectionId}"" stopped.");

        // ConnectionRead: Reserved: 3

        private static readonly Action<ILogger, string, Exception> _connectionPause =
            LoggerMessage.Define<string>(LogLevel.Debug, 4, @"Connection id ""{ConnectionId}"" paused.");

        private static readonly Action<ILogger, string, Exception> _connectionResume =
            LoggerMessage.Define<string>(LogLevel.Debug, 5, @"Connection id ""{ConnectionId}"" resumed.");

        private static readonly Action<ILogger, string, Exception> _connectionReadFin =
            LoggerMessage.Define<string>(LogLevel.Debug, 6, @"Connection id ""{ConnectionId}"" received FIN.");

        private static readonly Action<ILogger, string, Exception> _connectionWriteFin =
            LoggerMessage.Define<string>(LogLevel.Debug, 7, @"Connection id ""{ConnectionId}"" sending FIN.");

        private static readonly Action<ILogger, string, int, Exception> _connectionWroteFin =
            LoggerMessage.Define<string, int>(LogLevel.Debug, 8, @"Connection id ""{ConnectionId}"" sent FIN with status ""{Status}"".");

        private static readonly Action<ILogger, string, Exception> _connectionKeepAlive =
            LoggerMessage.Define<string>(LogLevel.Debug, 9, @"Connection id ""{ConnectionId}"" completed keep alive response.");

        private static readonly Action<ILogger, string, Exception> _connectionDisconnect =
            LoggerMessage.Define<string>(LogLevel.Debug, 10, @"Connection id ""{ConnectionId}"" disconnecting.");

        // ConnectionWrite: Reserved: 11

        // ConnectionWriteCallback: Reserved: 12

        private static readonly Action<ILogger, string, Exception> _applicationError =
            LoggerMessage.Define<string>(LogLevel.Error, 13, @"Connection id ""{ConnectionId}"": An unhandled exception was thrown by the application.");

        private static readonly Action<ILogger, string, Exception> _connectionError =
            LoggerMessage.Define<string>(LogLevel.Information, 14, @"Connection id ""{ConnectionId}"" communication error.");

        private static readonly Action<ILogger, string, int, Exception> _connectionDisconnectedWrite =
            LoggerMessage.Define<string, int>(LogLevel.Debug, 15, @"Connection id ""{ConnectionId}"" write of ""{count}"" bytes to disconnected client.");

        private static readonly Action<ILogger, Exception> _notAllConnectionsClosedGracefully =
            LoggerMessage.Define(LogLevel.Debug, 16, "Some connections failed to close gracefully during server shutdown.");

        private static readonly Action<ILogger, string, string, Exception> _connectionBadRequest =
            LoggerMessage.Define<string, string>(LogLevel.Information, 17, @"Connection id ""{ConnectionId}"" bad request data: ""{message}""");

        private static readonly Action<ILogger, string, long, Exception> _connectionHeadResponseBodyWrite =
            LoggerMessage.Define<string, long>(LogLevel.Debug, 18, @"Connection id ""{ConnectionId}"" write of ""{count}"" body bytes to non-body HEAD response.");

        private static readonly Action<ILogger, string, Exception> _connectionReset =
            LoggerMessage.Define<string>(LogLevel.Debug, 19, @"Connection id ""{ConnectionId}"" reset.");

        private static readonly Action<ILogger, string, Exception> _requestProcessingError =
            LoggerMessage.Define<string>(LogLevel.Information, 20, @"Connection id ""{ConnectionId}"" request processing ended abnormally.");

        private static readonly Action<ILogger, Exception> _notAllConnectionsAborted =
            LoggerMessage.Define(LogLevel.Debug, 21, "Some connections failed to abort during server shutdown.");

        protected readonly ILogger _logger;

        public KestrelTrace(ILogger logger)
        {
            _logger = logger;
        }

        public virtual void ConnectionStart(string connectionId)
        {
            _connectionStart(_logger, connectionId, null);
        }

        public virtual void ConnectionStop(string connectionId)
        {
            _connectionStop(_logger, connectionId, null);
        }

        public virtual void ConnectionRead(string connectionId, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 3
        }

        public virtual void ConnectionPause(string connectionId)
        {
            _connectionPause(_logger, connectionId, null);
        }

        public virtual void ConnectionResume(string connectionId)
        {
            _connectionResume(_logger, connectionId, null);
        }

        public virtual void ConnectionReadFin(string connectionId)
        {
            _connectionReadFin(_logger, connectionId, null);
        }

        public virtual void ConnectionWriteFin(string connectionId)
        {
            _connectionWriteFin(_logger, connectionId, null);
        }

        public virtual void ConnectionWroteFin(string connectionId, int status)
        {
            _connectionWroteFin(_logger, connectionId, status, null);
        }

        public virtual void ConnectionKeepAlive(string connectionId)
        {
            _connectionKeepAlive(_logger, connectionId, null);
        }

        public virtual void ConnectionDisconnect(string connectionId)
        {
            _connectionDisconnect(_logger, connectionId, null);
        }

        public virtual void ConnectionWrite(string connectionId, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 11
        }

        public virtual void ConnectionWriteCallback(string connectionId, int status)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 12
        }

        public virtual void ApplicationError(string connectionId, Exception ex)
        {
            _applicationError(_logger, connectionId, ex);
        }

        public virtual void ConnectionError(string connectionId, Exception ex)
        {
            _connectionError(_logger, connectionId, ex);
        }

        public virtual void ConnectionDisconnectedWrite(string connectionId, int count, Exception ex)
        {
            _connectionDisconnectedWrite(_logger, connectionId, count, ex);
        }

        public virtual void ConnectionHeadResponseBodyWrite(string connectionId, long count)
        {
            _connectionHeadResponseBodyWrite(_logger, connectionId, count, null);
        }

        public virtual void NotAllConnectionsClosedGracefully()
        {
            _notAllConnectionsClosedGracefully(_logger, null);
        }

        public virtual void NotAllConnectionsAborted()
        {
            _notAllConnectionsAborted(_logger, null);
        }

        public void ConnectionBadRequest(string connectionId, BadHttpRequestException ex)
        {
            _connectionBadRequest(_logger, connectionId, ex.Message, ex);
        }

        public virtual void ConnectionReset(string connectionId)
        {
            _connectionReset(_logger, connectionId, null);
        }

        public virtual void RequestProcessingError(string connectionId, Exception ex)
        {
            _requestProcessingError(_logger, connectionId, ex);
        }

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }

        public virtual bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public virtual IDisposable BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }
    }
}