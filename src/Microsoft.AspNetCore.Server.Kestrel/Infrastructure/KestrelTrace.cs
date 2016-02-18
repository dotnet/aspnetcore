// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    /// <summary>
    /// Summary description for KestrelTrace
    /// </summary>
    public class KestrelTrace : IKestrelTrace
    {
        private static readonly Action<ILogger, string, Exception> _connectionStart;
        private static readonly Action<ILogger, string, Exception> _connectionStop;
        private static readonly Action<ILogger, string, Exception> _connectionPause;
        private static readonly Action<ILogger, string, Exception> _connectionResume;
        private static readonly Action<ILogger, string, Exception> _connectionReadFin;
        private static readonly Action<ILogger, string, Exception> _connectionWriteFin;
        private static readonly Action<ILogger, string, int, Exception> _connectionWroteFin;
        private static readonly Action<ILogger, string, Exception> _connectionKeepAlive;
        private static readonly Action<ILogger, string, Exception> _connectionDisconnect;
        private static readonly Action<ILogger, string, Exception> _connectionError;
        private static readonly Action<ILogger, string, int, Exception> _connectionDisconnectedWrite;
        private static readonly Action<ILogger, Exception> _notAllConnectionsClosedGracefully;

        protected readonly ILogger _logger;

        static KestrelTrace()
        {
            _connectionStart = LoggerMessage.Define<string>(LogLevel.Debug, 1, @"Connection id ""{ConnectionId}"" started.");
            _connectionStop = LoggerMessage.Define<string>(LogLevel.Debug, 2, @"Connection id ""{ConnectionId}"" stopped.");
            // ConnectionRead: Reserved: 3
            _connectionPause = LoggerMessage.Define<string>(LogLevel.Debug, 4, @"Connection id ""{ConnectionId}"" paused.");
            _connectionResume = LoggerMessage.Define<string>(LogLevel.Debug, 5, @"Connection id ""{ConnectionId}"" resumed.");
            _connectionReadFin = LoggerMessage.Define<string>(LogLevel.Debug, 6, @"Connection id ""{ConnectionId}"" received FIN.");
            _connectionWriteFin = LoggerMessage.Define<string>(LogLevel.Debug, 7, @"Connection id ""{ConnectionId}"" sending FIN.");
            _connectionWroteFin = LoggerMessage.Define<string, int>(LogLevel.Debug, 8, @"Connection id ""{ConnectionId}"" sent FIN with status ""{Status}"".");
            _connectionKeepAlive = LoggerMessage.Define<string>(LogLevel.Debug, 9, @"Connection id ""{ConnectionId}"" completed keep alive response.");
            _connectionDisconnect = LoggerMessage.Define<string>(LogLevel.Debug, 10, @"Connection id ""{ConnectionId}"" disconnecting.");
            // ConnectionWrite: Reserved: 11
            // ConnectionWriteCallback: Reserved: 12
            // ApplicationError: Reserved: 13 - LoggerMessage.Define overload not present 
            _connectionError = LoggerMessage.Define<string>(LogLevel.Information, 14, @"Connection id ""{ConnectionId}"" communication error");
            _connectionDisconnectedWrite = LoggerMessage.Define<string, int>(LogLevel.Debug, 15, @"Connection id ""{ConnectionId}"" write of ""{count}"" bytes to disconnected client.");
            _notAllConnectionsClosedGracefully = LoggerMessage.Define(LogLevel.Debug, 16, "Some connections failed to close gracefully during server shutdown.");
        }

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

        public virtual void ApplicationError(Exception ex)
        {
            _logger.LogError(13, ex, "An unhandled exception was thrown by the application.");
        }

        public virtual void ConnectionError(string connectionId, Exception ex)
        {
            _connectionError(_logger, connectionId, ex);
        }

        public virtual void ConnectionDisconnectedWrite(string connectionId, int count, Exception ex)
        {
            _connectionDisconnectedWrite(_logger, connectionId, count, ex);
        }

        public virtual void NotAllConnectionsClosedGracefully()
        {
            _notAllConnectionsClosedGracefully(_logger, null);
        }

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }

        public virtual bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public virtual IDisposable BeginScopeImpl(object state)
        {
            return _logger.BeginScopeImpl(state);
        }
    }
}