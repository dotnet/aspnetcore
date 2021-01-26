// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic.Internal
{
    internal class QuicTrace : IQuicTrace
    {
        private static readonly Action<ILogger, string, Exception?> _acceptedConnection =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "AcceptedConnection"), @"Connection id ""{ConnectionId}"" accepted.");
        private static readonly Action<ILogger, string, Exception?> _acceptedStream =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, "AcceptedStream"), @"Stream id ""{ConnectionId}"" accepted.");
        private static readonly Action<ILogger, string, Exception?> _connectionError =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, "ConnectionError"), @"Connection id ""{ConnectionId}"" unexpected error.");
        private static readonly Action<ILogger, string, Exception?> _streamError =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, "StreamError"), @"Stream id ""{ConnectionId}"" unexpected error.");
        private static readonly Action<ILogger, string, Exception?> _streamPause =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, "StreamPause"), @"Stream id ""{ConnectionId}"" paused.");
        private static readonly Action<ILogger, string, Exception?> _streamResume =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6, "StreamResume"), @"Stream id ""{ConnectionId}"" resumed.");
        private static readonly Action<ILogger, string, string, Exception?> _streamShutdownWrite =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(7, "StreamShutdownWrite"), @"Stream id ""{ConnectionId}"" shutting down writes because: ""{Reason}"".");
        private static readonly Action<ILogger, string, string, Exception?> _streamAborted =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(8, "StreamAbort"), @"Stream id ""{ConnectionId}"" aborted by application because: ""{Reason}"".");

        private ILogger _logger;

        public QuicTrace(ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);

        public void AcceptedConnection(string connectionId)
        {
            _acceptedConnection(_logger, connectionId, null);
        }

        public void AcceptedStream(string streamId)
        {
            _acceptedStream(_logger, streamId, null);
        }

        public void ConnectionError(string connectionId, Exception ex)
        {
            _connectionError(_logger, connectionId, ex);
        }

        public void StreamError(string streamId, Exception ex)
        {
            _streamError(_logger, streamId, ex);
        }

        public void StreamPause(string streamId)
        {
            _streamPause(_logger, streamId, null);
        }

        public void StreamResume(string streamId)
        {
            _streamResume(_logger, streamId, null);
        }

        public void StreamShutdownWrite(string streamId, string reason)
        {
            _streamShutdownWrite(_logger, streamId, reason, null);
        }

        public void StreamAbort(string streamId, string reason)
        {
            _streamAborted(_logger, streamId, reason, null);
        }
    }
}
